using System.Collections.Generic;
using UnityEngine;

public class CarrierBlockLayout : CarrierBlockLayoutBase
{
    [SerializeField] private Transform blockRoot;
    [SerializeField] private Block blockSlotPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float spacing;
    [SerializeField] private float paddingTop;
    [SerializeField] private float paddingBottom;
    [SerializeField] private float paddingLeft;
    [SerializeField] private float paddingRight;
    [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperCenter;
    [SerializeField] private Vector2 layoutArea = new Vector2(1f, 4f);

    [SerializeField] private List<Block> blocks = new List<Block>();

    public override List<Block> Blocks => blocks;
    public override Transform Root => GetBlockRoot();
    public void ArrangeBlocks()
    {
        if (blocks == null || blocks.Count == 0) return;

        var count = blocks.Count;
        var blockSize = blocks[0].GetBoundSize();

        // Calculate total content size
        float totalContentZ = (count * blockSize.z) + (Mathf.Max(0, count - 1) * spacing);
        float totalContentX = blockSize.x;

        // Calculate base start position based on alignment
        float startX = 0f;
        float startZ = 0f;

        // Vertical Alignment (Z axis)
        switch (childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                startZ = paddingTop;
                break;
            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                startZ = (layoutArea.y - totalContentZ + paddingTop - paddingBottom) * 0.5f;
                break;
            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                startZ = layoutArea.y - paddingBottom - totalContentZ;
                break;
        }

        // Horizontal Alignment (X axis)
        switch (childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                startX = -(layoutArea.x * 0.5f) + paddingLeft + (totalContentX * 0.5f);
                break;
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                startX = (paddingLeft - paddingRight) * 0.5f;
                break;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                startX = (layoutArea.x * 0.5f) - paddingRight - (totalContentX * 0.5f);
                break;
        }

        var root = GetBlockRoot();
        for (var i = 0; i < count; i++)
        {
            if (blocks[i] == null) continue;

            var offsetZ = startZ + (blockSize.z + spacing) * i + (blockSize.z * 0.5f);
            var localPos = new Vector3(startX, 0f, offsetZ);

            blocks[i].transform.SetParent(root, false);
            blocks[i].transform.localPosition = localPos;
        }
    }

    public void EnsureBlockSlots(int targetCount)
    {
        var root = GetBlockRoot();
        while (blocks.Count < targetCount)
        {
            if (blockSlotPrefab == null) break;
            Block block;
            if (Application.isPlaying) block = PoolManagerNew.Instance.PopFromPool(blockSlotPrefab, root);
            else block = Instantiate(blockSlotPrefab, root);
            block.name = blockSlotPrefab.name;
            blocks.Add(block);
        }
    }

    public override Block GetBlockByIndex(int index)
    {
        return index >= 0 && index < blocks.Count ? blocks[index] : null;
    }

    private Transform GetBlockRoot()
    {
        return blockRoot ? blockRoot : transform;
    }

    public override Block GetBlockAt(int index)
    {
        return index >= 0 && index < blocks.Count ? blocks[index] : null;
    }

    public void RemoveExtraBlocks(int targetCount)
    {
        while (blocks.Count > targetCount)
        {
            var lastIndex = blocks.Count - 1;
            var block = blocks[lastIndex];
            blocks.RemoveAt(lastIndex);
            if (block)
            {
                if (Application.isPlaying) PoolManagerNew.Instance.PushToPool(block);
                else DestroyImmediate(block.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        var root = GetBlockRoot();
        if (root == null) return;

        Gizmos.matrix = root.localToWorldMatrix;

        // Draw Layout Area
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(new Vector3(0, 0, layoutArea.y * 0.5f), new Vector3(layoutArea.x, 0.1f, layoutArea.y));

        // Draw Padding Area
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        float pX = (layoutArea.x - paddingLeft - paddingRight);
        float pZ = (layoutArea.y - paddingTop - paddingBottom);
        Vector3 pCenter = new Vector3((paddingLeft - paddingRight) * 0.5f, 0, paddingTop + pZ * 0.5f);
        Gizmos.DrawWireCube(pCenter, new Vector3(pX, 0.05f, pZ));
    }
}
