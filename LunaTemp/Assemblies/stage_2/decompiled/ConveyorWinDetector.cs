using System.Collections.Generic;
using UnityEngine;

public class ConveyorWinDetector
{
	private readonly HashSet<EBlockColorType> _requiredColors = new HashSet<EBlockColorType>();

	private readonly HashSet<EBlockColorType> _finishedColors = new HashSet<EBlockColorType>();

	public HashSet<EBlockColorType> RequiredColors => _requiredColors;

	public HashSet<EBlockColorType> FinishedColors => _finishedColors;

	public void Init(LevelData levelData)
	{
		_requiredColors.Clear();
		_finishedColors.Clear();
		if (!(levelData == null))
		{
			RegisterCarriers((levelData.CarrierLayout != null) ? levelData.CarrierLayout.Carriers : null);
			Debug.Log($"[WinDetector] Initialized. Target: {_requiredColors.Count} colors.");
		}
	}

	public void OnCarrierFinished(EBlockColorType colorType)
	{
		if (colorType != EBlockColorType.None)
		{
			_finishedColors.Add(colorType);
			Debug.Log($"[WinDetector] Color Completed. Progress: {_finishedColors.Count}/{_requiredColors.Count}");
			if (CheckWin() && GameEventBus.OnWinTrigger != null)
			{
				GameEventBus.OnWinTrigger();
			}
		}
	}

	public bool CheckWin()
	{
		if (_requiredColors.Count <= 0)
		{
			return false;
		}
		foreach (EBlockColorType color in _requiredColors)
		{
			if (!_finishedColors.Contains(color))
			{
				return false;
			}
		}
		return MonoSingleton<ConveyorDeliverySystem>.Instance.IsConveyorEmpty;
	}

	private void RegisterCarriers(IEnumerable<CarrierStackData> carrierStacks)
	{
		if (carrierStacks == null)
		{
			return;
		}
		foreach (CarrierStackData carrierStack in carrierStacks)
		{
			RegisterCarrierColors(carrierStack);
		}
	}

	private void RegisterCarrierColors(CarrierStackData carrierStack)
	{
		if (carrierStack == null || carrierStack.Blocks == null)
		{
			return;
		}
		foreach (BlockData blockData in carrierStack.Blocks)
		{
			if (blockData != null && blockData.BlockColor != EBlockColorType.None)
			{
				_requiredColors.Add(blockData.BlockColor);
			}
		}
	}

	public float GetProgressLevel()
	{
		return Mathf.Clamp01((float)_finishedColors.Count / (float)_requiredColors.Count);
	}
}
