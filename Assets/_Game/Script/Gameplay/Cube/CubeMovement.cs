using UnityEngine;
using System.Collections;

public class CubeMovement : MonoBehaviour, ICustomTimeScaleTarget
{
    [SerializeField] private CubeMovementConfigSO config;
    [SerializeField] private Rigidbody rb;

    private readonly int _splineSearchRange = 15;
    private static readonly RigidbodyConstraints ConveyorMovementConstraints =
        RigidbodyConstraints.FreezePositionY
        | RigidbodyConstraints.FreezeRotationX
        | RigidbodyConstraints.FreezeRotationY
        | RigidbodyConstraints.FreezeRotationZ;

    private ConveyorPathRuntime _path;
    private float _lastProgress;
    private float _startProgress;
    private float _nextRoadGripTime;
    private float _nextMovementTime;
    private float _movementTime;
    private float _customTimeScale = 1f;
    private float _speedBoost;
    private float _speedBoostRemainingDistance;
    private float _lockedSpeedMultiplier = 1f;
    private float _smoothedSpeed;
    private float _lastMovementForceTime;
    private float _accumulatedForwardProgress;
    private float _progressOffset;
    private int _completedLapCount;
    private int _setupVersion;
    private int _yAxisLockVersion;
    private int _speedMultiplierLockVersion;
    private bool _hasLoggedMissingConfig;
    private bool _hasStartedFirstLoop = true;
    private bool _isFirstSampleOnSpline = true;

    private void OnDisable()
    {
        _isFirstSampleOnSpline = true;
        _setupVersion++;
        _yAxisLockVersion++;
        _speedMultiplierLockVersion++;
        ResetVelocity();
        _lockedSpeedMultiplier = 1f;
        _smoothedSpeed = 0f;
        _lastMovementForceTime = 0f;
        SetSpawnYAxisLocked(false);
        SetPhysicsEnabled(false);
        _customTimeScale = 1f;
    }

    private void FixedUpdate()
    {
        if (!CanMove())
        {
            return;
        }

        if (_customTimeScale <= 0f)
        {
            ResetVelocity();
            return;
        }

        _movementTime += Time.fixedDeltaTime * _customTimeScale;

        if (!IsMovementStepReady())
        {
            return;
        }

        AdvanceAlongSpline();
    }

    public void Setup(ConveyorPathRuntime path, float startProgress = 0f, float progressOffset = 0f, Vector3? initialWorldPosition = null)
    {
        _isFirstSampleOnSpline = true;
        _setupVersion++;
        _path = path;
        var progress = ResolveInitialProgress(Mathf.Repeat(startProgress, 1f), initialWorldPosition);
        _progressOffset = progressOffset;
        PrepareForSplineMovement(progress, initialWorldPosition);
        LockYAxisTemporarily(config != null ? config.SpawnFreezeYDuration : 0.3f);
        EnablePhysicsNextFrameAsync(_setupVersion, progress);
    }

    public void ApplyPortalTransfer(ConveyorPathRuntime path, Vector3 worldPosition, Vector3 worldVelocity, Vector3 worldForward)
    {
        _isFirstSampleOnSpline = true;
        _path = path;
        ResetDelayedForces();
        SetPhysicsEnabled(true);

        if (rb != null)
        {
            rb.position = worldPosition;
            rb.velocity = worldVelocity;
        }

        transform.position = worldPosition;
        if (worldForward.sqrMagnitude > 0.001f)
        {
            transform.forward = worldForward.normalized;
        }
    }

    public bool HasCompletedLap()
    {
        return _completedLapCount > 0;
    }

    public bool HasStartedFirstLoop()
    {
        return _hasStartedFirstLoop;
    }

    public float GetProgress()
    {
        return _lastProgress;
    }

    public float GetRealtimeProgress()
    {
        if (_path == null || !_path.IsValid) return _lastProgress;

        var localPosition = _path.InverseTransformPoint(transform.position);
        float progress;
        if (_isFirstSampleOnSpline)
        {
            _path.GetNearestPointGlobal(localPosition, out _, out progress, out _);
        }
        else
        {
            _path.GetNearestPointLocal(localPosition, _lastProgress, _splineSearchRange, out _, out progress, out _);
        }
        return Mathf.Repeat(progress, 1f);
    }

    public void SyncProgress(float progress)
    {
        _isFirstSampleOnSpline = true;
        var normalizedProgress = Mathf.Repeat(progress, 1f);
        _lastProgress = normalizedProgress;
        _startProgress = normalizedProgress;
    }

    public void ApplyTemporarySpeedBoost(float extraSpeed, float distance)
    {
        if (extraSpeed <= 0f || distance <= 0f) return;
        _speedBoost = Mathf.Max(_speedBoost, extraSpeed);
        _speedBoostRemainingDistance = Mathf.Max(_speedBoostRemainingDistance, distance);
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _customTimeScale = Mathf.Max(0f, timeScale);
        if (_customTimeScale <= 0f) ResetVelocity();
    }

    private bool CanMove()
    {
        if (!HasValidConfig()) return false;
        return rb != null
               && !rb.isKinematic
               && _path != null
               && _path.IsValid;
    }

    private float GetSpeedTimeScale()
    {
        if (_customTimeScale <= 1f)
        {
            return _customTimeScale;
        }
        float multiplier = config != null ? config.ScaleSpeedMultiplier : 0.2f;
        return 1f + (_customTimeScale - 1f) * multiplier;
    }

    private bool IsMovementStepReady()
    {
        if (_movementTime < _nextMovementTime) return false;
        float interval = config.MovementInterval;
        float speedScale = GetSpeedTimeScale();
        if (speedScale > 1f)
        {
            interval /= speedScale;
        }
        _nextMovementTime = _movementTime + Mathf.Max(0.005f, interval);
        return true;
    }

    private void AdvanceAlongSpline()
    {
        if (!TryGetSplineSample(out var sample))
        {
            return;
        }

        UpdateCompletedLapCount(sample.Progress);
        ApplyMovementForce(sample.WorldTangent);
        ApplyRoadGripForce(sample.WorldNearestPoint, sample.WorldTangent);
    }

    private bool TryGetSplineSample(out SplineSample sample)
    {
        sample = default;
        if (_path == null || !_path.IsValid)
        {
            return false;
        }

        var localPosition = _path.InverseTransformPoint(transform.position);

        Vector3 nearestPoint;
        float progress;
        Vector3 tangent;

        if (_isFirstSampleOnSpline)
        {
            _isFirstSampleOnSpline = false;
            _path.GetNearestPointGlobal(localPosition, out nearestPoint, out progress, out tangent);
            _lastProgress = progress;
        }
        else
        {
            _path.GetNearestPointLocal(localPosition, _lastProgress, _splineSearchRange, out nearestPoint, out progress, out tangent);
        }

        var worldTangent = _path.TransformDirection(tangent).normalized;
        if (worldTangent.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        sample = new SplineSample(
            Mathf.Repeat(progress, 1f),
            _path.TransformPoint(nearestPoint),
            worldTangent);
        return true;
    }

    private void ApplyMovementForce(Vector3 worldTangent)
    {
        var targetSpeed = GetSmoothedSpeed();
        var desiredVelocity = worldTangent * targetSpeed;
        var forwardVelocity = Vector3.Dot(rb.velocity, worldTangent);
        var velocityDelta = desiredVelocity - rb.velocity;
        rb.AddForce(velocityDelta * config.Acceleration, ForceMode.Acceleration);
        ApplyStallAssist(worldTangent, targetSpeed, forwardVelocity);
        StabilizeForwardVelocity(worldTangent, targetSpeed, forwardVelocity);
    }

    private void ApplyStallAssist(Vector3 worldTangent, float targetSpeed, float forwardVelocity)
    {
        if (config.StallAssistAcceleration <= 0f || targetSpeed <= 0f) return;
        var assistThreshold = targetSpeed * Mathf.Clamp01(config.StallAssistSpeedRatio);
        if (forwardVelocity >= assistThreshold) return;
        var assistRatio = Mathf.InverseLerp(assistThreshold, 0f, Mathf.Max(0f, forwardVelocity));
        rb.AddForce(
            worldTangent * (config.StallAssistAcceleration * assistRatio),
            ForceMode.Acceleration);
    }

    private void StabilizeForwardVelocity(Vector3 worldTangent, float targetSpeed, float forwardVelocity)
    {
        if (targetSpeed <= 0f) return;

        var minForwardSpeed = targetSpeed * Mathf.Clamp01(config.MinimumForwardSpeedRatio);
        if (forwardVelocity < minForwardSpeed && config.ForwardSpeedRecovery > 0f)
        {
            var missingSpeed = minForwardSpeed - forwardVelocity;
            var correction = missingSpeed * config.ForwardSpeedRecovery;
            if (config.MaxForwardCorrection > 0f)
            {
                correction = Mathf.Min(correction, config.MaxForwardCorrection);
            }

            rb.AddForce(worldTangent * correction, ForceMode.Acceleration);
        }

        var maxForwardSpeed = targetSpeed * Mathf.Max(1f, config.MaximumForwardSpeedRatio);
        if (forwardVelocity > maxForwardSpeed && config.ForwardOverspeedDamping > 0f)
        {
            var overspeed = forwardVelocity - maxForwardSpeed;
            rb.AddForce(
                -worldTangent * (overspeed * config.ForwardOverspeedDamping),
                ForceMode.Acceleration);
        }
    }

    private float GetSmoothedSpeed()
    {
        var targetSpeed = GetTargetSpeed();
        var deltaTime = Mathf.Max(
            0.0001f,
            _movementTime - _lastMovementForceTime);
        _lastMovementForceTime = _movementTime;
        _smoothedSpeed = Mathf.MoveTowards(
            _smoothedSpeed,
            targetSpeed,
            config.Acceleration * deltaTime);
        return _smoothedSpeed;
    }

    private float GetTargetSpeed()
    {
        return (config.Speed + _speedBoost) * _lockedSpeedMultiplier * GetSpeedTimeScale();
    }

    private float ResolveInitialProgress(float fallbackProgress, Vector3? initialWorldPosition)
    {
        if (_path == null || !_path.IsValid || !initialWorldPosition.HasValue)
        {
            return fallbackProgress;
        }

        var localPosition = _path.InverseTransformPoint(initialWorldPosition.Value);
        _path.GetNearestPointGlobal(localPosition, out _, out var resolvedProgress, out _);
        return Mathf.Repeat(resolvedProgress, 1f);
    }

    private void ApplyRoadGripForce(Vector3 worldNearestPoint, Vector3 worldTangent)
    {
        if (_movementTime < _nextRoadGripTime) return;
        var offsetToSpline = Vector3.ProjectOnPlane(worldNearestPoint - transform.position, worldTangent);
        ApplyRoadBoundaryAssist(worldNearestPoint, worldTangent, offsetToSpline);
        if (offsetToSpline.sqrMagnitude <= 0.000001f) return;
        rb.AddForce(offsetToSpline * config.RoadGripForce, ForceMode.Acceleration);
    }

    private void ApplyRoadBoundaryAssist(
        Vector3 worldNearestPoint,
        Vector3 worldTangent,
        Vector3 offsetToSpline)
    {
        var offsetDistance = offsetToSpline.magnitude;
        if (offsetDistance <= 0.0001f) return;

        var directionToCenter = offsetToSpline / offsetDistance;
        var forwardVelocity = Vector3.Dot(rb.velocity, worldTangent);
        var lateralVelocity = rb.velocity - worldTangent * forwardVelocity;

        var maxOffset = Mathf.Max(0.1f, config.RoadMaxOffset);
        var dampingStart = maxOffset * 0.35f;
        if (config.RoadLateralDamping > 0f && offsetDistance > dampingStart)
        {
            var dampingRatio = Mathf.InverseLerp(
                dampingStart,
                maxOffset + Mathf.Max(0f, config.RoadHardClampPadding),
                offsetDistance);
            rb.AddForce(
                -lateralVelocity * (config.RoadLateralDamping * dampingRatio),
                ForceMode.Acceleration);
        }

        if (offsetDistance <= maxOffset) return;

        var overshoot = offsetDistance - maxOffset;
        if (config.RoadBoundaryForce > 0f)
        {
            rb.AddForce(directionToCenter * overshoot * config.RoadBoundaryForce, ForceMode.Acceleration);
        }

        if (offsetDistance <= maxOffset + config.RoadHardClampPadding) return;

        var clampedPosition = worldNearestPoint - directionToCenter * maxOffset;
        rb.position = clampedPosition;
        transform.position = clampedPosition;

        var outwardDirection = -directionToCenter;
        var outwardSpeed = Vector3.Dot(rb.velocity, outwardDirection);
        if (outwardSpeed > 0f)
        {
            rb.velocity -= outwardDirection * outwardSpeed;
        }
    }

    private void ResetVelocity()
    {
        if (rb == null) return;
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private float GetForwardProgressDelta(float fromProgress, float toProgress)
    {
        return Mathf.Repeat(toProgress - fromProgress, 1f);
    }

    private void UpdateCompletedLapCount(float currentProgress)
    {
        ConsumeSpeedBoostDistance(currentProgress);

        float diff = currentProgress - _lastProgress;
        if (diff > 0.5f) diff -= 1f;
        else if (diff < -0.5f) diff += 1f;

        _accumulatedForwardProgress += diff;
        _accumulatedForwardProgress = Mathf.Max(_accumulatedForwardProgress, -1f);

        if (_accumulatedForwardProgress < -0.01f)
        {
            _hasStartedFirstLoop = false;
        }
        else if (_accumulatedForwardProgress >= 0f && !_hasStartedFirstLoop)
        {
            _hasStartedFirstLoop = true;
        }

        if (_accumulatedForwardProgress >= 0.8f)
        {
            _completedLapCount = 1;
        }

        _lastProgress = currentProgress;
    }

    private void ConsumeSpeedBoostDistance(float currentProgress)
    {
        if (_speedBoostRemainingDistance <= 0f) return;
        _speedBoostRemainingDistance -= GetForwardProgressDelta(_lastProgress, currentProgress);
        if (_speedBoostRemainingDistance > 0f) return;
        _speedBoostRemainingDistance = 0f;
        _speedBoost = 0f;
    }

    private void ResetDelayedForces()
    {
        _nextRoadGripTime = _movementTime + Mathf.Max(0f, config.RoadGripDelay);
        _nextMovementTime = _movementTime + Mathf.Max(0f, config.MovementInterval);
    }

    private void PrepareForSplineMovement(float progress, Vector3? initialWorldPosition)
    {
        ResetVelocity();
        SetPhysicsEnabled(false);
        ResetProgressTracking(progress);
        ResetDelayedForces();
        SnapToSpline(progress, initialWorldPosition);
    }

    private void ResetProgressTracking(float progress)
    {
        _startProgress = progress;
        _lastProgress = progress;
        _movementTime = 0f;
        _lastMovementForceTime = 0f;
        _speedBoost = 0f;
        _speedBoostRemainingDistance = 0f;
        _smoothedSpeed = config != null ? config.Speed : 0f;
        _completedLapCount = 0;
        _accumulatedForwardProgress = 0f;
        _hasStartedFirstLoop = true;
        _customTimeScale = CustomTimeScaleGroup.Instance != null ? CustomTimeScaleGroup.Instance.CurrentTimeScale : 1f;
    }

    private void SnapToSpline(float progress, Vector3? initialWorldPosition)
    {
        var worldPos = initialWorldPosition ?? GetSplineWorldPosition(progress);
        transform.position = worldPos;

        if (rb != null)
        {
            rb.position = worldPos;
        }
    }

    private Vector3 GetSplineWorldPosition(float progress)
    {
        return _path != null && _path.IsValid ? _path.EvaluateWorldPosition(progress) : transform.position;
    }

    private void EnablePhysicsNextFrameAsync(int setupVersion, float progress)
    {
        if (setupVersion != _setupVersion) return;
        SetPhysicsEnabled(true);
        ApplySpawnPush(progress);
    }

    public void LockYAxisTemporarily(float duration, float speedMultiplier = 1f, float speedMultiplierDuration = -1f)
    {
        if (rb == null) return;

        if (duration > 0f)
            StartCoroutine(ApplyYAxisLockRoutine(++_yAxisLockVersion, duration));

        float actualSpeedMultiplierDuration = speedMultiplierDuration < 0f ? duration : speedMultiplierDuration;
        if (actualSpeedMultiplierDuration > 0f && speedMultiplier < 0.999f)
        {
            StartCoroutine(ApplySpeedMultiplierRoutine(
                ++_speedMultiplierLockVersion,
                actualSpeedMultiplierDuration,
                Mathf.Clamp01(speedMultiplier)));
        }
    }

    private IEnumerator ApplyYAxisLockRoutine(int lockVersion, float duration)
    {
        if (rb == null) yield break;
        SetSpawnYAxisLocked(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime * _customTimeScale;
        }

        if (lockVersion != _yAxisLockVersion || rb == null) yield break;
        SetSpawnYAxisLocked(false);
    }

    private IEnumerator ApplySpeedMultiplierRoutine(int lockVersion, float duration, float speedMultiplier)
    {
        _lockedSpeedMultiplier = speedMultiplier;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime * _customTimeScale;
        }

        if (lockVersion != _speedMultiplierLockVersion) yield break;
        _lockedSpeedMultiplier = 1f;
    }

    private void SetSpawnYAxisLocked(bool isLocked)
    {
        if (rb == null) return;

        if (isLocked)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            return;
        }

        if (!rb.isKinematic)
        {
            rb.constraints |= ConveyorMovementConstraints;
            return;
        }

        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
    }

    private void ApplySpawnPush(float progress)
    {
        if (rb == null || rb.isKinematic || config.SpawnPushSpeed <= 0f) return;
        var tangent = GetSplineWorldTangent(progress);
        if (tangent.sqrMagnitude <= 0.000001f) return;
        rb.velocity = tangent * config.SpawnPushSpeed;
    }

    private void SetPhysicsEnabled(bool isEnabled)
    {
        if (rb == null) return;
        rb.constraints = isEnabled ? ConveyorMovementConstraints : RigidbodyConstraints.None;
        rb.isKinematic = !isEnabled;
        rb.detectCollisions = isEnabled;
    }

    private Vector3 GetSplineWorldTangent(float progress)
    {
        return _path != null && _path.IsValid ? _path.EvaluateWorldTangent(progress) : Vector3.forward;
    }

    private bool HasValidConfig()
    {
        if (config != null)
        {
            _hasLoggedMissingConfig = false;
            return true;
        }

        if (_hasLoggedMissingConfig) return false;
        Debug.LogError($"[{nameof(CubeMovement)}] Chua gan {nameof(CubeMovementConfigSO)} tren object {name}.", this);
        _hasLoggedMissingConfig = true;
        return false;
    }

    private struct SplineSample
    {
        public readonly float Progress;
        public readonly Vector3 WorldNearestPoint;
        public readonly Vector3 WorldTangent;

        public SplineSample(float progress, Vector3 worldNearestPoint, Vector3 worldTangent)
        {
            Progress = progress;
            WorldNearestPoint = worldNearestPoint;
            WorldTangent = worldTangent;
        }
    }
}
