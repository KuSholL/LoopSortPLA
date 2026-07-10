using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorSpawnPointCalculator
{
	private ConveyorSpawnPointConfigSO _spawnPointConfig;

	private ConveyorMeshBuilder _conveyorMeshBuilder;

	private SplineContainer _splineContainer;

	private Transform _spawnRoot;

	private Transform _fallbackTransform;

	private float _deliverySideSpread = 0.12f;

	private float _deliveryForwardSpread = 0.03f;

	public ConveyorSpawnPointCalculator(ConveyorSpawnPointConfigSO spawnPointConfig, ConveyorMeshBuilder conveyorMeshBuilder, SplineContainer splineContainer, Transform spawnRoot, Transform fallbackTransform)
	{
		_spawnPointConfig = spawnPointConfig;
		_conveyorMeshBuilder = conveyorMeshBuilder;
		_splineContainer = splineContainer;
		_spawnRoot = spawnRoot;
		_fallbackTransform = fallbackTransform;
	}

	public void CacheDeliverySpreadSettings()
	{
		_deliverySideSpread = GetDeliverySideSpreadInternal();
		_deliveryForwardSpread = GetDeliveryForwardSpreadInternal();
	}

	private float GetDeliverySideSpreadInternal()
	{
		if (_conveyorMeshBuilder == null)
		{
			return 0.12f;
		}
		float usableHalfWidth = _conveyorMeshBuilder.GetUsableRoadHalfWidth(_spawnPointConfig.DeliveryEdgePadding);
		return usableHalfWidth * _spawnPointConfig.DeliverySpreadSideRatio;
	}

	private float GetDeliveryForwardSpreadInternal()
	{
		if (_conveyorMeshBuilder == null)
		{
			return _spawnPointConfig.DeliveryMinForwardSpread;
		}
		float roadLength = _conveyorMeshBuilder.GetApproximateRoadLength(_splineContainer);
		float spread = roadLength * _spawnPointConfig.DeliveryForwardSpreadByLength;
		return Mathf.Clamp(spread, _spawnPointConfig.DeliveryMinForwardSpread, _spawnPointConfig.DeliveryMaxForwardSpread);
	}

	public Vector3 GetDeliverySpawnPosition(float progress, int index)
	{
		Vector3 center = GetSpawnPosition(progress);
		if (_splineContainer == null || _splineContainer.Spline == null || _splineContainer.Spline.Count <= 0)
		{
			return center;
		}
		float3 tangent = _splineContainer.Spline.EvaluateTangent(progress);
		Vector3 worldForward = _splineContainer.transform.TransformDirection(tangent).normalized;
		if (worldForward.sqrMagnitude <= 1E-06f)
		{
			worldForward = ((_fallbackTransform != null) ? _fallbackTransform.forward : Vector3.forward);
		}
		Vector3 worldRight = Vector3.Cross(Vector3.up, worldForward).normalized;
		if (worldRight.sqrMagnitude <= 1E-06f)
		{
			worldRight = ((_fallbackTransform != null) ? _fallbackTransform.right : Vector3.right);
		}
		float sideSpread = _deliverySideSpread;
		float forwardSpread = _deliveryForwardSpread;
		Vector2 patternOffset = GetDeliveryPatternOffset(index);
		float sideJitter = UnityEngine.Random.Range((0f - sideSpread) * _spawnPointConfig.DeliveryJitterSideRatio, sideSpread * _spawnPointConfig.DeliveryJitterSideRatio);
		float forwardJitter = UnityEngine.Random.Range((0f - forwardSpread) * _spawnPointConfig.DeliveryJitterForwardRatio, forwardSpread * _spawnPointConfig.DeliveryJitterForwardRatio);
		return center + worldRight * (patternOffset.x + sideJitter) + worldForward * (patternOffset.y + forwardJitter) + Vector3.up * _spawnPointConfig.DeliveryLift;
	}

	private Vector2 GetDeliveryPatternOffset(int index)
	{
		switch (index % 5)
		{
		case 1:
			return new Vector2(_deliverySideSpread, 0f);
		case 2:
			return new Vector2(0f - _deliverySideSpread, 0f);
		case 3:
			return new Vector2(0f, _deliveryForwardSpread);
		case 4:
			return new Vector2(0f, 0f - _deliveryForwardSpread);
		default:
			return Vector2.zero;
		}
	}

	private Vector3 GetSpawnPosition(float progress)
	{
		if (_splineContainer == null || _splineContainer.Spline == null || _splineContainer.Spline.Count <= 0)
		{
			return (_spawnRoot != null) ? _spawnRoot.position : Vector3.zero;
		}
		return _splineContainer.transform.TransformPoint(_splineContainer.Spline.EvaluatePosition(progress));
	}
}
