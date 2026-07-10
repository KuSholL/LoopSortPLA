using UnityEngine;

public class CapacityManager : MonoSingleton<CapacityManager>
{
	private int _currentCubeCount;

	private int _pendingCubeCount;

	private int _maxCapacity;

	private int _cubePerBlock;

	private readonly float _prelosePercent = 0.7f;

	public bool IsFull => _maxCapacity > 0 && _currentCubeCount + _pendingCubeCount >= _maxCapacity * _cubePerBlock;

	public bool IsPrelose => _maxCapacity > 0 && (float)(_currentCubeCount + _pendingCubeCount) >= (float)(_maxCapacity * _cubePerBlock) * _prelosePercent;

	public int RemainingCubeCapacity => (_maxCapacity > 0) ? Mathf.Max(0, _maxCapacity * _cubePerBlock - (_currentCubeCount + _pendingCubeCount)) : 0;

	public int CubePerBlock => _cubePerBlock;

	public int MaxCapacity => _maxCapacity;

	public bool CanAcceptCubes(int count)
	{
		return _maxCapacity > 0 && _currentCubeCount + _pendingCubeCount + count <= _maxCapacity * _cubePerBlock;
	}

	public void Init(LevelData levelData)
	{
		_maxCapacity = levelData.Capacity;
		_cubePerBlock = GetCubePerBlock();
		_currentCubeCount = 0;
		_pendingCubeCount = 0;
		CapacityUI capacityUI = CapacityUI.EnsureExists();
		if (capacityUI != null)
		{
			capacityUI.InitLevel(_maxCapacity);
		}
		UpdateCapacityUI();
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
		int maxCubeCount = _maxCapacity * _cubePerBlock;
		GameEventBus.OnUpdateCapcityUI?.Invoke(_currentCubeCount, maxCubeCount);
	}

	private void RefreshPreloseBlink()
	{
		if ((bool)MonoSingleton<ConveyorDeliverySystem>.Instance)
		{
			MonoSingleton<ConveyorDeliverySystem>.Instance.RefreshPreloseBlink();
		}
	}

	private int GetUsedCapacity()
	{
		if (_currentCubeCount == 0 || _cubePerBlock <= 0)
		{
			return 0;
		}
		return Mathf.CeilToInt((float)_currentCubeCount / (float)_cubePerBlock);
	}

	private int GetCubePerBlock()
	{
		CarrierConfigSO config = GetCarrierConfig();
		return CarrierGridUtility.GetCubePerBlock(GetCurrentLevel(), config);
	}

	private CarrierConfigSO GetCarrierConfig()
	{
		return (MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCarrierConfig() : null;
	}

	private LevelData GetCurrentLevel()
	{
		return (MonoSingleton<LevelManager>.Instance != null) ? MonoSingleton<LevelManager>.Instance.CurrentLevel : null;
	}
}
