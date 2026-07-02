using System.Collections.Generic;
using UnityEngine;

public class BlockLinkSystem : MonoSingleton<BlockLinkSystem>
{
    // Cấu trúc mô tả các block cùng màu liên tiếp cần unload của một Carrier
    public class CarrierUnloadGroup
    {
        public CarrierBase Carrier;
        public List<Block> RunBlocks = new();
    }

    public bool ResolveBlockLinkUnloadGroup(
        CarrierBase clickedCarrier, 
        out List<CarrierUnloadGroup> unloadGroups, 
        out int totalCubeCount,
        out bool isBlocked)
    {
        unloadGroups = new List<CarrierUnloadGroup>();
        totalCubeCount = 0;
        isBlocked = false;

        if (clickedCarrier == null) return false;

        // 1. Tìm block ở đỉnh của Carrier được click
        var topBlock = clickedCarrier.BlockController.GetCurrentBlock();
        if (topBlock == null) return false;

        // Queue dùng để loang các block có link cần xét
        var processQueue = new Queue<Block>();
        // Set lưu trữ các block đã được thu thập để tránh trùng lặp
        var visitedBlocks = new HashSet<Block>();
        // Dictionary để gom các block theo từng Carrier
        var carrierGroups = new Dictionary<CarrierBase, HashSet<Block>>();

        // Khởi tạo loang từ chuỗi của Carrier được click
        var initialRun = clickedCarrier.BlockController.GetContiguousSameColorRun(topBlock);
        foreach (var block in initialRun)
        {
            visitedBlocks.Add(block);
            processQueue.Enqueue(block);
            AddToCarrierGroup(carrierGroups, clickedCarrier, block);
        }

        // 2. Thực hiện quét loang (Breadth-First Search) tìm các block liên kết
        while (processQueue.Count > 0)
        {
            var currentBlock = processQueue.Dequeue();
            if (currentBlock.HasLinkGroupId())
            {
                int groupId = currentBlock.GetLinkGroupId();
                var linkedBlocks = FindAllBlocksWithGroupId(groupId);

                foreach (var linkedBlock in linkedBlocks)
                {
                    if (visitedBlocks.Contains(linkedBlock)) continue;

                    var linkedCarrier = linkedBlock.GetComponentInParent<CarrierBase>();
                    if (linkedCarrier == null) continue;

                    if (!linkedBlock.CanBeginUnload())
                    {
                        isBlocked = true;
                        return false;
                    }

                    // Với mỗi block liên kết mới, ta phải lấy cả chuỗi cùng màu liên tiếp của nó
                    var linkRun = linkedCarrier.BlockController.GetContiguousSameColorRun(linkedBlock);
                    foreach (var block in linkRun)
                    {
                        if (visitedBlocks.Add(block))
                        {
                            processQueue.Enqueue(block);
                            AddToCarrierGroup(carrierGroups, linkedCarrier, block);
                        }
                    }
                }
            }
        }

        // 3. Chuyển đổi dữ liệu và thực hiện kiểm tra Obstruction (Có bị đè không) và trạng thái Carrier
        foreach (var pair in carrierGroups)
        {
            var carrier = pair.Key;

            // Kiểm tra trạng thái hoạt động của Carrier
            if (carrier.RuntimeState.State != CarrierStateType.Idle)
            {
                isBlocked = true;
                return false;
            }

            if (!carrier.CanUnloadByMechanic())
            {
                isBlocked = true;
                return false;
            }

            if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(carrier))
            {
                isBlocked = true;
                return false;
            }

            var blocksInGroup = new List<Block>(pair.Value);
            
            // Sắp xếp từ index thấp nhất (đỉnh) đến index cao nhất (đáy) để phục vụ unload chuẩn từ đỉnh xuống đáy
            blocksInGroup.Sort((a, b) => carrier.BlockController.GetBlockIndex(a).CompareTo(carrier.BlockController.GetBlockIndex(b)));

            // Kiểm tra xem block cao nhất trong nhóm gom có phải là block đỉnh hiện tại của Carrier không
            var currentTop = carrier.BlockController.GetCurrentBlock();
            if (currentTop == null || blocksInGroup[0] != currentTop)
            {
                // Bị đè bởi block khác màu ở phía trên
                isBlocked = true;
                return false;
            }

            // Kiểm tra tất cả các block trong nhóm có thể unload được không (ví dụ không bị đóng băng/khoá bởi mechanic)
            foreach (var block in blocksInGroup)
            {
                if (!block.CanBeginUnload())
                {
                    isBlocked = true;
                    return false;
                }
            }

            // Tính tổng số cube cần unload
            int groupCubeCount = 0;
            foreach (var block in blocksInGroup)
            {
                groupCubeCount += block.GetExpectedUnloadCount();
            }
            totalCubeCount += groupCubeCount;

            unloadGroups.Add(new CarrierUnloadGroup
            {
                Carrier = carrier,
                RunBlocks = blocksInGroup
            });
        }

        return unloadGroups.Count > 0;
    }

    private void AddToCarrierGroup(Dictionary<CarrierBase, HashSet<Block>> carrierGroups, CarrierBase carrier, Block block)
    {
        if (!carrierGroups.ContainsKey(carrier))
        {
            carrierGroups[carrier] = new HashSet<Block>();
        }
        carrierGroups[carrier].Add(block);
    }

    public List<Block> FindAllBlocksWithGroupId(int groupId)
    {
        var result = new List<Block>();
        if (CarrierSystem.Instance == null || CarrierSystem.Instance.SpawnedCarriers == null) return result;

        foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
        {
            if (carrier == null || carrier.BlockLayout == null) continue;
            for (int i = 0; i < carrier.MaxBlockCount; i++)
            {
                var block = carrier.BlockLayout.GetBlockByIndex(i);
                if (block != null && block.HasContent && block.GetLinkGroupId() == groupId)
                {
                    result.Add(block);
                }
            }
        }
        return result;
    }

    public Vector3 GetAnchorPositionForCarrierLink(CarrierBase carrier, Block linkBlock)
    {
        if (carrier == null || linkBlock == null) return Vector3.zero;

        // Lấy chuỗi cùng màu liên tiếp chứa block link
        var run = carrier.BlockController.GetContiguousSameColorRun(linkBlock);
        if (run == null || run.Count == 0) return linkBlock.AnimationPivot.position;

        Vector3 sum = Vector3.zero;
        foreach (var b in run)
        {
            if (b != null && b.AnimationPivot != null)
                sum += b.AnimationPivot.position;
            else if (b != null)
                sum += b.transform.position;
        }

        return sum / run.Count;
    }
}
