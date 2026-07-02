using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;

public class ContainerMechanic : MonoBehaviour
{
    private static readonly List<ContainerMechanic> ActiveContainersInternal = new();

    [SerializeField] private int containerId = -1;
    [SerializeField] private bool isOpen;
    [SerializeField] private EBlockColorType unlockColor = EBlockColorType.None;
    [SerializeField] private List<CarrierContainerMember> carriers = new();

    private Vector3 _targetScale = Vector3.one;
    public Vector3 TargetScale => _targetScale;
    private MotionHandle _scaleMotionHandle;

    public void SetTargetScale(Vector3 scale)
    {
        _targetScale = scale;
    }

    public void SetScaleMotionHandle(MotionHandle handle)
    {
        if (_scaleMotionHandle.IsActive())
        {
            _scaleMotionHandle.TryCancel();
        }
        _scaleMotionHandle = handle;
    }

    public void CancelScaleAnimation()
    {
        if (_scaleMotionHandle.IsActive())
        {
            _scaleMotionHandle.TryCancel();
        }
    }
    [SerializeField] private GiftBoxVisual giftBoxVisual1X;
    [SerializeField] private GiftBoxVisual giftBoxVisual2X;
    [SerializeField] private GiftBoxVisual giftBoxVisual3X;
    [SerializeField] private GameObject ribbonParticle;
    [SerializeField] private Key3DCodeAnimator keyAnimator;
    public Key3DCodeAnimator KeyAnimator => keyAnimator;
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private float delayVfx = 0.5f;

    private bool _isOpening;

    private bool _isAssignedToUnlock;
    public bool IsAssignedToUnlock
    {
        get => _isAssignedToUnlock;
        set => _isAssignedToUnlock = value;
    }

    private bool _isLidOpening;
    public bool IsLidOpening => _isLidOpening;

    public bool IsOpen => isOpen;
    public bool IsOpening => _isOpening;
    public int ContainerId => containerId;

    public static IReadOnlyList<ContainerMechanic> GetActiveContainers() => ActiveContainersInternal;

    public static ContainerMechanic FindTarget(int targetContainerId, EBlockColorType colorType)
    {
        foreach (var container in ActiveContainersInternal)
        {
            if (container == null || container.containerId != targetContainerId) continue;
            if (!container.CanOpenWith(colorType)) continue;
            return container;
        }

        return null;
    }

    public static void UnlockTarget(int targetContainerId, EBlockColorType colorType)
    {
        foreach (var container in ActiveContainersInternal)
            container?.TryUnlock(targetContainerId, colorType);
    }

    private void Awake() => BindExistingCarriers();

    private void OnEnable()
    {
        if (!ActiveContainersInternal.Contains(this)) ActiveContainersInternal.Add(this);
        _isOpening = false;
        _isAssignedToUnlock = false;
        
        if (keyAnimator != null)
        {
            keyAnimator.SetActiveState(false);
        }

        if (ribbonParticle != null)
        {
            ribbonParticle.SetActive(false);
        }
        
        ApplyVisuals(unlockColor);

        if (giftBoxVisual1X != null)
        {
            giftBoxVisual1X.ResetVisual();
            giftBoxVisual1X.ApplyColor(unlockColor);
        }
        if (giftBoxVisual2X != null)
        {
            giftBoxVisual2X.ResetVisual();
            giftBoxVisual2X.ApplyColor(unlockColor);
        }
        if (giftBoxVisual3X != null)
        {
            giftBoxVisual3X.ResetVisual();
            giftBoxVisual3X.ApplyColor(unlockColor);
        }
        
        RefreshVisualState();
    }

    private void OnDisable()
    {
        ActiveContainersInternal.Remove(this);
        CancelScaleAnimation();
    }

    private void OnDestroy()
    {
        CancelScaleAnimation();
    }

    public void PlayTieScaleAnimation()
    {
        if (giftBoxVisual1X != null && giftBoxVisual1X.gameObject.activeInHierarchy)
            giftBoxVisual1X.PlayTieScaleAnimation();
        if (giftBoxVisual2X != null && giftBoxVisual2X.gameObject.activeInHierarchy)
            giftBoxVisual2X.PlayTieScaleAnimation();
        if (giftBoxVisual3X != null && giftBoxVisual3X.gameObject.activeInHierarchy)
            giftBoxVisual3X.PlayTieScaleAnimation();
    }

    public async UniTask PlayRibbonDisappearAnimationAsync()
    {
        var tasks = new List<UniTask>();
        if (giftBoxVisual1X != null && giftBoxVisual1X.gameObject.activeInHierarchy)
            tasks.Add(giftBoxVisual1X.PlayRibbonDisappearAnimation());
        if (giftBoxVisual2X != null && giftBoxVisual2X.gameObject.activeInHierarchy)
            tasks.Add(giftBoxVisual2X.PlayRibbonDisappearAnimation());
        if (giftBoxVisual3X != null && giftBoxVisual3X.gameObject.activeInHierarchy)
            tasks.Add(giftBoxVisual3X.PlayRibbonDisappearAnimation());
        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    public void StartUnlockSequence(Vector3 startPosition, Quaternion startRotation, Vector3 startScale, float flyDuration, Ease flyEase)
    {
        _isOpening = true;
        SetCollidersEnabled(false);

        if (keyAnimator == null)
        {
            Debug.LogError($"[ContainerMechanic] keyAnimator is null on {gameObject.name}!", this);
            return;
        }

        keyAnimator.PlayFlyAndUnlockAnimationAsync(
            startPosition,
            startRotation,
            startScale,
            flyDuration,
            flyEase,
            onFirstCut: () =>
            {
                PlayTieScaleAnimation();
            },
            onSecondCut: () =>
            {
                PlayTieScaleAnimation();
                PlayDisappearAndOpenSequenceAsync().Forget();
            }).Forget();
    }

    private async UniTaskVoid PlayDisappearAndOpenSequenceAsync()
    {
        await PlayRibbonDisappearAnimationAsync();

        if (ribbonParticle != null)
        {
            ribbonParticle.SetActive(true);
        }
        PlayOpenAnimationAndDisableAsync().Forget();
    }

    private async UniTask Open()
    {
        _isOpening = true;
        SetCollidersEnabled(false);
        
        if (keyAnimator == null)
        {
            Debug.LogError($"[ContainerMechanic] keyAnimator is null on {gameObject.name}!", this);
            return;
        }

        if (delayVfx > 0f)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delayVfx));
        }

        keyAnimator.PlayFlyAndUnlockAnimationAsync(
            keyAnimator.transform.position,
            keyAnimator.transform.rotation,
            keyAnimator.transform.localScale,
            0f,
            Ease.Linear,
            onFirstCut: () =>
            {
                PlayTieScaleAnimation();
            },
            onSecondCut: () =>
            {
                PlayTieScaleAnimation();
                PlayDisappearAndOpenSequenceAsync().Forget();
            }).Forget();
    }

    private async UniTaskVoid PlayOpenAnimationAndDisableAsync()
    {
        _isLidOpening = true;
        RefreshCarriers();

        var animTasks = new List<UniTask>();
        if (giftBoxVisual1X != null && giftBoxVisual1X.gameObject.activeInHierarchy)
            animTasks.Add(giftBoxVisual1X.PlayOpenAnimationAsync());
        if (giftBoxVisual2X != null && giftBoxVisual2X.gameObject.activeInHierarchy)
            animTasks.Add(giftBoxVisual2X.PlayOpenAnimationAsync());
        if (giftBoxVisual3X != null && giftBoxVisual3X.gameObject.activeInHierarchy)
            animTasks.Add(giftBoxVisual3X.PlayOpenAnimationAsync());

        if (animTasks.Count > 0)
        {
            await UniTask.WhenAll(animTasks);
        }

        isOpen = true;
        _isOpening = false;
        _isLidOpening = false;
        _isAssignedToUnlock = false;
        RefreshVisualState();
        RefreshCarriers();
        GameEventBus.OnContainerUnlocked?.Invoke();
    }

    private bool CanOpenWith(EBlockColorType colorType) =>
        unlockColor != EBlockColorType.None && unlockColor == colorType;

    private void TryUnlock(int targetContainerId, EBlockColorType colorType)
    {
        if (containerId != targetContainerId || IsOpen || _isOpening || !CanOpenWith(colorType)) return;
        Open().Forget();
    }

    private void ApplyVisuals(EBlockColorType colorType)
    {
        var colorConfig = ConfigManager.Instance != null ? ConfigManager.Instance.GetColorConfig() : null;
        if (colorConfig != null)
        {
            var colorEntry = colorConfig.GetColorEntry(colorType);
            if (colorEntry != null)
            {
                if (particleSystems != null)
                {
                    foreach (var ps in particleSystems)
                    {
                        if (ps == null) continue;
                        var main = ps.main;
                        main.startColor = colorEntry.Color;
                    }
                }
            }
        }

        if (keyAnimator != null)
        {
            keyAnimator.SetKeyColor(colorType);
        }
    }

    public void Configure(int targetContainerId, EBlockColorType colorType)
    {
        containerId = targetContainerId;
        unlockColor = colorType;
        isOpen = false;
        _isOpening = false;
        _isLidOpening = false;
        _isAssignedToUnlock = false;
        carriers.Clear();

        ApplyVisuals(colorType);
        
        if (keyAnimator != null)
        {
            keyAnimator.SetActiveState(false);
        }

        if (ribbonParticle != null)
        {
            ribbonParticle.SetActive(false);
        }
        
        if (giftBoxVisual1X != null)
        {
            giftBoxVisual1X.ResetVisual();
            giftBoxVisual1X.ApplyColor(colorType);
        }
        if (giftBoxVisual2X != null)
        {
            giftBoxVisual2X.ResetVisual();
            giftBoxVisual2X.ApplyColor(colorType);
        }
        if (giftBoxVisual3X != null)
        {
            giftBoxVisual3X.ResetVisual();
            giftBoxVisual3X.ApplyColor(colorType);
        }
        
        RefreshVisualState();
    }

    public void ConfigureForPreview(int targetContainerId, EBlockColorType colorType) =>
        Configure(targetContainerId, colorType);

    public void SetOpenSilently()
    {
        isOpen = true;
        _isOpening = false;
        _isLidOpening = false;
        _isAssignedToUnlock = false;
        RefreshVisualState();
        RefreshCarriers();
    }

    public void AddCarrier(CarrierContainerMember carrier)
    {
        if (carrier == null || carriers.Contains(carrier)) return;
        carriers.Add(carrier);
        carrier.Bind(this);
        RefreshCarrier(carrier);
        UpdateGiftBoxVisuals();
    }

    private void BindExistingCarriers()
    {
        foreach (var carrier in carriers)
        {
            if (carrier == null) continue;
            carrier.Bind(this);
            RefreshCarrier(carrier);
        }
        UpdateGiftBoxVisuals();
    }

    private void RefreshCarriers()
    {
        foreach (var carrier in carriers)
            RefreshCarrier(carrier);
    }

    private static void RefreshCarrier(CarrierContainerMember carrier) =>
        carrier?.Carrier?.RefreshMechanicVisualState();

    private void RefreshVisualState()
    {
        SetRenderersVisible(!IsOpen);
        SetCollidersEnabled(!IsOpen);
    }

    private void SetRenderersVisible(bool isVisible)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var targetRenderer in renderers)
            if (targetRenderer != null)
                targetRenderer.enabled = isVisible;
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        foreach (var targetCollider in colliders)
            if (targetCollider != null)
                targetCollider.enabled = isEnabled;
    }

    public void UpdateGiftBoxVisuals()
    {
        int count = carriers != null ? carriers.Count : 0;
        int activeIndex = Mathf.Clamp(count, 1, 3);
        
        if (giftBoxVisual1X != null) giftBoxVisual1X.gameObject.SetActive(activeIndex == 1);
        if (giftBoxVisual2X != null) giftBoxVisual2X.gameObject.SetActive(activeIndex == 2);
        if (giftBoxVisual3X != null) giftBoxVisual3X.gameObject.SetActive(activeIndex == 3);
    }
}