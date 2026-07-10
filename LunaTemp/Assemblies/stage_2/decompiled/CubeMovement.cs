using System.Collections;
using UnityEngine;

public class CubeMovement : MonoBehaviour, ICustomTimeScaleTarget
{
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

	[SerializeField]
	private CubeMovementConfigSO config;

	[SerializeField]
	private Rigidbody rb;

	private readonly int _splineSearchRange = 15;

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

	private bool _lunaManualMode;

	private void OnDisable()
	{
		if (!_lunaManualMode)
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
	}

	private void Update()
	{
		_movementTime += Time.unscaledDeltaTime * _customTimeScale;
	}

	private void FixedUpdate()
	{
		if (CanMove())
		{
			if (_customTimeScale <= 0f)
			{
				ResetVelocity();
			}
			else
			{
				ManualUpdate(Time.unscaledDeltaTime);
			}
		}
	}

	public void Setup(ConveyorPathRuntime path, float startProgress = 0f, float progressOffset = 0f, Vector3? initialWorldPosition = null)
	{
		_isFirstSampleOnSpline = true;
		_setupVersion++;
		_path = path;
		float progress = Mathf.Repeat(startProgress, 1f);
		_progressOffset = progressOffset;
		PrepareForSplineMovement(progress, initialWorldPosition);
		_lunaManualMode = true;
		SetPhysicsEnabled(false);
		base.enabled = false;
	}

	public void ApplyPortalTransfer(ConveyorPathRuntime path, Vector3 worldPosition, Vector3 worldVelocity, Vector3 worldForward)
	{
		_isFirstSampleOnSpline = true;
		_path = path;
		ResetDelayedForces();
		_lunaManualMode = true;
		SetPhysicsEnabled(false);
		base.transform.position = worldPosition;
		if (worldForward.sqrMagnitude > 0.001f)
		{
			base.transform.forward = worldForward.normalized;
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
		if (_path == null || !_path.IsValid)
		{
			return _lastProgress;
		}
		Vector3 localPosition = _path.InverseTransformPoint(base.transform.position);
		Vector3 nearestPoint;
		float progress;
		Vector3 tangent;
		if (_isFirstSampleOnSpline)
		{
			_path.GetNearestPointGlobal(localPosition, out nearestPoint, out progress, out tangent);
		}
		else
		{
			_path.GetNearestPointLocal(localPosition, _lastProgress, _splineSearchRange, out tangent, out progress, out nearestPoint);
		}
		return Mathf.Repeat(progress, 1f);
	}

	public void SyncProgress(float progress)
	{
		_isFirstSampleOnSpline = true;
		_startProgress = (_lastProgress = Mathf.Repeat(progress, 1f));
	}

	public void ApplyTemporarySpeedBoost(float extraSpeed, float distance)
	{
		if (!(extraSpeed <= 0f) && !(distance <= 0f))
		{
			_speedBoost = Mathf.Max(_speedBoost, extraSpeed);
			_speedBoostRemainingDistance = Mathf.Max(_speedBoostRemainingDistance, distance);
		}
	}

	public void SetCustomTimeScale(float timeScale)
	{
		_customTimeScale = Mathf.Max(0f, timeScale);
		if (_customTimeScale <= 0f)
		{
			ResetVelocity();
		}
	}

	private bool CanMove()
	{
		if (!HasValidConfig())
		{
			return false;
		}
		return _path != null && _path.IsValid;
	}

	private float GetSpeedTimeScale()
	{
		if (_customTimeScale <= 1f)
		{
			return _customTimeScale;
		}
		float multiplier = ((config != null) ? config.ScaleSpeedMultiplier : 0.2f);
		return 1f + (_customTimeScale - 1f) * multiplier;
	}

	private bool IsMovementStepReady()
	{
		if (_movementTime < _nextMovementTime)
		{
			return false;
		}
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
		if (TryGetSplineSample(out var sample))
		{
			UpdateCompletedLapCount(sample.Progress);
			ApplyMovementForce(sample.WorldTangent);
			ApplyRoadGripForce(sample.WorldNearestPoint, sample.WorldTangent);
		}
	}

	public void ManualUpdate(float deltaTime)
	{
		_movementTime += Mathf.Max(0f, deltaTime) * _customTimeScale;
		if (!(_customTimeScale <= 0f) && CanMove())
		{
			AdvanceAlongSplineManual(deltaTime);
		}
	}

	private void AdvanceAlongSplineManual(float deltaTime)
	{
		if (_path == null || !_path.IsValid)
		{
			return;
		}
		float pathLength = Mathf.Max(0.0001f, _path.CalculateLength());
		float deltaProgress = GetTargetSpeed() * Mathf.Max(0f, deltaTime) / pathLength;
		if (!(deltaProgress <= 0f))
		{
			float nextProgress = Mathf.Repeat(_lastProgress + deltaProgress, 1f);
			UpdateCompletedLapCount(nextProgress);
			Vector3 worldPosition = _path.EvaluateWorldPosition(nextProgress);
			Vector3 worldTangent = _path.EvaluateWorldTangent(nextProgress);
			base.transform.position = worldPosition;
			if (worldTangent.sqrMagnitude > 1E-06f)
			{
				base.transform.forward = worldTangent.normalized;
			}
		}
	}

	private bool TryGetSplineSample(out SplineSample sample)
	{
		sample = default(SplineSample);
		if (_path == null || !_path.IsValid)
		{
			return false;
		}
		Vector3 localPosition = _path.InverseTransformPoint(base.transform.position);
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
		Vector3 worldTangent = _path.TransformDirection(tangent).normalized;
		if (worldTangent.sqrMagnitude <= 1E-06f)
		{
			return false;
		}
		sample = new SplineSample(Mathf.Repeat(progress, 1f), _path.TransformPoint(nearestPoint), worldTangent);
		return true;
	}

	private void ApplyMovementForce(Vector3 worldTangent)
	{
		Vector3 desiredVelocity = worldTangent * GetSmoothedSpeed();
		Vector3 velocityDelta = desiredVelocity - rb.velocity;
		rb.AddForce(velocityDelta * config.Acceleration, ForceMode.Acceleration);
	}

	private float GetSmoothedSpeed()
	{
		float targetSpeed = GetTargetSpeed();
		float deltaTime = Mathf.Max(0.0001f, _movementTime - _lastMovementForceTime);
		_lastMovementForceTime = _movementTime;
		_smoothedSpeed = Mathf.MoveTowards(_smoothedSpeed, targetSpeed, config.Acceleration * deltaTime);
		return _smoothedSpeed;
	}

	private float GetTargetSpeed()
	{
		return (config.Speed + _speedBoost) * _lockedSpeedMultiplier * GetSpeedTimeScale();
	}

	private void ApplyRoadGripForce(Vector3 worldNearestPoint, Vector3 worldTangent)
	{
		if (!(_movementTime < _nextRoadGripTime))
		{
			Vector3 offsetToSpline = Vector3.ProjectOnPlane(worldNearestPoint - base.transform.position, worldTangent);
			if (!(offsetToSpline.sqrMagnitude <= 1E-06f))
			{
				rb.AddForce(offsetToSpline * config.RoadGripForce, ForceMode.Acceleration);
			}
		}
	}

	private void ResetVelocity()
	{
		if (!(rb == null) && !rb.isKinematic)
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
		if (diff > 0.5f)
		{
			diff -= 1f;
		}
		else if (diff < -0.5f)
		{
			diff += 1f;
		}
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
		if (!(_speedBoostRemainingDistance <= 0f))
		{
			_speedBoostRemainingDistance -= GetForwardProgressDelta(_lastProgress, currentProgress);
			if (!(_speedBoostRemainingDistance > 0f))
			{
				_speedBoostRemainingDistance = 0f;
				_speedBoost = 0f;
			}
		}
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
		_smoothedSpeed = ((config != null) ? config.Speed : 0f);
		_completedLapCount = 0;
		_accumulatedForwardProgress = 0f;
		_hasStartedFirstLoop = true;
		_customTimeScale = ((MonoSingleton<CustomTimeScaleGroup>.Instance != null) ? MonoSingleton<CustomTimeScaleGroup>.Instance.CurrentTimeScale : 1f);
	}

	private void SnapToSpline(float progress, Vector3? initialWorldPosition)
	{
		Vector3 worldPos = initialWorldPosition ?? GetSplineWorldPosition(progress);
		base.transform.position = worldPos;
		if (rb != null)
		{
			rb.position = worldPos;
		}
	}

	private Vector3 GetSplineWorldPosition(float progress)
	{
		return (_path != null && _path.IsValid) ? _path.EvaluateWorldPosition(progress) : base.transform.position;
	}

	private void EnablePhysicsNextFrameAsync(int setupVersion, float progress)
	{
		if (setupVersion == _setupVersion)
		{
			SetPhysicsEnabled(true);
			ApplySpawnPush(progress);
		}
	}

	public void LockYAxisTemporarily(float duration, float speedMultiplier = 1f, float speedMultiplierDuration = -1f)
	{
		if (!(rb == null))
		{
			if (duration > 0f)
			{
				StartCoroutine(ApplyYAxisLockRoutine(++_yAxisLockVersion, duration));
			}
			float actualSpeedMultiplierDuration = ((speedMultiplierDuration < 0f) ? duration : speedMultiplierDuration);
			if (actualSpeedMultiplierDuration > 0f && speedMultiplier < 0.999f)
			{
				StartCoroutine(ApplySpeedMultiplierRoutine(++_speedMultiplierLockVersion, actualSpeedMultiplierDuration, Mathf.Clamp01(speedMultiplier)));
			}
		}
	}

	private IEnumerator ApplyYAxisLockRoutine(int lockVersion, float duration)
	{
		if (!(rb == null))
		{
			SetSpawnYAxisLocked(true);
			for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime * _customTimeScale)
			{
				yield return null;
			}
			if (lockVersion == _yAxisLockVersion && !(rb == null))
			{
				SetSpawnYAxisLocked(false);
			}
		}
	}

	private IEnumerator ApplySpeedMultiplierRoutine(int lockVersion, float duration, float speedMultiplier)
	{
		_lockedSpeedMultiplier = speedMultiplier;
		for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime * _customTimeScale)
		{
			yield return null;
		}
		if (lockVersion == _speedMultiplierLockVersion)
		{
			_lockedSpeedMultiplier = 1f;
		}
	}

	private void SetSpawnYAxisLocked(bool isLocked)
	{
		if (!(rb == null))
		{
			if (isLocked)
			{
				rb.constraints |= RigidbodyConstraints.FreezePositionY;
			}
			else
			{
				rb.constraints &= (RigidbodyConstraints)(-5);
			}
		}
	}

	private void ApplySpawnPush(float progress)
	{
		if (!(rb == null) && !rb.isKinematic && !(config.SpawnPushSpeed <= 0f))
		{
			Vector3 tangent = GetSplineWorldTangent(progress);
			if (!(tangent.sqrMagnitude <= 1E-06f))
			{
				rb.velocity = tangent * config.SpawnPushSpeed;
			}
		}
	}

	private void SetPhysicsEnabled(bool isEnabled)
	{
		if (!(rb == null))
		{
			rb.isKinematic = !isEnabled;
			rb.detectCollisions = isEnabled;
		}
	}

	private Vector3 GetSplineWorldTangent(float progress)
	{
		return (_path != null && _path.IsValid) ? _path.EvaluateWorldTangent(progress) : Vector3.forward;
	}

	private bool HasValidConfig()
	{
		if (config != null)
		{
			_hasLoggedMissingConfig = false;
			return true;
		}
		if (_hasLoggedMissingConfig)
		{
			return false;
		}
		Debug.LogError("[CubeMovement] Chua gan CubeMovementConfigSO tren object " + base.name + ".", this);
		_hasLoggedMissingConfig = true;
		return false;
	}
}
