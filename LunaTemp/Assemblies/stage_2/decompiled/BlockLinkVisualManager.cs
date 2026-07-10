using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlockLinkVisualManager : MonoSingleton<BlockLinkVisualManager>
{
	private struct LinkMember
	{
		public readonly CarrierBase Carrier;

		public readonly Block Block;

		public LinkMember(CarrierBase carrier, Block block)
		{
			Carrier = carrier;
			Block = block;
		}
	}

	[SerializeField]
	private BlockLinkVisual linkVisualPrefab;

	private readonly List<BlockLinkVisual> _activeVisuals = new List<BlockLinkVisual>();

	protected override void Awake()
	{
		base.Awake();
		if (linkVisualPrefab == null && MonoSingleton<ConfigManager>.Instance != null)
		{
			CarrierConfigSO config = MonoSingleton<ConfigManager>.Instance.GetCarrierConfig();
			if (config != null)
			{
				linkVisualPrefab = config.BlockLinkVisualPrefab;
			}
		}
	}

	private void OnEnable()
	{
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Combine(GameEventBus.OnInitLoadLevel, new Action(ClearAllVisuals));
		GameEventBus.OnCarrierUnload = (Action)Delegate.Combine(GameEventBus.OnCarrierUnload, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierUnloadDone = (Action)Delegate.Combine(GameEventBus.OnCarrierUnloadDone, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierPickupDone = (Action)Delegate.Combine(GameEventBus.OnCarrierPickupDone, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Combine(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
	}

	private void OnDisable()
	{
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Remove(GameEventBus.OnInitLoadLevel, new Action(ClearAllVisuals));
		GameEventBus.OnCarrierUnload = (Action)Delegate.Remove(GameEventBus.OnCarrierUnload, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierUnloadDone = (Action)Delegate.Remove(GameEventBus.OnCarrierUnloadDone, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierPickupDone = (Action)Delegate.Remove(GameEventBus.OnCarrierPickupDone, new Action(RefreshAllVisualPositions));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Remove(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
		ClearAllVisuals();
	}

	public void SetupLevelLinks()
	{
		ClearAllVisuals();
		if (!MonoSingleton<CarrierSystem>.HasInstance || MonoSingleton<CarrierSystem>.Instance.SpawnedCarriers == null)
		{
			return;
		}
		Dictionary<int, List<LinkMember>> groups = new Dictionary<int, List<LinkMember>>();
		IReadOnlyList<CarrierBase> carriers = MonoSingleton<CarrierSystem>.Instance.SpawnedCarriers;
		for (int carrierIndex = 0; carrierIndex < carriers.Count; carrierIndex++)
		{
			CarrierBase carrier = carriers[carrierIndex];
			if (carrier == null || carrier.BlockLayout == null)
			{
				continue;
			}
			HashSet<int> seenGroups = new HashSet<int>();
			for (int blockIndex = 0; blockIndex < carrier.MaxBlockCount; blockIndex++)
			{
				Block block = carrier.BlockLayout.GetBlockByIndex(blockIndex);
				if (block == null || !block.HasContent || !block.HasLinkGroupId())
				{
					continue;
				}
				int groupId = block.GetLinkGroupId();
				if (seenGroups.Add(groupId))
				{
					if (!groups.TryGetValue(groupId, out var members))
					{
						members = new List<LinkMember>();
						groups.Add(groupId, members);
					}
					members.Add(new LinkMember(carrier, block));
				}
			}
		}
		foreach (KeyValuePair<int, List<LinkMember>> item in groups)
		{
			CreateMinimumSpanningTree(item.Value);
		}
	}

	public void RefreshAllVisualPositions()
	{
		for (int i = _activeVisuals.Count - 1; i >= 0; i--)
		{
			BlockLinkVisual visual = _activeVisuals[i];
			if (visual == null)
			{
				_activeVisuals.RemoveAt(i);
			}
			else
			{
				visual.UpdatePositions();
			}
		}
	}

	public void ClearAllVisuals()
	{
		for (int i = 0; i < _activeVisuals.Count; i++)
		{
			BlockLinkVisual visual = _activeVisuals[i];
			if (!(visual == null))
			{
				if (MonoSingleton<PoolManagerNew>.Instance != null)
				{
					MonoSingleton<PoolManagerNew>.Instance.PushToPool(visual);
				}
				else
				{
					UnityEngine.Object.Destroy(visual.gameObject);
				}
			}
		}
		_activeVisuals.Clear();
	}

	private void CreateMinimumSpanningTree(List<LinkMember> members)
	{
		if (members == null || members.Count < 2 || linkVisualPrefab == null)
		{
			return;
		}
		int count = members.Count;
		bool[] included = new bool[count];
		float[] bestDistance = new float[count];
		int[] parent = new int[count];
		for (int k = 0; k < count; k++)
		{
			bestDistance[k] = float.MaxValue;
			parent[k] = -1;
		}
		bestDistance[0] = 0f;
		for (int step = 0; step < count; step++)
		{
			int next = -1;
			float nearest = float.MaxValue;
			for (int j = 0; j < count; j++)
			{
				if (!included[j] && !(bestDistance[j] >= nearest))
				{
					nearest = bestDistance[j];
					next = j;
				}
			}
			if (next < 0)
			{
				break;
			}
			included[next] = true;
			if (parent[next] >= 0)
			{
				CreateVisual(members[parent[next]], members[next]);
			}
			Vector3 nextPosition = GetCenter(members[next]);
			for (int i = 0; i < count; i++)
			{
				if (!included[i])
				{
					float distance = Vector3.SqrMagnitude(nextPosition - GetCenter(members[i]));
					if (!(distance >= bestDistance[i]))
					{
						bestDistance[i] = distance;
						parent[i] = next;
					}
				}
			}
		}
	}

	private void CreateVisual(LinkMember first, LinkMember second)
	{
		BlockLinkVisual visual = ((MonoSingleton<PoolManagerNew>.Instance != null) ? MonoSingleton<PoolManagerNew>.Instance.PopFromPool(linkVisualPrefab, base.transform) : UnityEngine.Object.Instantiate(linkVisualPrefab, base.transform));
		if (!(visual == null))
		{
			LunaMaterialUtility.NormalizeRenderers(visual.gameObject);
			visual.Setup(first.Carrier, first.Block, second.Carrier, second.Block);
			_activeVisuals.Add(visual);
		}
	}

	private static Vector3 GetCenter(LinkMember member)
	{
		return (member.Block.AnimationPivot != null) ? member.Block.AnimationPivot.position : member.Block.transform.position;
	}

	private void OnCarrierFinished(EBlockColorType colorType)
	{
		RefreshAllVisualPositions();
	}
}
