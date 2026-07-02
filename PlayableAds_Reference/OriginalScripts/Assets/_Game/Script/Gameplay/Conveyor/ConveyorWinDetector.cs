using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks completed colors required to win the level.
/// </summary>
public class ConveyorWinDetector
{
    private readonly HashSet<EBlockColorType> _requiredColors = new();
    private readonly HashSet<EBlockColorType> _finishedColors = new();

    public HashSet<EBlockColorType> RequiredColors => _requiredColors;
    public HashSet<EBlockColorType> FinishedColors => _finishedColors;

    public void Init(LevelData levelData)
    {
        _requiredColors.Clear();
        _finishedColors.Clear();

        if (levelData == null) return;

        RegisterCarriers(levelData.CarrierLayout?.Carriers);
        Debug.Log($"[WinDetector] Initialized. Target: {_requiredColors.Count} colors.");
    }

    public void OnCarrierFinished(EBlockColorType colorType)
    {
        if (colorType == EBlockColorType.None) return;

        _finishedColors.Add(colorType);
        Debug.Log($"[WinDetector] Color Completed. Progress: {_finishedColors.Count}/{_requiredColors.Count}");

        if (CheckWin())
        {
            GameEventBus.OnWinTrigger?.Invoke();
        }
    }

    public bool CheckWin()
    {
        if (_requiredColors.Count <= 0) return false;

        foreach (var color in _requiredColors)
            if (!_finishedColors.Contains(color)) return false;

        return ConveyorDeliverySystem.Instance.IsConveyorEmpty;
    }

    private void RegisterCarriers(IEnumerable<CarrierStackData> carrierStacks)
    {
        if (carrierStacks == null) return;
        foreach (var carrierStack in carrierStacks) RegisterCarrierColors(carrierStack);
    }

    private void RegisterCarrierColors(CarrierStackData carrierStack)
    {
        if (carrierStack?.Blocks == null) return;

        foreach (var blockData in carrierStack.Blocks)
        {
            if (blockData != null && blockData.BlockColor != EBlockColorType.None)
                _requiredColors.Add(blockData.BlockColor);
        }
    }
    
    public float GetProgressLevel()
    {
        var percent = Mathf.Clamp01((float)_finishedColors.Count / _requiredColors.Count);
        return percent;
    }
}
