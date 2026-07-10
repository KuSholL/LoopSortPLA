using UnityEngine;

public class ConveyorSpawnPointCalculator
{
    private ConveyorSpawnPointConfigSO _spawnPointConfig;
    private ConveyorMeshBuilder _conveyorMeshBuilder;
    private ConveyorPathRuntime _path;
    private Transform _spawnRoot;
    private Transform _fallbackTransform;
    private float _deliverySideSpread = 0.12f;
    private float _deliveryForwardSpread = 0.03f;

    public ConveyorSpawnPointCalculator(
        ConveyorSpawnPointConfigSO spawnPointConfig,
        ConveyorMeshBuilder conveyorMeshBuilder,
        ConveyorPathRuntime path,
        Transform spawnRoot,
        Transform fallbackTransform)
    {
        _spawnPointConfig = spawnPointConfig;
        _conveyorMeshBuilder = conveyorMeshBuilder;
        _path = path;
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

        var usableHalfWidth = 
            _conveyorMeshBuilder.GetUsableRoadHalfWidth(_spawnPointConfig.DeliveryEdgePadding);
        return usableHalfWidth * _spawnPointConfig.DeliverySpreadSideRatio;
    }

    private float GetDeliveryForwardSpreadInternal()
    {
        if (_conveyorMeshBuilder == null)
        {
            return _spawnPointConfig.DeliveryMinForwardSpread;
        }

        var roadLength = _conveyorMeshBuilder.GetApproximateRoadLength(_path);
        var spread = roadLength * _spawnPointConfig.DeliveryForwardSpreadByLength;
        return Mathf.Clamp(spread, _spawnPointConfig.DeliveryMinForwardSpread, _spawnPointConfig.DeliveryMaxForwardSpread);
    }
    
    public Vector3 GetDeliverySpawnPosition(float progress, int index)
    {
        var center = GetSpawnPosition(progress);
        if (_path == null || !_path.IsValid)
        {
            return center;
        }

        var worldForward = _path.EvaluateWorldTangent(progress);
        if (worldForward.sqrMagnitude <= 0.000001f)
        {
            worldForward = _fallbackTransform != null
                ? _fallbackTransform.forward
                : Vector3.forward;
        }

        var worldRight = Vector3.Cross(Vector3.up, worldForward).normalized;
        if (worldRight.sqrMagnitude <= 0.000001f)
        {
            worldRight = _fallbackTransform != null
                ? _fallbackTransform.right
                : Vector3.right;
        }

        var sideSpread = _deliverySideSpread;
        var forwardSpread = _deliveryForwardSpread;
        var patternOffset = GetDeliveryPatternOffset(index);
        var sideJitter = Random.Range(
            -sideSpread * _spawnPointConfig.DeliveryJitterSideRatio,
            sideSpread * _spawnPointConfig.DeliveryJitterSideRatio);
        var forwardJitter = Random.Range(
            -forwardSpread * _spawnPointConfig.DeliveryJitterForwardRatio,
            forwardSpread * _spawnPointConfig.DeliveryJitterForwardRatio);

        return center
               + worldRight * (patternOffset.x + sideJitter)
               + worldForward * (patternOffset.y + forwardJitter)
               + Vector3.up * _spawnPointConfig.DeliveryLift;
    }

    private Vector2 GetDeliveryPatternOffset(int index)
    {
        switch (index % 5)
        {
            case 1:
                return new Vector2(_deliverySideSpread, 0f);
            case 2:
                return new Vector2(-_deliverySideSpread, 0f);
            case 3:
                return new Vector2(0f, _deliveryForwardSpread);
            case 4:
                return new Vector2(0f, -_deliveryForwardSpread);
            default:
                return Vector2.zero;
        }
    }

    private Vector3 GetSpawnPosition(float progress)
    {
        if (_path == null || !_path.IsValid)
        {
            return _spawnRoot != null ? _spawnRoot.position : Vector3.zero;
        }

        return _path.EvaluateWorldPosition(progress);
    }
}
