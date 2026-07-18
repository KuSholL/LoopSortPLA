using UnityEngine;

public class CapacityManager : MonoSingleton<CapacityManager>
{
    private int _currentCubeCount;
    private int _pendingCubeCount;
    private int _maxCapacity;
    private int _cubePerBlock;
    private readonly float _prelosePercent = 0.7f;

    public bool IsFull => _maxCapacity > 0 && (_currentCubeCount + _pendingCubeCount) >= _maxCapacity * _cubePerBlock;
    public bool IsPrelose => _maxCapacity > 0 && (_currentCubeCount + _pendingCubeCount) >= _maxCapacity * _cubePerBlock * _prelosePercent;
    public bool CanAcceptCubes(int count) => _maxCapacity > 0 && (_currentCubeCount + _pendingCubeCount + count) <= _maxCapacity * _cubePerBlock;
    public int RemainingCubeCapacity => _maxCapacity > 0 ? Mathf.Max(0, _maxCapacity * _cubePerBlock - (_currentCubeCount + _pendingCubeCount)) : 0;
    
    public int CubePerBlock => _cubePerBlock;   
    public int MaxCapacity => _maxCapacity;

    public void Init(LevelData levelData)
    {
        _maxCapacity = levelData.Capacity;
        _cubePerBlock = GetCubePerBlock();
        _currentCubeCount = 0;
        _pendingCubeCount = 0;

        var capacityUI = CapacityUI.EnsureExists();
        if (capacityUI != null) capacityUI.InitLevel(_maxCapacity);
        UpdateCapacityUI();
        RefreshPreloseBlink();
    }

    public void ReservePendingCubes(int count)
    {
        _pendingCubeCount += count;
    }

    public void AddCube(int count = 1)
    {
        _currentCubeCount += count;
        _pendingCubeCount = Mathf.Max(0, _pendingCubeCount - count);
        UpdateCapacityUI();
        RefreshPreloseBlink();
    }

    public void RemoveCube(int count = 1)
    {
        _currentCubeCount = Mathf.Max(0, _currentCubeCount - count);
        UpdateCapacityUI();
        RefreshPreloseBlink();
    }

    private void UpdateCapacityUI()
    {
        var maxCubeCount = _maxCapacity * _cubePerBlock;
        GameEventBus.OnUpdateCapcityUI?.Invoke(_currentCubeCount, maxCubeCount);
    }

    private void RefreshPreloseBlink()
    {
        if (!ConveyorDeliverySystem.Instance) return;
        ConveyorDeliverySystem.Instance.RefreshPreloseBlink();
    }

    private int GetUsedCapacity()
    {
        if (_currentCubeCount == 0 || _cubePerBlock <= 0) return 0;
        return Mathf.CeilToInt((float)_currentCubeCount / _cubePerBlock);
    }

    private int GetCubePerBlock()
    {
        var config = GetCarrierConfig();
        return CarrierGridUtility.GetCubePerBlock(GetCurrentLevel(), config);
    }

    private CarrierConfigSO GetCarrierConfig()
    {
        return ConfigManager.Instance != null ? ConfigManager.Instance.GetCarrierConfig() : null;
    }

    private LevelData GetCurrentLevel()
    {
        return LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;
    }
}

public static class CarrierGridUtility
{
    private const int BaselineColumns = 2;
    private const int BaselineRows = 2;
    private const int BaselineDepth = 2;

    public static int GetDepth(CarrierConfigSO config)
    {
        return Mathf.Max(1, config != null ? config.Depth : BaselineDepth);
    }

    public static int GetBaselineCubePerBlock(CarrierConfigSO config)
    {
        return BaselineColumns * BaselineRows * GetDepth(config);
    }

    public static int GetCubePerBlock(LevelData level, CarrierConfigSO config)
    {
        var grid = GetGridSize(level);
        return grid.x * grid.y * GetDepth(config);
    }

    public static Vector2Int GetGridSize(LevelData level)
    {
        return GetBestGridSize(GetLayerCellCount(level));
    }

    private static int GetLayerCellCount(LevelData level)
    {
        var baseline = level != null ? Mathf.Max(1, level.BaselineCapacity) : 0;
        var capacity = level != null ? Mathf.Max(1, level.Capacity) : 0;
        if (baseline <= 0 || capacity <= 0) return BaselineColumns * BaselineRows;
        return Mathf.Max(1, Mathf.RoundToInt(baseline * 4f / capacity));
    }

    private static Vector2Int GetBestGridSize(int value)
    {
        var best = new Vector2Int(2, 2);
        var bestArea = int.MaxValue;
        var bestGap = int.MaxValue;
        for (var row = 2; row <= value; row++)
            UpdateBestGrid(value, row, ref best, ref bestArea, ref bestGap);
        return best;
    }

    private static void UpdateBestGrid(
        int value,
        int row,
        ref Vector2Int best,
        ref int bestArea,
        ref int bestGap)
    {
        var col = Mathf.Max(2, Mathf.CeilToInt((float)value / row));
        var area = col * row;
        var gap = Mathf.Abs(col - row);
        if (area > bestArea) return;
        if (area == bestArea && gap >= bestGap) return;
        best = new Vector2Int(col, row);
        bestArea = area;
        bestGap = gap;
    }
}
