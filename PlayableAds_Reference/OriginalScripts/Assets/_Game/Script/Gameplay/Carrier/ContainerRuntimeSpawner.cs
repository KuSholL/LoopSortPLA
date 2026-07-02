using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.Splines;

public sealed class ContainerRuntimeSpawner
{
    private const string RootName = "RuntimeContainers";
    private readonly List<ContainerMechanic> _spawnedContainers = new();
    private CancellationTokenSource _containerAnimCts;

    public void SpawnContainers(
        LevelData levelData,
        IReadOnlyList<CarrierBase> carriers,
        CarrierConfigSO config,
        SplineContainer splineContainer)
    {
        var openContainerIds = new HashSet<int>();
        foreach (var container in _spawnedContainers)
        {
            if (container != null && (container.IsOpen || container.IsOpening))
            {
                openContainerIds.Add(container.ContainerId);
            }
        }

        ClearContainers();
        var containerData = levelData?.CarrierLayout?.Containers;
        if (containerData == null || config == null || splineContainer == null) return;
        var root = GetOrCreateRoot(splineContainer.transform);
        for (var i = 0; i < containerData.Count; i++)
        {
            var data = containerData[i];
            var container = CreateContainer(data, carriers, config, splineContainer, root);
            if (container != null && openContainerIds.Contains(data.ContainerId))
            {
                container.SetOpenSilently();
                container.transform.localScale = container.TargetScale;
            }
        }
    }

    public void ClearContainers()
    {
        _containerAnimCts?.Cancel();
        _containerAnimCts?.Dispose();
        _containerAnimCts = null;

        foreach (var container in _spawnedContainers)
        {
            if (container == null) continue;
            Object.Destroy(container.gameObject);
        }

        _spawnedContainers.Clear();
    }

    public async UniTask PlayContainersScaleAnimation(CancellationToken parentToken)
    {
        if (_spawnedContainers == null || _spawnedContainers.Count == 0) return;

        _containerAnimCts?.Cancel();
        _containerAnimCts?.Dispose();
        _containerAnimCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        var token = _containerAnimCts.Token;

        var animConfig = LevelManager.Instance.LevelEntryAnimConfig;
        var containerScaleStagger = animConfig.ContainerScaleStagger;
        var containerScaleDuration = animConfig.ContainerScaleDuration;
        var containerScaleEase = animConfig.ContainerScaleEase;

        try
        {
            var tasks = new List<UniTask>();
            for (var i = 0; i < _spawnedContainers.Count; i++)
            {
                var container = _spawnedContainers[i];
                if (container == null || container.IsOpen) continue;

                if (i > 0 && containerScaleStagger > 0f)
                {
                    await UniTask.Delay(
                        System.TimeSpan.FromSeconds(containerScaleStagger),
                        delayType: Cysharp.Threading.Tasks.DelayType.DeltaTime,
                        cancellationToken: token);
                }

                if (container != null)
                {
                    container.transform.localScale = Vector3.zero;
                    var handle = LMotion.Create(Vector3.zero, container.TargetScale, containerScaleDuration)
                        .WithEase(containerScaleEase)
                        .BindToLocalScale(container.transform);
                    container.SetScaleMotionHandle(handle);
                    tasks.Add(handle.ToUniTask(token));
                }
            }

            await UniTask.WhenAll(tasks);
        }
        catch (System.OperationCanceledException)
        {
            // Reset to zero or targetScale? Handled by Destruction/Next level.
        }
    }

    private ContainerMechanic CreateContainer(
        ContainerLevelData data,
        IReadOnlyList<CarrierBase> carriers,
        CarrierConfigSO config,
        SplineContainer splineContainer,
        Transform root)
    {
        if (data == null || config.ContainerMechanic == null) return null;
        var container = Object.Instantiate(config.ContainerMechanic, root);
        container.Configure(data.ContainerId, data.UnlockColor);
        container.transform.position = splineContainer.transform.TransformPoint(data.Position);
        container.transform.rotation = splineContainer.transform.rotation * Quaternion.Euler(0f, data.RotationY, 0f);
        var scaleXZ = data.ScaleXZ == 0f ? 1f : data.ScaleXZ;
        var targetScale = new Vector3(scaleXZ, 1f, scaleXZ);
        container.SetTargetScale(targetScale);
        container.transform.localScale = Vector3.zero;
        BindCarriers(container, carriers, data.CarrierIndexes);
        _spawnedContainers.Add(container);
        return container;
    }

    private static void BindCarriers(
        ContainerMechanic container,
        IReadOnlyList<CarrierBase> carriers,
        List<int> carrierIndexes)
    {
        if (container == null || carriers == null || carrierIndexes == null) return;
        foreach (var carrierIndex in carrierIndexes)
        {
            if (carrierIndex < 0 || carrierIndex >= carriers.Count) continue;
            var carrier = carriers[carrierIndex];
            if (carrier == null) continue;
            var member = carrier.GetComponent<CarrierContainerMember>();
            if (member == null) member = carrier.gameObject.AddComponent<CarrierContainerMember>();
            member.SetCarrier(carrier);
            container.AddCarrier(member);
        }
    }

    private static Transform GetOrCreateRoot(Transform parent)
    {
        var root = parent.Find(RootName);
        if (root != null) return root;
        var gameObject = new GameObject(RootName);
        gameObject.transform.SetParent(parent, false);
        return gameObject.transform;
    }
}
