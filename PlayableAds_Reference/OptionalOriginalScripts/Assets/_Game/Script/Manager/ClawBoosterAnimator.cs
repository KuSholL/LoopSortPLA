using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class ClawBoosterAnimator : MonoBehaviour
{
    private const string TakeStateName = "ClawMachine_Take";
    private const string TakeIdleStateName = "ClawMachine_TakeIdle";
    private const string DropStateName = "ClawMachine_Drop";
    private static readonly string[] HighlightLayerNames =
        { "HighlightSlime1x", "HighlightSlime2x", "HighlightSlime3x", "HighlightSlime4x" };

    [SerializeField] private ClawBoosterAnimatorData config;
    [SerializeField] private Transform mainTrans;
    [SerializeField] private Transform bodyTrans;
    [SerializeField] private Block singleBlock;
    [SerializeField] private LinkedBlockVisual block2X;
    [SerializeField] private LinkedBlockVisual block3X;

    // References for the claw bones to animate via LitMotion
    [SerializeField] private Transform rootTrans;
    [SerializeField] private Transform clawL;
    [SerializeField] private Transform clawR;

    private MotionHandle _positionHandle;
    private MotionHandle _rotationHandle;
    private Quaternion _topScreenRotation;
    private Quaternion _bodyDefaultLocalRotation;

    // LitMotion animation variables
    private MotionHandle _takeIdleHandle;
    private CancellationTokenSource _animCts;

    // Cached default values to ensure robust relative animation
    private Vector3 _rootDefaultLocalPos;
    
    // Caching block scale to restore it on animation cancellation/completion
    private Transform _cachedActiveBlock;
    private Vector3 _originalBlockScale;

    private void Awake()
    {
        if (rootTrans != null) _rootDefaultLocalPos = rootTrans.localPosition;
        _topScreenRotation = (bodyTrans != null ? bodyTrans : (mainTrans != null ? mainTrans : transform)).rotation;
        if (bodyTrans != null) _bodyDefaultLocalRotation = bodyTrans.localRotation;
        ClearCarriedVisuals();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        CancelCurrentAnimation();
        StopTakeIdle();
        RestoreCachedBlockScale();
    }

    private void CancelCurrentAnimation()
    {
        if (_animCts != null)
        {
            _animCts.Cancel();
            _animCts.Dispose();
            _animCts = null;
        }
    }

    private void StopTakeIdle()
    {
        if (_takeIdleHandle.IsActive())
        {
            _takeIdleHandle.Cancel();
        }
    }

    private void CacheBlockScale(Transform activeBlock)
    {
        RestoreCachedBlockScale();
        if (activeBlock != null)
        {
            _cachedActiveBlock = activeBlock;
            _originalBlockScale = activeBlock.localScale;
        }
    }

    private void RestoreCachedBlockScale()
    {
        if (_cachedActiveBlock != null)
        {
            _cachedActiveBlock.localScale = _originalBlockScale;
            _cachedActiveBlock = null;
        }
    }

    public void SetRootYOffset(float offset)
    {
        if (rootTrans != null)
        {
            var pos = rootTrans.localPosition;
            pos.y = _rootDefaultLocalPos.y + offset;
            rootTrans.localPosition = pos;
        }
    }

    public void SetClawsZ(float zRotation)
    {
        if (clawL != null)
        {
            var rot = clawL.localEulerAngles;
            clawL.localRotation = Quaternion.Euler(rot.x, rot.y, zRotation);
        }
        if (clawR != null)
        {
            var rot = clawR.localEulerAngles;
            rot.z = -zRotation;
            clawR.localRotation = Quaternion.Euler(rot.x, rot.y, -zRotation);
        }
    }
    
    private Vector3 GetTopScreenWorldPosition(Vector3? targetWorldPos = null)
    {
        var cam = CameraManager.Instance != null ? CameraManager.Instance.MainCamera : Camera.main;
        if (cam == null) return mainTrans != null ? mainTrans.position : transform.position;

        var target = mainTrans != null ? mainTrans : transform;
        if (targetWorldPos.HasValue)
        {
            var targetScreenPoint = cam.WorldToScreenPoint(targetWorldPos.Value);
            var topScreenPoint = new Vector3(targetScreenPoint.x, Screen.height, targetScreenPoint.z);
            return cam.ScreenToWorldPoint(topScreenPoint);
        }
        else
        {
            var depth = cam.WorldToScreenPoint(target.position).z;
            var topScreenPoint = new Vector3(Screen.width * 0.5f, Screen.height, depth);
            return cam.ScreenToWorldPoint(topScreenPoint);
        }
    }

    private Vector3 GetAdjustedTargetPosition(
        Vector3 targetWorldPosition,
        Quaternion carrierRotation,
        Vector3 localOffset)
    {
        return targetWorldPosition + carrierRotation * localOffset;
    }

    public async UniTask MoveMainFromTopScreenToWorldPosition(
        Vector3 targetWorldPosition,
        Quaternion carrierRotation,
        Action onComplete,
        Vector3 localOffset)
    {
        gameObject.SetActive(true);
        ResetDefaultState();
        var target = mainTrans != null ? mainTrans : transform;
        targetWorldPosition = GetAdjustedTargetPosition(targetWorldPosition, carrierRotation, localOffset);
        var startPosition = GetTopScreenWorldPosition(targetWorldPosition);
        target.position = startPosition;
        
        if (bodyTrans != null) bodyTrans.localRotation = _bodyDefaultLocalRotation;
        else target.rotation = _topScreenRotation;

        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_down);
        await MoveMain(
            target,
            startPosition,
            targetWorldPosition,
            GetDuration(config != null ? config.TopToSourceMoveDuration : 0f, config != null ? config.MoveDuration : 0f),
            config != null ? config.TopToSourceMoveEase : Ease.Linear);

        var rotateTarget = bodyTrans != null ? bodyTrans : target;
        if (WillRotate(rotateTarget, rotateTarget.rotation, carrierRotation))
        {
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_turn);
        }
        await RotateMain(
            rotateTarget,
            rotateTarget.rotation,
            carrierRotation,
            GetDuration(config != null ? config.SourceRotateDuration : 0f, config != null ? config.RotateDuration : 0f),
            config != null ? config.SourceRotateEase : Ease.Linear);
        PlayAnimatorState(TakeStateName, 0f);
        var stateLength = GetStateLength(TakeStateName);
        var grabTime = 0.36666667f; // Frame 11 time (11 / 30s)

        await UniTask.Delay(
            TimeSpan.FromSeconds(grabTime),
            Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());

        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_grab);
        onComplete?.Invoke();

        var remainingTime = Mathf.Max(0f, stateLength - grabTime);
        if (remainingTime > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(remainingTime),
                Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy());
        }

        PlayAnimatorState(TakeIdleStateName);
    }

    public async UniTask MoveMainToWorldPosition(
        Vector3 targetWorldPosition,
        Quaternion carrierRotation,
        Action onComplete,
        Vector3 localOffset)
    {
        gameObject.SetActive(true);
        var target = mainTrans != null ? mainTrans : transform;
        targetWorldPosition = GetAdjustedTargetPosition(targetWorldPosition, carrierRotation, localOffset);
        var startPosition = target.position;
        var startRotation = (bodyTrans != null ? bodyTrans : target).rotation;

        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_movement);
        await UniTask.WhenAll(
            MoveMain(
                target,
                startPosition,
                targetWorldPosition,
                GetDuration(config != null ? config.SourceToTargetMoveDuration : 0f, config != null ? config.MoveDuration : 0f),
                config != null ? config.SourceToTargetMoveEase : Ease.Linear),
            RotateMain(
                bodyTrans != null ? bodyTrans : target,
                startRotation,
                carrierRotation,
                GetDuration(config != null ? config.SourceToTargetRotateDuration : 0f, config != null ? config.RotateDuration : 0f),
                config != null ? config.SourceToTargetRotateEase : Ease.Linear));
        PlayAnimatorState(DropStateName, 0f);
        var stateLength = GetStateLength(DropStateName);
        var dropTime = 0.13333334f; // Frame 4 time (4 / 30s)

        await UniTask.Delay(
            TimeSpan.FromSeconds(dropTime),
            Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());

        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_drop);
        onComplete?.Invoke();
        ClearCarriedVisuals();

        var remainingTime = Mathf.Max(0f, stateLength - dropTime);
        if (remainingTime > 0f)
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(remainingTime),
                Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                this.GetCancellationTokenOnDestroy());
        }

        PlayAnimatorState(TakeIdleStateName);
        
        var rotationTarget = bodyTrans != null ? bodyTrans : target;

        if (WillRotate(rotationTarget, rotationTarget.rotation, _topScreenRotation))
        {
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_turn);
        }
        await RotateMain(
            rotationTarget,
            rotationTarget.rotation,
            _topScreenRotation,
            GetDuration(config != null ? config.ReturnRotateDuration : 0f, config != null ? config.RotateDuration : 0f),
            config != null ? config.ReturnRotateEase : Ease.Linear);

        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_claw_down);
        await MoveMain(
            target,
            target.position,
            GetTopScreenWorldPosition(targetWorldPosition),
            GetDuration(config != null ? config.ReturnMoveDuration : 0f, config != null ? config.MoveDuration : 0f),
            config != null ? config.ReturnMoveEase : Ease.Linear);
        gameObject.SetActive(false);
    }

    private async UniTask MoveMain(
        Transform target,
        Vector3 startPosition,
        Vector3 targetWorldPosition,
        float duration,
        Ease ease)
    {
        if (target == null || startPosition == targetWorldPosition)
        {
            if (target != null) target.position = targetWorldPosition;
            return;
        }

        _positionHandle = LMotion.Create(startPosition, targetWorldPosition, duration)
            .WithEase(ease)
            .BindToPosition(target);
        await _positionHandle.ToUniTask();
    }

    private async UniTask RotateMain(
        Transform target,
        Quaternion startRotation,
        Quaternion targetRotation,
        float duration,
        Ease ease)
    {
        if (target == null) return;

        Quaternion localStartRot = (target.parent != null)
            ? (Quaternion.Inverse(target.parent.rotation) * startRotation)
            : startRotation;
        Quaternion localTargetRot = (target.parent != null)
            ? (Quaternion.Inverse(target.parent.rotation) * targetRotation)
            : targetRotation;

        var startEuler = localStartRot.eulerAngles;
        var targetEuler = localTargetRot.eulerAngles;

        var shortestDeltaY = Mathf.DeltaAngle(startEuler.y, targetEuler.y);
        var finalY = startEuler.y + shortestDeltaY;

        var currentLocalEuler = target.localEulerAngles;
        var defaultX = currentLocalEuler.x;
        var defaultZ = currentLocalEuler.z;

        var absDelta = Mathf.Abs(shortestDeltaY);
        if (absDelta < 1f || Mathf.Abs(absDelta - 180f) < 1f)
        {
            target.localRotation = Quaternion.Euler(defaultX, targetEuler.y, defaultZ);
            return;
        }

        _rotationHandle = LMotion.Create(startEuler.y, finalY, duration)
            .WithEase(ease)
            .Bind(y =>
            {
                target.localRotation = Quaternion.Euler(defaultX, y, defaultZ);
            });

        await _rotationHandle.ToUniTask();
    }

    private bool WillRotate(Transform target, Quaternion startRotation, Quaternion targetRotation)
    {
        if (target == null) return false;
        Quaternion localStartRot = (target.parent != null)
            ? (Quaternion.Inverse(target.parent.rotation) * startRotation)
            : startRotation;
        Quaternion localTargetRot = (target.parent != null)
            ? (Quaternion.Inverse(target.parent.rotation) * targetRotation)
            : targetRotation;
        var shortestDeltaY = Mathf.DeltaAngle(localStartRot.eulerAngles.y, localTargetRot.eulerAngles.y);
        var absDelta = Mathf.Abs(shortestDeltaY);
        return !(absDelta < 1f || Mathf.Abs(absDelta - 180f) < 1f);
    }

    public void ShowAtTopScreen()
    {
        gameObject.SetActive(true);
        mainTrans.position = GetTopScreenWorldPosition();
        PlayAnimatorState(TakeStateName, 0f, 0f);
    }

    public void RestoreMainLayers()
    {
        ClearCarriedVisuals();
        gameObject.SetActive(false);
    }

    public void ShowCarriedBlock(Block block, BlockRuntimeData runtimeData)
    {
        if (singleBlock == null || runtimeData == null) return;
        ClearCarriedVisuals();
        ApplyHighlightLayer(singleBlock, 1);
        singleBlock.gameObject.SetActive(true);
        singleBlock.SetHiddenForClawBooster(false);
        singleBlock.ApplyRuntimeData(runtimeData);
    }

    public void ShowCarriedLinkedVisual(LinkedBlockVisual linkedVisual, EBlockColorType colorType, int blockSize)
    {
        var targetVisual = GetPresetLinkedVisual(blockSize);
        if (targetVisual == null) return;

        ClearCarriedVisuals();
        ApplyHighlightLayer(targetVisual, blockSize);
        targetVisual.gameObject.SetActive(true);
        targetVisual.Apply(GetColorEntry(colorType), GetCatColorEntry(colorType));
    }

    private void ResetDefaultState()
    {
        _positionHandle.TryCancel();
        _rotationHandle.TryCancel();
        PlayAnimatorState(TakeStateName, 0f, 0f);
    }

    private LinkedBlockVisual GetPresetLinkedVisual(int blockSize)
    {
        return blockSize switch
        {
            2 => block2X,
            3 => block3X,
            _ => null
        };
    }

    private void ClearCarriedVisuals()
    {
        if (singleBlock != null) singleBlock.gameObject.SetActive(false);
        if (block2X != null)
        {
            block2X.SetVisible(false);
            block2X.gameObject.SetActive(false);
        }

        if (block3X != null)
        {
            block3X.SetVisible(false);
            block3X.gameObject.SetActive(false);
        }
    }

    private static ColorEntry GetColorEntry(EBlockColorType colorType)
    {
        var colorConfig = ConfigManager.Instance ? ConfigManager.Instance.GetColorConfig() : null;
        return colorConfig ? colorConfig.GetColorEntry(colorType) : null;
    }

    private static CatColorEntry GetCatColorEntry(EBlockColorType colorType)
    {
        return ConfigManager.Instance != null ? ConfigManager.Instance.GetCatColorEntryByType(colorType) : null;
    }

    private static void ApplyHighlightLayer(Block block, int size)
    {
        var layer = GetHighlightLayer(size);
        if (block == null || layer < 0) return;
        block.SetLayer(layer);
    }

    private static void ApplyHighlightLayer(LinkedBlockVisual visual, int size)
    {
        var layer = GetHighlightLayer(size);
        if (visual == null || layer < 0) return;
        visual.SetLayer(layer);
    }

    private static int GetHighlightLayer(int size)
    {
        if (size < 1 || size > HighlightLayerNames.Length) return -1;
        return LayerMask.NameToLayer(HighlightLayerNames[size - 1]);
    }

    private static float GetDuration(float value, float fallback)
    {
        return value > 0f ? value : fallback;
    }

    private Transform GetActiveCarriedBlock()
    {
        if (singleBlock != null && singleBlock.gameObject.activeSelf) return singleBlock.transform;
        if (block2X != null && block2X.gameObject.activeSelf) return block2X.transform;
        if (block3X != null && block3X.gameObject.activeSelf) return block3X.transform;
        return null;
    }

    private void PlayAnimatorState(string stateName, float normalizedTime = 0f, float speed = 1f)
    {
        Play(stateName, GetActiveCarriedBlock(), speed);
    }

    private float GetStateLength(string stateName)
    {
        return stateName switch
        {
            "ClawMachine_Take" => 0.6666667f,
            "ClawMachine_TakeIdle" => 0.46666667f,
            "ClawMachine_Drop" => 1f,
            "ClawMachine_Idle" => 1f,
            _ => 0f
        };
    }

    // Direct local Play implementation replacing ClawMachineLMotionAnimator
    public void Play(string stateName, Transform activeBlock, float speed = 1f)
    {
        CancelCurrentAnimation();
        StopTakeIdle();
        
        CacheBlockScale(activeBlock);

        // If speed is 0 (i.e. paused or resetting state), just set the initial pose and return
        if (speed == 0f)
        {
            ApplyStateInitialPose(stateName);
            return;
        }

        _animCts = new CancellationTokenSource();
        var token = _animCts.Token;

        switch (stateName)
        {
            case "ClawMachine_Take":
                PlayTakeAsync(activeBlock, token).Forget();
                break;
            case "ClawMachine_TakeIdle":
                PlayTakeIdle(activeBlock);
                break;
            case "ClawMachine_Drop":
                PlayDropAsync(activeBlock, token).Forget();
                break;
            case "ClawMachine_Idle":
                PlayIdle();
                break;
        }
    }

    private void ApplyStateInitialPose(string stateName)
    {
        switch (stateName)
        {
            case "ClawMachine_Take":
            case "ClawMachine_Idle":
                SetRootYOffset(0f);
                SetClawsZ(0f);
                break;
            case "ClawMachine_TakeIdle":
                SetRootYOffset(0f);
                SetClawsZ(30f);
                break;
            case "ClawMachine_Drop":
                SetRootYOffset(1.29f);
                SetClawsZ(30f);
                break;
        }
    }

    private void PlayIdle()
    {
        SetRootYOffset(0f);
        SetClawsZ(0f);
    }

    private async UniTask PlayTakeAsync(Transform activeBlock, CancellationToken token)
    {
        try
        {
            // Reset to initial
            SetRootYOffset(0f);
            SetClawsZ(0f);

            // Phase 1: 0.0s -> 0.23333333s (duration: 0.23333333f)
            var rootTween1 = LMotion.Create(0f, 2.04f, 0.23333333f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween1 = LMotion.Create(0f, 50f, 0.23333333f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);
            await UniTask.WhenAll(rootTween1.ToUniTask(token), clawTween1.ToUniTask(token));

            // Phase 2: 0.23333333s -> 0.36666667s (duration: 0.13333334f)
            var rootTween2 = LMotion.Create(2.04f, -1.6866717f, 0.13333334f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween2 = LMotion.Create(50f, 30f, 0.13333334f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);
            await UniTask.WhenAll(rootTween2.ToUniTask(token), clawTween2.ToUniTask(token));

            // Phase 3: 0.36666667s -> 0.43333334s (duration: 0.06666667f)
            var rootTween3 = LMotion.Create(-1.6866717f, 1.6743507f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween3 = LMotion.Create(30f, 25f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);

            MotionHandle blockScaleTween3 = default;
            if (activeBlock != null)
            {
                blockScaleTween3 = LMotion.Create(_originalBlockScale.x, _originalBlockScale.x * (1.2651193f / 1.391573f), 0.06666667f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(x => activeBlock.localScale = new Vector3(x, _originalBlockScale.y, _originalBlockScale.z));
            }
            await UniTask.WhenAll(
                rootTween3.ToUniTask(token), 
                clawTween3.ToUniTask(token),
                blockScaleTween3.IsActive() ? blockScaleTween3.ToUniTask(token) : UniTask.CompletedTask
            );

            // Phase 4: 0.43333334s -> 0.5s (duration: 0.06666667f)
            var rootTween4 = LMotion.Create(1.6743507f, 1.29f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween4 = LMotion.Create(25f, 30f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);

            MotionHandle blockScaleTween4 = default;
            if (activeBlock != null)
            {
                blockScaleTween4 = LMotion.Create(activeBlock.localScale.x, _originalBlockScale.x, 0.06666667f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(x => activeBlock.localScale = new Vector3(x, _originalBlockScale.y, _originalBlockScale.z));
            }
            await UniTask.WhenAll(
                rootTween4.ToUniTask(token), 
                clawTween4.ToUniTask(token),
                blockScaleTween4.IsActive() ? blockScaleTween4.ToUniTask(token) : UniTask.CompletedTask
            );

            // Phase 5: 0.5s -> 0.6666667s (duration: 0.1666667f)
            var rootTween5 = LMotion.Create(1.29f, 0f, 0.1666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            await rootTween5.ToUniTask(token);
        }
        catch (OperationCanceledException) { }
    }

    private void PlayTakeIdle(Transform activeBlock)
    {
        SetRootYOffset(0f);
        SetClawsZ(30f);

        if (activeBlock != null)
        {
            Vector3 targetScale = new Vector3(_originalBlockScale.x, _originalBlockScale.y * (1.3421651f / 1.391573f), _originalBlockScale.z * (1.4463731f / 1.391573f));
            
            _takeIdleHandle = LMotion.Create(_originalBlockScale, targetScale, 0.23333333f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithEase(Ease.InOutQuad)
                .WithLoops(-1, LoopType.Yoyo)
                .Bind(scale => activeBlock.localScale = scale);
        }
    }

    private async UniTask PlayDropAsync(Transform activeBlock, CancellationToken token)
    {
        try
        {
            SetRootYOffset(1.29f);
            SetClawsZ(30f);

            // Phase 1: 0.0s -> 0.06666667s (duration: 0.06666667f)
            float midRootYOffset = (1.29f + (-1.6866717f)) * 0.5f;
            var rootTween1 = LMotion.Create(1.29f, midRootYOffset, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween1 = LMotion.Create(30f, 25f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);

            MotionHandle blockScaleTween1 = default;
            if (activeBlock != null)
            {
                blockScaleTween1 = LMotion.Create(_originalBlockScale.x, _originalBlockScale.x * (1.2651193f / 1.391573f), 0.06666667f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(x => activeBlock.localScale = new Vector3(x, _originalBlockScale.y, _originalBlockScale.z));
            }
            await UniTask.WhenAll(
                rootTween1.ToUniTask(token), 
                clawTween1.ToUniTask(token),
                blockScaleTween1.IsActive() ? blockScaleTween1.ToUniTask(token) : UniTask.CompletedTask
            );

            // Phase 2: 0.06666667s -> 0.13333334s (duration: 0.06666667f)
            var rootTween2 = LMotion.Create(midRootYOffset, -1.6866717f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween2 = LMotion.Create(25f, 30f, 0.06666667f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);

            MotionHandle blockScaleTween2 = default;
            if (activeBlock != null)
            {
                blockScaleTween2 = LMotion.Create(activeBlock.localScale.x, _originalBlockScale.x, 0.06666667f)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                    .Bind(x => activeBlock.localScale = new Vector3(x, _originalBlockScale.y, _originalBlockScale.z));
            }
            await UniTask.WhenAll(
                rootTween2.ToUniTask(token), 
                clawTween2.ToUniTask(token),
                blockScaleTween2.IsActive() ? blockScaleTween2.ToUniTask(token) : UniTask.CompletedTask
            );

            // Phase 3: 0.13333334s -> 0.26666668s (duration: 0.13333334f)
            var rootTween3 = LMotion.Create(-1.6866717f, 2.04f, 0.13333334f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween3 = LMotion.Create(30f, 50f, 0.13333334f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);
            await UniTask.WhenAll(rootTween3.ToUniTask(token), clawTween3.ToUniTask(token));

            // Phase 4: 0.26666668s -> 0.5s (duration: 0.23333332f)
            var rootTween4 = LMotion.Create(2.04f, 1.29f, 0.23333332f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetRootYOffset);
            var clawTween4 = LMotion.Create(50f, 30f, 0.23333332f)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetClawsZ);
            await UniTask.WhenAll(rootTween4.ToUniTask(token), clawTween4.ToUniTask(token));

            // Phase 5: 0.5s -> 1.0s (duration: 0.5f)
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), delayType: Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime, cancellationToken: token);
        }
        catch (OperationCanceledException) { }
    }
}

[Serializable]
public class ClawBoosterAnimatorData
{
    public float TopToSourceMoveDuration = 0.3f;
    public Ease TopToSourceMoveEase = Ease.Linear;
    public float SourceRotateDuration = 0.2f;
    public Ease SourceRotateEase = Ease.Linear;
    public float SourceToTargetMoveDuration = 0.3f;
    public Ease SourceToTargetMoveEase = Ease.Linear;
    public float SourceToTargetRotateDuration = 0.3f;
    public Ease SourceToTargetRotateEase = Ease.Linear;
    public float ReturnRotateDuration = 0.2f;
    public Ease ReturnRotateEase = Ease.Linear;
    public float ReturnMoveDuration = 0.3f;
    public Ease ReturnMoveEase = Ease.Linear;
    public float MoveDuration = 0.3f;
    public Ease MoveEase = Ease.Linear;
    public float RotateDuration = 0.3f;
    public Ease RotateEase = Ease.Linear;
    public float BlockOffset;
}
