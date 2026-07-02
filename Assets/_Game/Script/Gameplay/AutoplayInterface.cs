using System.Collections.Generic;

public static class AutoplayInterface
{
    public static bool IsFilterActive = false;
    public static readonly Dictionary<int, EBlockColorType> CarrierColorFilters = new Dictionary<int, EBlockColorType>();

    public static bool CanCarrierReceiveColor(CarrierBase carrier, EBlockColorType color)
    {
        if (!IsFilterActive) return true;
        if (carrier == null) return true;

        var spawnedCarriers = CarrierSystem.Instance != null && CarrierSystem.Instance.SpawnedCarriers != null
            ? CarrierSystem.Instance.SpawnedCarriers
            : null;

        if (spawnedCarriers == null) return true;

        int idx = -1;
        for (int i = 0; i < spawnedCarriers.Count; i++)
        {
            if (spawnedCarriers[i] == carrier)
            {
                idx = i;
                break;
            }
        }

        if (idx == -1) return true;

        EBlockColorType expectedColor;
        if (CarrierColorFilters.TryGetValue(idx, out expectedColor))
        {
            if (IsCarrierEmptyInGame(carrier))
            {
                return color == expectedColor;
            }
        }

        return true;
    }

    private static bool IsCarrierEmptyInGame(CarrierBase carrier)
    {
        if (carrier == null || carrier.BlockLayout == null) return true;
        for (var i = 0; i < carrier.MaxBlockCount; i++)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (block != null && block.HasContent) return false;
        }
        return true;
    }
}
