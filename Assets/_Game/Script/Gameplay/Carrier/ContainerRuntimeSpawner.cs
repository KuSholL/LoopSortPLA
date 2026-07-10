using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class ContainerRuntimeSpawner
{
    private const string RootName = "RuntimeContainers";
    private readonly List<ContainerMechanic> _spawnedContainers = new List<ContainerMechanic>();
    private Sequence _scaleSequence;

    public void SpawnContainers(
        LevelData levelData,
        IReadOnlyList<CarrierBase> carriers,
        CarrierConfigSO config,
        ConveyorPathRuntime splineContainer)
    {
        ClearContainers();
        var containerData = levelData != null && levelData.CarrierLayout != null
            ? levelData.CarrierLayout.Containers
            : null;
        if (containerData == null
            || config == null
            || config.ContainerMechanic == null
            || splineContainer == null
            || !splineContainer.IsValid)
            return;

        var root = GetOrCreateRoot(splineContainer.Root);
        for (var i = 0; i < containerData.Count; i++)
        {
            var data = containerData[i];
            if (data == null) continue;
            var container = Object.Instantiate(config.ContainerMechanic, root);
            container.Configure(data.ContainerId, data.UnlockColor);
            container.transform.position = splineContainer.TransformPoint(data.Position);
            container.transform.rotation = splineContainer.Root.rotation
                                           * Quaternion.Euler(0f, data.RotationY, 0f);
            var scaleXZ = Mathf.Approximately(data.ScaleXZ, 0f) ? 1f : data.ScaleXZ;
            var targetScale = new Vector3(scaleXZ, 1f, scaleXZ);
            container.SetTargetScale(targetScale);
#if UNITY_LUNA
            container.transform.localScale = targetScale;
#else
            container.transform.localScale = Vector3.zero;
#endif
            BindCarriers(container, carriers, data.CarrierIndexes);
            _spawnedContainers.Add(container);
        }
    }

    public IEnumerator PlayScaleAnimation(LevelEntryAnimConfigSO animationConfig)
    {
        if (_spawnedContainers.Count == 0) yield break;
#if UNITY_LUNA
        EnsureContainersAtFinalState();
        yield break;
#endif

        var duration = animationConfig != null ? animationConfig.ContainerScaleDuration : 0.3f;
        var stagger = animationConfig != null ? animationConfig.ContainerScaleStagger : 0.1f;
        var ease = animationConfig != null ? animationConfig.ContainerScaleEase : DG.Tweening.Ease.OutBack;
        for (var i = 0; i < _spawnedContainers.Count; i++)
        {
            var container = _spawnedContainers[i];
            if (container == null || container.IsOpen) continue;
            container.transform.localScale = Vector3.zero;
        }

        if (_scaleSequence != null)
        {
            _scaleSequence.Kill();
            _scaleSequence = null;
        }

        _scaleSequence = DOTween.Sequence().SetUpdate(true);
        for (var i = 0; i < _spawnedContainers.Count; i++)
        {
            var container = _spawnedContainers[i];
            if (container == null || container.IsOpen) continue;
            var tween = container.transform
                .DOScale(container.TargetScale, duration)
                .SetEase(ease);
            container.SetScaleTween(tween);
            _scaleSequence.Insert(i * stagger, tween);
        }

        var elapsed = 0f;
        var timeout = Mathf.Max(0.05f, duration + stagger * Mathf.Max(0, _spawnedContainers.Count - 1) + 0.25f);
        while (_scaleSequence != null
               && _scaleSequence.IsActive()
               && !_scaleSequence.IsComplete()
               && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_scaleSequence != null && _scaleSequence.IsActive() && !_scaleSequence.IsComplete())
        {
            _scaleSequence.Kill();
        }

        _scaleSequence = null;
        EnsureContainersAtFinalState();
    }

    public void EnsureContainersAtFinalState()
    {
        for (var i = 0; i < _spawnedContainers.Count; i++)
        {
            var container = _spawnedContainers[i];
            if (container == null || container.IsOpen) continue;
            container.transform.localScale = container.TargetScale;
        }
    }

    public void ClearContainers()
    {
        if (_scaleSequence != null)
        {
            _scaleSequence.Kill();
            _scaleSequence = null;
        }

        for (var i = 0; i < _spawnedContainers.Count; i++)
        {
            if (_spawnedContainers[i] != null)
                Object.Destroy(_spawnedContainers[i].gameObject);
        }
        _spawnedContainers.Clear();
    }

    private static void BindCarriers(
        ContainerMechanic container,
        IReadOnlyList<CarrierBase> carriers,
        List<int> carrierIndexes)
    {
        if (container == null || carriers == null || carrierIndexes == null) return;
        for (var i = 0; i < carrierIndexes.Count; i++)
        {
            var index = carrierIndexes[i];
            if (index < 0 || index >= carriers.Count) continue;
            var carrier = carriers[index];
            if (carrier == null) continue;
            var member = carrier.GetComponent<CarrierContainerMember>();
            if (member == null) member = carrier.gameObject.AddComponent<CarrierContainerMember>();
            member.SetCarrier(carrier);
            container.AddCarrier(member);
        }
    }

    private static Transform GetOrCreateRoot(Transform parent)
    {
        var existing = parent.Find(RootName);
        if (existing != null) return existing;
        var root = new GameObject(RootName).transform;
        root.SetParent(parent, false);
        return root;
    }
}
