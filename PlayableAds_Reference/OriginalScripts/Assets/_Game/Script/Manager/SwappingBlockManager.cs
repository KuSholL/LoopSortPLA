using System;
using System.Collections.Generic;
using UnityEngine;

public class SwappingBlockManager : MonoSingleton<SwappingBlockManager>
{
    public class SwappingPair
    {
        public int GroupId;
        public Block BlockA;
        public Block BlockB;
        public bool IsActive = true;

        public void SwapColors(ColorConfigSO colorConfig)
        {
            if (!IsActive || BlockA == null || BlockB == null) return;
            
            var tempColor = BlockA.GetBlockColorType();
            BlockA.UpdateColorType(BlockB.GetBlockColorType(), colorConfig);
            BlockB.UpdateColorType(tempColor, colorConfig);

            UpdateArrowColors(colorConfig);

            BlockA.PlaySwapRotateAnimation();
            BlockB.PlaySwapRotateAnimation();
        }

        public void UpdateArrowColors(ColorConfigSO colorConfig)
        {
            if (!IsActive || BlockA == null || BlockB == null) return;
            
            BlockA.SetSwapArrowColor(BlockB.GetBlockColorType(), colorConfig);
            BlockB.SetSwapArrowColor(BlockA.GetBlockColorType(), colorConfig);
        }

        public void Disable()
        {
            IsActive = false;
            if (BlockA != null) BlockA.DisableSwappingMechanic();
            if (BlockB != null) BlockB.DisableSwappingMechanic();
        }
    }

    private readonly List<SwappingPair> _activePairs = new();

    private void OnEnable()
    {
        GameEventBus.OnInitLoadLevel += ClearAll;
    }

    private void OnDisable()
    {
        GameEventBus.OnInitLoadLevel -= ClearAll;
    }

    private void OnDestroy()
    {
        GameEventBus.OnInitLoadLevel -= ClearAll;
    }

    private void ClearAll()
    {
        _activePairs.Clear();
    }

    public void InitializeLevel()
    {
        _activePairs.Clear();
        if (CarrierSystem.Instance == null || CarrierSystem.Instance.SpawnedCarriers == null) return;

        var blocksByGroup = new Dictionary<int, List<Block>>();

        foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
        {
            if (carrier == null || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) continue;
            foreach (var block in carrier.BlockLayout.Blocks)
            {
                if (block == null) continue;
                var swapGroupId = block.GetSwapGroupId();
                if (swapGroupId >= 0)
                {
                    if (!blocksByGroup.ContainsKey(swapGroupId))
                    {
                        blocksByGroup[swapGroupId] = new List<Block>();
                    }
                    blocksByGroup[swapGroupId].Add(block);
                }
            }
        }

        var colorConfig = ConfigManager.Instance != null ? ConfigManager.Instance.GetColorConfig() : null;
        foreach (var kvp in blocksByGroup)
        {
            if (kvp.Value.Count == 2)
            {
                var pair = new SwappingPair
                {
                    GroupId = kvp.Key,
                    BlockA = kvp.Value[0],
                    BlockB = kvp.Value[1],
                    IsActive = true
                };
                _activePairs.Add(pair);
                pair.UpdateArrowColors(colorConfig);
            }
            else
            {
                Debug.LogWarning($"[SwappingBlockManager] Group {kvp.Key} has {kvp.Value.Count} blocks. Only exactly 2 blocks are supported for swapping.");
            }
        }
    }

    public void SwapAllActivePairs()
    {
        if (ConfigManager.Instance == null) return;
        var colorConfig = ConfigManager.Instance.GetColorConfig();
        if (colorConfig == null) return;

        var carriersToCheck = new HashSet<CarrierBase>();

        foreach (var pair in _activePairs)
        {
            if (pair.IsActive && pair.BlockA != null && pair.BlockB != null)
            {
                pair.SwapColors(colorConfig);
                if (pair.BlockA.OwnerCarrier != null) carriersToCheck.Add(pair.BlockA.OwnerCarrier);
                if (pair.BlockB.OwnerCarrier != null) carriersToCheck.Add(pair.BlockB.OwnerCarrier);
            }
        }

        foreach (var carrier in carriersToCheck)
        {
            int mergedBlockIndex = -1;
            if (carrier.BlockLayout != null && carrier.BlockLayout.Blocks != null)
            {
                int count = carrier.MaxBlockCount;
                for (int i = 0; i < count; i++)
                {
                    var block = carrier.BlockLayout.GetBlockByIndex(i);
                    if (block == null || !block.HasContent || !block.HasSwappingMechanic()) continue;

                    var color = block.GetBlockColorType();
                    if (color == EBlockColorType.None) continue;

                    bool adjacentMatches = false;
                    if (i > 0)
                    {
                        var left = carrier.BlockLayout.GetBlockByIndex(i - 1);
                        if (left != null && left.HasContent && left.GetBlockColorType() == color)
                        {
                            adjacentMatches = true;
                        }
                    }
                    if (i < count - 1)
                    {
                        var right = carrier.BlockLayout.GetBlockByIndex(i + 1);
                        if (right != null && right.HasContent && right.GetBlockColorType() == color)
                        {
                            adjacentMatches = true;
                        }
                    }

                    if (adjacentMatches)
                    {
                        mergedBlockIndex = i;
                        DisablePair(block.GetSwapGroupId());
                        block.PlayMergeVfx();
                        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_merge);
                    }
                }
            }

            if (carrier.LinkedBlockVisualController != null)
            {
                if (mergedBlockIndex >= 0)
                {
                    carrier.LinkedBlockVisualController.RefreshAfterReceive(mergedBlockIndex);
                }
                else
                {
                    carrier.LinkedBlockVisualController.Refresh(true);
                }
            }
        }
    }

    public void RebindBlockReferences()
    {
        if (CarrierSystem.Instance == null || CarrierSystem.Instance.SpawnedCarriers == null) return;

        var blocksByGroup = new Dictionary<int, List<Block>>();

        foreach (var carrier in CarrierSystem.Instance.SpawnedCarriers)
        {
            if (carrier == null || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) continue;
            foreach (var block in carrier.BlockLayout.Blocks)
            {
                if (block == null) continue;
                var swapGroupId = block.GetSwapGroupId();
                if (swapGroupId >= 0 && block.HasSwappingMechanic())
                {
                    if (!blocksByGroup.ContainsKey(swapGroupId))
                    {
                        blocksByGroup[swapGroupId] = new List<Block>();
                    }
                    blocksByGroup[swapGroupId].Add(block);
                }
            }
        }

        var colorConfig = ConfigManager.Instance != null ? ConfigManager.Instance.GetColorConfig() : null;
        for (int i = _activePairs.Count - 1; i >= 0; i--)
        {
            var pair = _activePairs[i];
            if (blocksByGroup.TryGetValue(pair.GroupId, out var blocks))
            {
                if (blocks.Count == 2)
                {
                    pair.BlockA = blocks[0];
                    pair.BlockB = blocks[1];
                    pair.UpdateArrowColors(colorConfig);
                }
                else
                {
                    pair.Disable();
                    _activePairs.RemoveAt(i);
                }
            }
            else
            {
                pair.Disable();
                _activePairs.RemoveAt(i);
            }
        }
    }

    public void DisablePair(int swapGroupId)
    {
        if (swapGroupId < 0) return;
        var pair = _activePairs.Find(p => p.GroupId == swapGroupId);
        if (pair != null)
        {
            pair.Disable();
            _activePairs.Remove(pair);
        }
    }

    public void CheckAndDisableSwappingBlocksOnCarrier(CarrierBase carrier, bool playVisuals = false)
    {
        if (carrier == null || carrier.BlockLayout == null || carrier.BlockLayout.Blocks == null) return;
        int count = carrier.MaxBlockCount;
        for (int i = 0; i < count; i++)
        {
            var block = carrier.BlockLayout.GetBlockByIndex(i);
            if (block == null || !block.HasContent || !block.HasSwappingMechanic()) continue;

            var color = block.GetBlockColorType();
            if (color == EBlockColorType.None) continue;

            bool adjacentMatches = false;
            // Check left neighbor
            if (i > 0)
            {
                var left = carrier.BlockLayout.GetBlockByIndex(i - 1);
                if (left != null && left.HasContent && left.GetBlockColorType() == color)
                {
                    adjacentMatches = true;
                }
            }

            // Check right neighbor
            if (i < count - 1)
            {
                var right = carrier.BlockLayout.GetBlockByIndex(i + 1);
                if (right != null && right.HasContent && right.GetBlockColorType() == color)
                {
                    adjacentMatches = true;
                }
            }

            if (adjacentMatches)
            {
                DisablePair(block.GetSwapGroupId());
                if (playVisuals)
                {
                    block.PlayMergeVfx();
                    SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_merge);
                }
            }
        }
    }
}
