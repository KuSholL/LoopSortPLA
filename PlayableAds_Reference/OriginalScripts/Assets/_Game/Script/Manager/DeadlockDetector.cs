using System.Collections.Generic;

public static class DeadlockDetector
{
    // Lưu thông tin tối thiểu của một carrier dùng để xét deadlock theo rule mới.
    private readonly struct CarrierUnloadSequenceInfo
    {
        public CarrierUnloadSequenceInfo(CarrierBase carrier, EBlockColorType color, int unloadableChainLength)
        {
            Carrier = carrier;
            Color = color;
            UnloadableChainLength = unloadableChainLength;
        }

        public CarrierBase Carrier { get; }
        public EBlockColorType Color { get; }
        public int UnloadableChainLength { get; }
    }

    public static bool IsGameDeadlocked()
    {
        // Chỉ xét thua khi game đang ở trạng thái hợp lệ và conveyor đã đứng yên.
        if (!CanCheckDeadlock()) return false;
        if (CapacityManager.Instance == null) return false;

        var remainingCapacity = CapacityManager.Instance.RemainingCubeCapacity;
        var cubePerBlock = CapacityManager.Instance.CubePerBlock;
        // Rule mới chỉ xét đúng 2 ngưỡng:
        // - Còn đúng 1 block capacity  -> yêu cầu cụm top tối thiểu 2 block cùng màu
        // - Còn đúng 2 block capacity  -> yêu cầu cụm top tối thiểu 3 block cùng màu
        if (!TryResolveCapacityRule(remainingCapacity, cubePerBlock, out var requiredRunLength))
        {
            return false;
        }

        // Nếu vẫn còn cube trên conveyor có thể được hút vào carrier thì chưa coi là kẹt.
        if (ConveyorDeliverySystem.Instance.CanAnyConveyorCubeBeReceived())
        {
            return false;
        }

        var spawnedCarriers = GetSpawnedCarriers();
        if (spawnedCarriers == null || spawnedCarriers.Count == 0) return false;

        var unloadableInfos = GetUnloadableCarrierInfos(spawnedCarriers);
        if (unloadableInfos.Count == 0) return false;
        // Tất cả carrier có thể unload phải có cùng "định dạng" top block.
        // Chỉ cần lệch 1 carrier là bỏ qua, không xử thua.
        if (!AllUnloadableCarriersMatchPattern(unloadableInfos, requiredRunLength)) return false;

        for (var i = 0; i < unloadableInfos.Count; i++)
        {
            // Chỉ cần tồn tại 1 carrier mà block vừa unload ra có thể bay sang carrier khác
            // thì game vẫn còn đường đi, chưa thua.
            if (CanReachAnotherCarrier(unloadableInfos[i], spawnedCarriers))
            {
                return false;
            }
        }

        // Đi hết tất cả các bước trên mà vẫn không có đường đi sang carrier khác thì deadlock.
        return true;
    }

    private static bool CanCheckDeadlock()
    {
        // Không xét deadlock khi game đã kết thúc hoặc đang ở tutorial.
        if (LevelManager.Instance == null || LevelManager.Instance.IsGameEnded || LevelManager.Instance.IsTutorial)
        {
            return false;
        }

        // Chỉ xét khi conveyor/cube/carrier không còn chuyển động dang dở.
        return ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsConveyorStable();
    }

    private static bool TryResolveCapacityRule(int remainingCapacity, int cubePerBlock, out int requiredRunLength)
    {
        requiredRunLength = 0;
        if (cubePerBlock <= 0) return false;

        // Case 1: capacity còn đúng 1 block.
        if (remainingCapacity == cubePerBlock)
        {
            requiredRunLength = 2;
            return true;
        }

        // Case 2: capacity còn đúng 2 block.
        if (remainingCapacity == cubePerBlock * 2)
        {
            requiredRunLength = 3;
            return true;
        }

        return false;
    }

    private static IReadOnlyList<CarrierBase> GetSpawnedCarriers()
    {
        // Lấy toàn bộ carrier hiện có trong màn để lọc ra tập carrier được tham gia xét lose.
        return CarrierSystem.Instance != null && CarrierSystem.Instance.CarrierSpawner != null
            ? CarrierSystem.Instance.CarrierSpawner.SpawnedCarriers
            : null;
    }

    private static List<CarrierUnloadSequenceInfo> GetUnloadableCarrierInfos(IReadOnlyList<CarrierBase> spawnedCarriers)
    {
        var infos = new List<CarrierUnloadSequenceInfo>();
        if (spawnedCarriers == null) return infos;

        for (var i = 0; i < spawnedCarriers.Count; i++)
        {
            var carrier = spawnedCarriers[i];
            // Bỏ qua carrier không thể unload hoặc không đủ dữ liệu để xét.
            if (!CanParticipateAsUnloadSource(carrier)) continue;
            if (!TryGetUnloadSequenceInfo(carrier, out var info)) continue;

            infos.Add(info);
        }

        return infos;
    }

    private static bool CanParticipateAsUnloadSource(CarrierBase carrier)
    {
        // Carrier dùng để xét thua phải là carrier mà người chơi thực sự còn thao tác unload được.
        if (carrier == null) return false;
        if (carrier.RuntimeState != null && carrier.RuntimeState.IsFinished) return false;
        if (carrier.IsLockedByContainer()) return false;
        if (!carrier.Interactable) return false;
        if (!carrier.CanUnloadByMechanic()) return false;
        if (carrier.BlockController == null || carrier.BlockLayout == null) return false;

        // Top block phải thật sự có thể unload được theo mechanic của block hiện tại.
        return carrier.BlockController.GetTopUnloadCandidateBlock() != null;
    }

    private static bool TryGetUnloadSequenceInfo(CarrierBase carrier, out CarrierUnloadSequenceInfo info)
    {
        info = default;
        if (carrier == null || carrier.BlockController == null) return false;

        var topBlock = carrier.BlockController.GetTopUnloadCandidateBlock();
        if (topBlock == null) return false;

        var color = topBlock.GetBlockColorType();
        // Lấy số block cùng màu liên tiếp ở top mà logic hiện tại cho phép unload thật.
        var unloadBlocks = carrier.BlockController.GetPotentialUnloadBlocks(color);
        if (unloadBlocks == null || unloadBlocks.Count == 0) return false;

        info = new CarrierUnloadSequenceInfo(carrier, color, unloadBlocks.Count);
        return true;
    }

    private static bool AllUnloadableCarriersMatchPattern(
        List<CarrierUnloadSequenceInfo> unloadableInfos,
        int requiredRunLength)
    {
        if (unloadableInfos == null || unloadableInfos.Count == 0) return false;

        var patternLength = unloadableInfos[0].UnloadableChainLength;
        // Carrier đầu tiên phải đạt ngưỡng tối thiểu của case capacity hiện tại.
        if (patternLength < requiredRunLength) return false;

        for (var i = 1; i < unloadableInfos.Count; i++)
        {
            // Rule mới yêu cầu tất cả carrier unload được phải có cùng một định dạng top block.
            if (unloadableInfos[i].UnloadableChainLength != patternLength)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanReachAnotherCarrier(
        CarrierUnloadSequenceInfo sourceInfo,
        IReadOnlyList<CarrierBase> spawnedCarriers)
    {
        if (spawnedCarriers == null) return false;

        for (var i = 0; i < spawnedCarriers.Count; i++)
        {
            var targetCarrier = spawnedCarriers[i];
            // Kiểm tra block màu vừa unload từ source có thể đi vào một carrier khác hay không.
            if (!CanReceiveFromAnotherCarrier(targetCarrier, sourceInfo.Carrier, sourceInfo.Color))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool CanReceiveFromAnotherCarrier(
        CarrierBase targetCarrier,
        CarrierBase sourceCarrier,
        EBlockColorType color)
    {
        // Không tính trường hợp block vừa unload quay ngược lại chính carrier nguồn.
        if (targetCarrier == null || targetCarrier == sourceCarrier) return false;
        // Carrier đích đã finished hoặc đang ở trạng thái không nhận được thì bỏ qua.
        if (targetCarrier.RuntimeState != null && targetCarrier.RuntimeState.IsFinished) return false;
        if (!targetCarrier.Interactable || targetCarrier.IsDelivering) return false;

        // Dùng chính rule receive hiện có của game để tôn trọng mechanic/hidden/special receiver.
        return targetCarrier.CanPotentiallyReceive(color);
    }
}
