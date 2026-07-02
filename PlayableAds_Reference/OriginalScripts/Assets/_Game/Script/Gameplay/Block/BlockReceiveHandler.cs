using System.Collections.Generic;
using UnityEngine;

public sealed class BlockReceiveHandler
{
    public bool IsAvailableForReceive(
        bool hasContent, int currentCount, int reservedCount, EBlockState state,
        EBlockColorType blockColorType, EBlockColorType colorType, int maxCubes)
    {
        if (state == EBlockState.Unloading) return false;
        if (IsClosedBlock(hasContent, currentCount, state, maxCubes)) return false;
        if (!IsMatchingColorType(hasContent, state, blockColorType, colorType)) return false;
        return currentCount < maxCubes;
    }

    public bool TryReserveReceive(
        ref bool hasContent, ref EBlockColorType blockColorType, ref Color blockColor, ref Color blockShadowColor,
        ref EBlockState state,
        ref int currentCount, ref int reservedCount, EBlockColorType colorType, int maxCubes)
    {
        if (!IsAvailableForReceive(hasContent, currentCount, reservedCount, state, blockColorType, colorType,
                maxCubes)) return false;

        // Lock color and state on first reservation
        if (!hasContent || state == EBlockState.Idle)
        {
            PrepareToReceive(ref hasContent, ref blockColorType, ref blockColor, ref blockShadowColor, ref state,
                colorType, Color.white);
        }

        reservedCount++;
        currentCount++; // Increment immediately for progress visualization
        return true;
    }

    public bool TryReceiveCube(
        ref int currentCount, ref int visualCount, ref int reservedCount,
        ref bool hasContent, ref EBlockColorType blockColorType, ref Color blockColor, ref Color blockShadowColor,
        ref EBlockState state,
        EBlockColorType colorType, Color color, int maxCubes)
    {
        if (reservedCount <= 0) return false;
        reservedCount--;

        // Update with actual color info
        PrepareToReceive(ref hasContent, ref blockColorType, ref blockColor, ref blockShadowColor, ref state, colorType,
            color);

        visualCount++;

        if (currentCount < maxCubes || reservedCount > 0) return true;

        state = EBlockState.Idle;
        return true;
    }

    private bool IsClosedBlock(bool hasContent, int currentCount, EBlockState state, int maxCubes)
    {
        return hasContent && currentCount == maxCubes && state == EBlockState.Idle;
    }

    private bool IsMatchingColorType(bool hasContent, EBlockState state, EBlockColorType blockColorType,
        EBlockColorType colorType)
    {
        if (!hasContent) return true;
        return blockColorType == colorType;
    }

    private void PrepareToReceive(
        ref bool hasContent, ref EBlockColorType blockColorType,
        ref Color blockColor, ref Color blockShadowColor, ref EBlockState state, EBlockColorType colorType, Color color)
    {
        var colorEntry = GetColorEntry(colorType);
        hasContent = true;
        state = EBlockState.Receiving;
        blockColorType = colorType;
        blockColor = colorEntry?.Color ?? color;
        blockShadowColor = colorEntry?.ShadowColor ?? blockColor;
    }

    private ColorEntry GetColorEntry(EBlockColorType colorType)
    {
        var colorConfig = ConfigManager.Instance.GetColorConfig();
        return colorConfig != null ? colorConfig.GetColorEntry(colorType) : null;
    }
}
