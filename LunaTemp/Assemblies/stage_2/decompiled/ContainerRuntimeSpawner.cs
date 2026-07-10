using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class ContainerRuntimeSpawner
{
	private const string RootName = "RuntimeContainers";

	private readonly List<ContainerMechanic> _spawnedContainers = new List<ContainerMechanic>();

	private Sequence _scaleSequence;

	public void SpawnContainers(LevelData levelData, IReadOnlyList<CarrierBase> carriers, CarrierConfigSO config, ConveyorPathRuntime splineContainer)
	{
		ClearContainers();
		List<ContainerLevelData> containerData = ((levelData != null && levelData.CarrierLayout != null) ? levelData.CarrierLayout.Containers : null);
		if (containerData == null || config == null || config.ContainerMechanic == null || splineContainer == null || !splineContainer.IsValid)
		{
			return;
		}
		Transform root = GetOrCreateRoot(splineContainer.Root);
		for (int i = 0; i < containerData.Count; i++)
		{
			ContainerLevelData data = containerData[i];
			if (data != null)
			{
				ContainerMechanic container = Object.Instantiate(config.ContainerMechanic, root);
				container.Configure(data.ContainerId, data.UnlockColor);
				container.transform.position = splineContainer.TransformPoint(data.Position);
				container.transform.rotation = splineContainer.Root.rotation * Quaternion.Euler(0f, data.RotationY, 0f);
				float scaleXZ = (Mathf.Approximately(data.ScaleXZ, 0f) ? 1f : data.ScaleXZ);
				Vector3 targetScale = new Vector3(scaleXZ, 1f, scaleXZ);
				container.SetTargetScale(targetScale);
				container.transform.localScale = targetScale;
				BindCarriers(container, carriers, data.CarrierIndexes);
				_spawnedContainers.Add(container);
			}
		}
	}

	public IEnumerator PlayScaleAnimation(LevelEntryAnimConfigSO animationConfig)
	{
		if (_spawnedContainers.Count != 0)
		{
			EnsureContainersAtFinalState();
		}
		yield break;
	}

	public void EnsureContainersAtFinalState()
	{
		for (int i = 0; i < _spawnedContainers.Count; i++)
		{
			ContainerMechanic container = _spawnedContainers[i];
			if (!(container == null) && !container.IsOpen)
			{
				container.transform.localScale = container.TargetScale;
			}
		}
	}

	public void ClearContainers()
	{
		if (_scaleSequence != null)
		{
			_scaleSequence.Kill();
			_scaleSequence = null;
		}
		for (int i = 0; i < _spawnedContainers.Count; i++)
		{
			if (_spawnedContainers[i] != null)
			{
				Object.Destroy(_spawnedContainers[i].gameObject);
			}
		}
		_spawnedContainers.Clear();
	}

	private static void BindCarriers(ContainerMechanic container, IReadOnlyList<CarrierBase> carriers, List<int> carrierIndexes)
	{
		if (container == null || carriers == null || carrierIndexes == null)
		{
			return;
		}
		for (int i = 0; i < carrierIndexes.Count; i++)
		{
			int index = carrierIndexes[i];
			if (index < 0 || index >= carriers.Count)
			{
				continue;
			}
			CarrierBase carrier = carriers[index];
			if (!(carrier == null))
			{
				CarrierContainerMember member = carrier.GetComponent<CarrierContainerMember>();
				if (member == null)
				{
					member = carrier.gameObject.AddComponent<CarrierContainerMember>();
				}
				member.SetCarrier(carrier);
				container.AddCarrier(member);
			}
		}
	}

	private static Transform GetOrCreateRoot(Transform parent)
	{
		Transform existing = parent.Find("RuntimeContainers");
		if (existing != null)
		{
			return existing;
		}
		Transform root = new GameObject("RuntimeContainers").transform;
		root.SetParent(parent, false);
		return root;
	}
}
