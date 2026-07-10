using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CapacityUI : MonoBehaviour
{
	private const float DefaultWidth = 500f;

	private const float DefaultHeight = 50f;

	private const float BarOffsetY = -150f;

	private const float FillAreaInsetLeft = 12f;

	private const float FillAreaInsetRight = 12f;

	private const float FillAreaInsetTop = 4.15f;

	private const float FillAreaInsetBottom = 9.35f;

	private const float FillVisualOffsetX = 0f;

	private const float FillVisualOffsetY = -2.25f;

	private const float TickWidth = 5f;

	private const float TickHeight = 12.5f;

	private const float TextTweenDuration = 0.2f;

	private static readonly Color DisabledColor = new Color(40f / 51f, 40f / 51f, 40f / 51f, 0.5019608f);

	private static CapacityUI _instance;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private RectTransform fillArea;

	[SerializeField]
	private RectTransform fillRect;

	[SerializeField]
	private RectTransform ticksContainer;

	[SerializeField]
	private Text percentText;

	private Sprite _fillLowSprite;

	private Sprite _fillMidSprite;

	private Sprite _fillHighSprite;

	private Sprite _fillFullSprite;

	private Sprite _tickSprite;

	private Coroutine _textRoutine;

	private Coroutine _fillRoutine;

	private float _displayedPercent;

	private float _displayedFillPercent;

	private int _lastTickCapacity = -1;

	private bool _isDisabled;

	public static CapacityUI Instance => _instance;

	public static CapacityUI EnsureExists()
	{
		if (_instance != null)
		{
			return _instance;
		}
		GameObject root = new GameObject("Capacity", typeof(RectTransform));
		Canvas canvas = root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 50;
		CanvasScaler scaler = root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1080f, 1920f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;
		root.AddComponent<GraphicRaycaster>();
		return root.AddComponent<CapacityUI>();
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		_instance = this;
		LoadSprites();
		BuildUI();
	}

	private void OnEnable()
	{
		GameEventBus.OnUpdateCapcityUI = (Action<int, int>)Delegate.Combine(GameEventBus.OnUpdateCapcityUI, new Action<int, int>(SetCapacity));
		GameEventBus.OnLevelLoaded = (Action<LevelData>)Delegate.Combine(GameEventBus.OnLevelLoaded, new Action<LevelData>(OnLevelLoaded));
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Combine(GameEventBus.OnInitLoadLevel, new Action(ResetView));
	}

	private void OnDisable()
	{
		GameEventBus.OnUpdateCapcityUI = (Action<int, int>)Delegate.Remove(GameEventBus.OnUpdateCapcityUI, new Action<int, int>(SetCapacity));
		GameEventBus.OnLevelLoaded = (Action<LevelData>)Delegate.Remove(GameEventBus.OnLevelLoaded, new Action<LevelData>(OnLevelLoaded));
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Remove(GameEventBus.OnInitLoadLevel, new Action(ResetView));
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	public void InitLevel(int maxCapacity)
	{
		UpdateTicks(maxCapacity);
		ResetView();
	}

	public void SetCapacity(int capacity, int maxCapacity)
	{
		float percent = ((maxCapacity > 0) ? Mathf.Clamp01((float)capacity / (float)maxCapacity) : 0f);
		SetFillSmooth(percent);
		SetTextSmooth(percent);
		ApplyFillSprite(percent);
	}

	public void SetDisableCapacity(bool isDisabled)
	{
		_isDisabled = isDisabled;
		Color color = (isDisabled ? DisabledColor : Color.white);
		if (backgroundImage != null)
		{
			backgroundImage.color = color;
		}
		if (fillImage != null)
		{
			fillImage.color = color;
		}
		if (percentText != null)
		{
			percentText.color = color;
		}
		SetTickColor(color);
	}

	private void OnLevelLoaded(LevelData levelData)
	{
		if (MonoSingleton<CapacityManager>.Instance != null)
		{
			UpdateTicks(MonoSingleton<CapacityManager>.Instance.MaxCapacity);
		}
		else if (levelData != null)
		{
			UpdateTicks(levelData.Capacity);
		}
	}

	private void ResetView()
	{
		if (_textRoutine != null)
		{
			StopCoroutine(_textRoutine);
			_textRoutine = null;
		}
		if (_fillRoutine != null)
		{
			StopCoroutine(_fillRoutine);
			_fillRoutine = null;
		}
		_displayedPercent = 0f;
		_displayedFillPercent = 0f;
		SetFill(0f);
		ApplyFillSprite(0f);
		if (percentText != null)
		{
			percentText.text = "0%";
		}
	}

	private void BuildUI()
	{
		RectTransform rootRect = base.transform as RectTransform;
		if (rootRect == null)
		{
			Debug.LogError("[CapacityUI] Root is not a RectTransform.");
			return;
		}
		rootRect.anchorMin = Vector2.zero;
		rootRect.anchorMax = Vector2.one;
		rootRect.pivot = new Vector2(0.5f, 0.5f);
		rootRect.offsetMin = Vector2.zero;
		rootRect.offsetMax = Vector2.zero;
		RectTransform barRoot = CreateRect("CapacityBar", base.transform);
		barRoot.anchorMin = new Vector2(0.5f, 1f);
		barRoot.anchorMax = new Vector2(0.5f, 1f);
		barRoot.pivot = new Vector2(0.5f, 0.5f);
		barRoot.anchoredPosition = new Vector2(0f, -150f);
		barRoot.sizeDelta = new Vector2(500f, 50f);
		backgroundImage = CreateImage("Background", barRoot, GetSprite("progress_bar1"), Image.Type.Sliced);
		RectTransform backgroundRect = backgroundImage.rectTransform;
		Stretch(backgroundRect, Vector2.zero, Vector2.zero);
		Mask backgroundMask = backgroundImage.gameObject.AddComponent<Mask>();
		backgroundMask.showMaskGraphic = true;
		fillArea = CreateRect("Fill Area", backgroundRect);
		Stretch(fillArea, new Vector2(12f, 9.35f), new Vector2(-12f, -4.15f));
		fillArea.gameObject.AddComponent<RectMask2D>();
		fillImage = CreateImage("Fill", fillArea, _fillLowSprite, Image.Type.Tiled);
		fillRect = fillImage.rectTransform;
		fillRect.anchorMin = new Vector2(0f, 0f);
		fillRect.anchorMax = new Vector2(0f, 1f);
		fillRect.pivot = new Vector2(0f, 0.5f);
		fillRect.anchoredPosition = new Vector2(0f, -2.25f);
		fillRect.sizeDelta = new Vector2(0f, -2.6f);
		ticksContainer = CreateRect("Ticks", backgroundRect);
		Stretch(ticksContainer, new Vector2(12f, 0f), new Vector2(-12f, 0f));
		percentText = CreateText("PercentText", backgroundRect);
		RectTransform textRect = percentText.rectTransform;
		textRect.anchorMin = new Vector2(0.5f, 0.5f);
		textRect.anchorMax = new Vector2(0.5f, 0.5f);
		textRect.pivot = new Vector2(0.5f, 0.5f);
		textRect.anchoredPosition = new Vector2(0f, -2.4f);
		textRect.sizeDelta = new Vector2(500f, 35f);
		percentText.text = "0%";
	}

	private void LoadSprites()
	{
		_fillLowSprite = GetSprite("progress_bar2");
		_fillMidSprite = GetSprite("progress_bar3");
		_fillHighSprite = GetSprite("progress_bar4");
		_fillFullSprite = GetSprite("progress_bar5");
		_tickSprite = GetSprite("progress_vachchia");
	}

	private static Sprite GetSprite(string spriteName)
	{
		string path = "Sprites/" + spriteName;
		Sprite sprite = Resources.Load<Sprite>(path);
		if (sprite != null)
		{
			return sprite;
		}
		Sprite[] sprites = Resources.LoadAll<Sprite>(path);
		if (sprites == null || sprites.Length == 0)
		{
			return null;
		}
		for (int i = 0; i < sprites.Length; i++)
		{
			if (sprites[i] != null && sprites[i].name == spriteName)
			{
				return sprites[i];
			}
		}
		for (int j = 0; j < sprites.Length; j++)
		{
			if (sprites[j] != null && sprites[j].name.StartsWith(spriteName))
			{
				return sprites[j];
			}
		}
		return sprites[0];
	}

	private static Image CreateImage(string name, Transform parent, Sprite sprite, Image.Type type)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		Image image = go.AddComponent<Image>();
		image.raycastTarget = false;
		image.sprite = sprite;
		image.type = type;
		if (sprite == null)
		{
			image.color = Color.clear;
		}
		return image;
	}

	private static RectTransform CreateRect(string name, Transform parent)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		return go.AddComponent<RectTransform>();
	}

	private static Text CreateText(string name, Transform parent)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		Text text = go.AddComponent<Text>();
		text.raycastTarget = false;
		text.alignment = TextAnchor.MiddleCenter;
		text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		text.fontSize = 26;
		text.fontStyle = FontStyle.Bold;
		text.color = Color.white;
		return text;
	}

	private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = offsetMin;
		rect.offsetMax = offsetMax;
	}

	private void SetFill(float percent)
	{
		if (!(fillRect == null) && !(fillArea == null))
		{
			float maxWidth = Mathf.Max(0f, GetFillAreaWidth());
			float width = maxWidth * Mathf.Clamp01(percent);
			fillRect.sizeDelta = new Vector2(width, -2.6f);
			_displayedFillPercent = Mathf.Clamp01(percent);
		}
	}

	private void ApplyFillSprite(float percent)
	{
		if (!(fillImage == null))
		{
			Sprite sprite = _fillLowSprite;
			if (percent > 0.75f)
			{
				sprite = _fillFullSprite;
			}
			else if (percent > 0.5f)
			{
				sprite = _fillHighSprite;
			}
			else if (percent > 0.25f)
			{
				sprite = _fillMidSprite;
			}
			if (sprite != null && fillImage.sprite != sprite)
			{
				fillImage.sprite = sprite;
				fillImage.type = Image.Type.Tiled;
			}
		}
	}

	private void SetFillSmooth(float targetPercent)
	{
		if (!(fillRect == null) && !(fillArea == null))
		{
			targetPercent = Mathf.Clamp01(targetPercent);
			if (_fillRoutine != null)
			{
				StopCoroutine(_fillRoutine);
			}
			if (Mathf.Approximately(targetPercent, 0f))
			{
				SetFill(0f);
				_fillRoutine = null;
			}
			else
			{
				_fillRoutine = StartCoroutine(AnimateFillRoutine(_displayedFillPercent, targetPercent));
			}
		}
	}

	private IEnumerator AnimateFillRoutine(float from, float to)
	{
		float elapsed = 0f;
		while (elapsed < 0.2f)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / 0.2f);
			SetFill(Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t)));
			yield return null;
		}
		SetFill(to);
		_fillRoutine = null;
	}

	private float GetFillAreaWidth()
	{
		if (fillArea == null)
		{
			return 0f;
		}
		float width = fillArea.rect.width;
		if (width > 0f)
		{
			return width;
		}
		return Mathf.Max(0f, 476f);
	}

	private void SetTextSmooth(float targetPercent)
	{
		if (!(percentText == null))
		{
			if (_textRoutine != null)
			{
				StopCoroutine(_textRoutine);
			}
			if (Mathf.Approximately(targetPercent, 0f))
			{
				_displayedPercent = 0f;
				percentText.text = "0%";
				_textRoutine = null;
			}
			else
			{
				_textRoutine = StartCoroutine(AnimateTextRoutine(_displayedPercent, targetPercent));
			}
		}
	}

	private IEnumerator AnimateTextRoutine(float from, float to)
	{
		float elapsed = 0f;
		while (elapsed < 0.2f)
		{
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / 0.2f);
			_displayedPercent = Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t));
			percentText.text = Mathf.RoundToInt(_displayedPercent * 100f) + "%";
			yield return null;
		}
		_displayedPercent = to;
		percentText.text = Mathf.RoundToInt(to * 100f) + "%";
		_textRoutine = null;
	}

	private void UpdateTicks(int maxCapacity)
	{
		if (ticksContainer == null || maxCapacity == _lastTickCapacity)
		{
			return;
		}
		_lastTickCapacity = maxCapacity;
		for (int i = ticksContainer.childCount - 1; i >= 0; i--)
		{
			UnityEngine.Object.Destroy(ticksContainer.GetChild(i).gameObject);
		}
		if (maxCapacity > 1)
		{
			Color color = (_isDisabled ? DisabledColor : Color.white);
			for (int j = 1; j < maxCapacity; j++)
			{
				Image tick = CreateImage("Tick_" + j, ticksContainer, _tickSprite, Image.Type.Simple);
				tick.color = color;
				RectTransform rect = tick.rectTransform;
				rect.anchorMin = new Vector2((float)j / (float)maxCapacity, 0.5f);
				rect.anchorMax = rect.anchorMin;
				rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = Vector2.zero;
				rect.sizeDelta = new Vector2(5f, 12.5f);
			}
		}
	}

	private void SetTickColor(Color color)
	{
		if (ticksContainer == null)
		{
			return;
		}
		for (int i = 0; i < ticksContainer.childCount; i++)
		{
			Image image = ticksContainer.GetChild(i).GetComponent<Image>();
			if (image != null)
			{
				image.color = color;
			}
		}
	}
}
