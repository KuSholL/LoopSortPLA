using System.Collections.Generic;
using UnityEngine;

public class CarrierBlockLayout : CarrierBlockLayoutBase
{
	[SerializeField]
	private Transform blockRoot;

	[SerializeField]
	private Block blockSlotPrefab;

	[Header("Layout Settings")]
	[SerializeField]
	private float spacing;

	[SerializeField]
	private float paddingTop;

	[SerializeField]
	private float paddingBottom;

	[SerializeField]
	private float paddingLeft;

	[SerializeField]
	private float paddingRight;

	[SerializeField]
	private TextAnchor childAlignment = TextAnchor.UpperCenter;

	[SerializeField]
	private Vector2 layoutArea = new Vector2(1f, 4f);

	[SerializeField]
	private List<Block> blocks = new List<Block>();

	public override List<Block> Blocks => blocks;

	public override Transform Root => GetBlockRoot();

	public void ArrangeBlocks()
	{
		if (blocks == null || blocks.Count == 0)
		{
			return;
		}
		int count = blocks.Count;
		Vector3 blockSize = blocks[0].GetBoundSize();
		float totalContentZ = (float)count * blockSize.z + (float)Mathf.Max(0, count - 1) * spacing;
		float totalContentX = blockSize.x;
		float startX = 0f;
		float startZ = 0f;
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
		switch (childAlignment)
		{
		case TextAnchor.UpperLeft:
		case TextAnchor.MiddleLeft:
		case TextAnchor.LowerLeft:
			startX = 0f - layoutArea.x * 0.5f + paddingLeft + totalContentX * 0.5f;
			break;
		case TextAnchor.UpperCenter:
		case TextAnchor.MiddleCenter:
		case TextAnchor.LowerCenter:
			startX = (paddingLeft - paddingRight) * 0.5f;
			break;
		case TextAnchor.UpperRight:
		case TextAnchor.MiddleRight:
		case TextAnchor.LowerRight:
			startX = layoutArea.x * 0.5f - paddingRight - totalContentX * 0.5f;
			break;
		}
		Transform root = GetBlockRoot();
		for (int i = 0; i < count; i++)
		{
			if (!(blocks[i] == null))
			{
				float offsetZ = startZ + (blockSize.z + spacing) * (float)i + blockSize.z * 0.5f;
				Vector3 localPos = new Vector3(startX, 0f, offsetZ);
				blocks[i].transform.SetParent(root, false);
				blocks[i].transform.localPosition = localPos;
			}
		}
	}

	public void EnsureBlockSlots(int targetCount)
	{
		Transform root = GetBlockRoot();
		while (blocks.Count < targetCount && !(blockSlotPrefab == null))
		{
			Block block = ((!Application.isPlaying) ? Object.Instantiate(blockSlotPrefab, root) : MonoSingleton<PoolManagerNew>.Instance.PopFromPool(blockSlotPrefab, root));
			block.name = blockSlotPrefab.name;
			blocks.Add(block);
		}
	}

	public override Block GetBlockByIndex(int index)
	{
		return (index >= 0 && index < blocks.Count) ? blocks[index] : null;
	}

	private Transform GetBlockRoot()
	{
		return blockRoot ? blockRoot : base.transform;
	}

	public override Block GetBlockAt(int index)
	{
		return (index >= 0 && index < blocks.Count) ? blocks[index] : null;
	}

	public void RemoveExtraBlocks(int targetCount)
	{
		while (blocks.Count > targetCount)
		{
			int lastIndex = blocks.Count - 1;
			Block block = blocks[lastIndex];
			blocks.RemoveAt(lastIndex);
			if ((bool)block)
			{
				if (Application.isPlaying)
				{
					MonoSingleton<PoolManagerNew>.Instance.PushToPool(block);
				}
				else
				{
					Object.DestroyImmediate(block.gameObject);
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Transform root = GetBlockRoot();
		if (!(root == null))
		{
			Gizmos.matrix = root.localToWorldMatrix;
			Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
			Gizmos.DrawWireCube(new Vector3(0f, 0f, layoutArea.y * 0.5f), new Vector3(layoutArea.x, 0.1f, layoutArea.y));
			Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
			float pX = layoutArea.x - paddingLeft - paddingRight;
			float pZ = layoutArea.y - paddingTop - paddingBottom;
			Vector3 pCenter = new Vector3((paddingLeft - paddingRight) * 0.5f, 0f, paddingTop + pZ * 0.5f);
			Gizmos.DrawWireCube(pCenter, new Vector3(pX, 0.05f, pZ));
		}
	}
}
