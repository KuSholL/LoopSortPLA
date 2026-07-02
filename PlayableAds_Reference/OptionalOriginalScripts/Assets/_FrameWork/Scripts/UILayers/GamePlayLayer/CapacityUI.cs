using System.Collections;
using System.Collections.Generic;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CapacityUI : MonoBehaviour
{
    public static CapacityUI Instance { get; private set; }
    [SerializeField] private Color disableColor;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI capacityUI;
    [SerializeField] private SliderValidator progressSlider;
    [SerializeField] private Canvas highlightCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 0.2f;
    
    [Header("Ticks Configuration")]
    [SerializeField] private RectTransform ticksContainer;
    [SerializeField] private Image tickPrefab;

    private static List<CapacitySliderStateData> SliderStates => ConfigHolder.Instance.CapacityConfigSO.sliderStates;

    private MonoPool<Image> _tickPool;
    private int _lastMaxCapacity = -1;
    private bool _isDisabled = false;
    private int _currentStateIndex = -1;
    private float _displayedPercent = 0f;
    private MotionHandle _textMotionHandle;

    private void Awake()
    {
        Instance = this;
        GameEventBus.OnHighlightCameraActiveChanged += OnHighlightCameraActiveChanged;
        GameEventBus.OnLevelLoaded += OnLevelLoaded;
    }
    
    public void InitLevelLoaded()
    {
        capacityUI.text = TextUtility.GetI2("conveyor_load");
        _displayedPercent = 0f;
        if (_textMotionHandle.IsActive())
        {
            _textMotionHandle.Cancel();
        }
        SetProgressValue(0);
        
        _lastMaxCapacity = -1;
        if (_tickPool != null)
        {
            _tickPool.ReleasePool();
        }
    }

    public void SetCapacity(int capacity, int maxCapacity)
    {
        var percent = maxCapacity > 0 ? Mathf.Clamp01((float)capacity / maxCapacity) : 0f;

        SetProgressValue(percent);
        SetTextValueSmooth(percent);
        ApplyVisualForPercent(percent, false);
    }

    private void SetTextValueSmooth(float targetPercent)
    {
        if (capacityUI == null) return;

        if (_textMotionHandle.IsActive())
        {
            _textMotionHandle.Cancel();
        }

        if (targetPercent == 0f)
        {
            _displayedPercent = 0f;
            capacityUI.text = "0%";
        }
        else
        {
            _textMotionHandle = LMotion.Create(_displayedPercent, targetPercent, duration)
                .WithEase(Ease.OutQuad)
                .Bind(val =>
                {
                    _displayedPercent = val;
                    capacityUI.text = Mathf.RoundToInt(val * 100f) + "%";
                });
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (_textMotionHandle.IsActive())
        {
            _textMotionHandle.Cancel();
        }
        GameEventBus.OnHighlightCameraActiveChanged -= OnHighlightCameraActiveChanged;
        GameEventBus.OnLevelLoaded -= OnLevelLoaded;
        
        if (_tickPool != null)
        {
            _tickPool.Dispose();
        }
    }

    private void OnHighlightCameraActiveChanged(bool active)
    {
        bool isBoosterTut = UIClawBoosterTutorial.IsTutorial || UIExtraSlotBoosterLayer.IsTutorial || UIUndoBoosterTutorialLayer.IsTutorial;
        SetDisableCapacity(active && isBoosterTut);
    }

    private void OnLevelLoaded(LevelData levelData)
    {
        if (CapacityManager.Instance != null)
        {
            UpdateTicks(CapacityManager.Instance.MaxCapacity);
        }
        else if (levelData != null)
        {
            UpdateTicks(levelData.Capacity);
        }
    }

    public void SetTutorialHighlightActive(bool isActive)
    {
        if (highlightCanvas == null) return;
        Debug.Log($"Capacity canvas active: {isActive}, sorting: {highlightCanvas.sortingOrder}");
        highlightCanvas.overrideSorting = isActive;
    }

    private void ApplyVisualForPercent(float percent, bool forceRefresh)
    {
        if (fillImage == null || SliderStates == null || SliderStates.Count == 0)
        {
            return;
        }

        var matchedIndex = GetMatchedStateIndex(percent);
        if (matchedIndex < 0)
        {
            return;
        }

        if (!forceRefresh && matchedIndex == _currentStateIndex)
        {
            return;
        }

        _currentStateIndex = matchedIndex;
        var state = SliderStates[matchedIndex];
        fillImage.sprite = state.sprite;
        fillImage.type = state.imageType;
        fillImage.SetAllDirty();
    }

    private int GetMatchedStateIndex(float percent)
    {
        if (SliderStates == null || SliderStates.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < SliderStates.Count; i++)
        {
            if (SliderStates[i] != null && SliderStates[i].Matches(percent))
            {
                return i;
            }
        }

        // Fallback: If percent is out of bounds, return the first state for negative percent, or the last state otherwise
        if (percent < 0f)
        {
            return 0;
        }

        return SliderStates.Count - 1;
    }

    private void SetProgressValue(float value)
    {
        if (value == 0)
        {
            progressSlider.Value = value;
        }
        else
        {
            progressSlider.SetValueSmooth(value, duration);
        }
    }
    
    public void SetDisableCapacity(bool isDisabled)
    {
        _isDisabled = isDisabled;
        backgroundImage.color = isDisabled ? disableColor : Color.white;
        capacityUI.color = isDisabled ? disableColor : Color.white;
        fillImage.color = isDisabled ? disableColor : Color.white;
        UpdateTicksColor();
    }

    private void UpdateTicks(int maxCapacity)
    {
        if (ticksContainer == null || tickPrefab == null) return;
        if (maxCapacity == _lastMaxCapacity) return;

        _lastMaxCapacity = maxCapacity;

        if (_tickPool == null)
        {
            _tickPool = new MonoPool<Image>(tickPrefab, ticksContainer);
        }

        _tickPool.BeginSetup();

        if (maxCapacity > 1)
        {
            Color color = _isDisabled ? disableColor : Color.white;
            float containerWidth = ticksContainer.rect.width;
            for (int i = 1; i < maxCapacity; i++)
            {
                var tick = _tickPool.Get(true, ticksContainer);
                if (tick != null)
                {
                    tick.color = color;
                    var rect = tick.rectTransform;
                    float x = ((float)i / maxCapacity) * containerWidth;
                    rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                }
            }
        }

        _tickPool.EndSetup();
    }

    private void UpdateTicksColor()
    {
        if (_tickPool == null) return;
        Color color = _isDisabled ? disableColor : Color.white;
        foreach (var tick in _tickPool.Data)
        {
            if (tick != null)
            {
                tick.color = color;
            }
        }
    }
}
