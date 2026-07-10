using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

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

	private readonly int _splineSampleCount = 200;

	private readonly int _splineSearchRange = 15;

	private SplineContainer _splineContainer;

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

	private static readonly Dictionary<Spline, SplineDataCache> _splineCache;

	static CubeMovement()
	{
		_splineCache = new Dictionary<Spline, SplineDataCache>();
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Combine(GameEventBus.OnInitLoadLevel, new Action(ClearSplineCache));
	}

	private static void ClearSplineCache()
	{
		_splineCache.Clear();
	}

	private static SplineDataCache GetOrCreateCache(Spline spline, int sampleCount)
	{
		if (spline == null)
		{
			return null;
		}
		if (!_splineCache.TryGetValue(spline, out var cache))
		{
			cache = new SplineDataCache(spline, sampleCount);
			_splineCache.Add(spline, cache);
		}
		return cache;
	}

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
			else if (IsMovementStepReady())
			{
				AdvanceAlongSpline();
			}
		}
	}

	public void Setup(SplineContainer splineContainer, float startProgress = 0f, float progressOffset = 0f, Vector3? initialWorldPosition = null)
	{
		_isFirstSampleOnSpline = true;
		_setupVersion++;
		_splineContainer = splineContainer;
		float progress = Mathf.Repeat(startProgress, 1f);
		_progressOffset = progressOffset;
		PrepareForSplineMovement(progress, initialWorldPosition);
		LockYAxisTemporarily((config != null) ? config.SpawnFreezeYDuration : 0.3f);
		EnablePhysicsNextFrameAsync(_setupVersion, progress);
	}

	public void ApplyPortalTransfer(SplineContainer splineContainer, Vector3 worldPosition, Vector3 worldVelocity, Vector3 worldForward)
	{
		_isFirstSampleOnSpline = true;
		_splineContainer = splineContainer;
		ResetDelayedForces();
		SetPhysicsEnabled(true);
		if (rb != null)
		{
			rb.position = worldPosition;
			rb.velocity = worldVelocity;
		}
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
		if (_splineContainer == null || _splineContainer.Spline == null || _splineContainer.Spline.Count <= 0)
		{
			return _lastProgress;
		}
		SplineDataCache cache = GetOrCreateCache(_splineContainer.Spline, _splineSampleCount);
		if (cache == null)
		{
			return _lastProgress;
		}
		Vector3 localPosition = _splineContainer.transform.InverseTransformPoint(base.transform.position);
		Vector3 nearestLocalPoint;
		float progress;
		Vector3 localTangent;
		if (_isFirstSampleOnSpline)
		{
			cache.GetNearestPointGlobal(localPosition, out nearestLocalPoint, out progress, out localTangent);
		}
		else
		{
			cache.GetNearestPointLocal(localPosition, _lastProgress, _splineSearchRange, out localTangent, out progress, out nearestLocalPoint);
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
		return rb != null && !rb.isKinematic && _splineContainer != null && _splineContainer.Spline != null && _splineContainer.Spline.Count > 0;
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

	private bool TryGetSplineSample(out SplineSample sample)
	{
		sample = default(SplineSample);
		SplineDataCache cache = GetOrCreateCache(_splineContainer.Spline, _splineSampleCount);
		if (cache == null)
		{
			return false;
		}
		Vector3 localPosition = _splineContainer.transform.InverseTransformPoint(base.transform.position);
		Vector3 nearestPoint;
		float progress;
		Vector3 tangent;
		if (_isFirstSampleOnSpline)
		{
			_isFirstSampleOnSpline = false;
			cache.GetNearestPointGlobal(localPosition, out nearestPoint, out progress, out tangent);
			_lastProgress = progress;
		}
		else
		{
			cache.GetNearestPointLocal(localPosition, _lastProgress, _splineSearchRange, out nearestPoint, out progress, out tangent);
		}
		Vector3 worldTangent = _splineContainer.transform.TransformDirection(tangent).normalized;
		if (worldTangent.sqrMagnitude <= 1E-06f)
		{
			return false;
		}
		sample = new SplineSample(Mathf.Repeat(progress, 1f), _splineContainer.transform.TransformPoint(nearestPoint), worldTangent);
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
		float3 localPos = _splineContainer.Spline.EvaluatePosition(progress);
		return _splineContainer.transform.TransformPoint(localPos);
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
		float3 tangent = _splineContainer.Spline.EvaluateTangent(progress);
		return _splineContainer.transform.TransformDirection(tangent).normalized;
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
