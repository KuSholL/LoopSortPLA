using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnerBlockAnimation : MonoBehaviour, ICustomTimeScaleTarget
{
    [SerializeField] private Transform startPoint;
    [SerializeField] private float staggerDelay = 0.05f;
    [SerializeField] private float flightHeight = 0.2f;
    [SerializeField] private float customDuration = 0.2f;

    private readonly List<AnimCube> _spawnedCubes = new List<AnimCube>();
    private Coroutine _animationRoutine;
    private float _currentTimeScale = 1f;
    private int _animationVersion;
    private Block _animatedBlock;

    public bool IsAnimating { get; private set; }

    private void OnEnable()
    {
        GameEventBus.OnInitLoadLevel += Cancel;
        if (CustomTimeScaleGroup.Instance != null)
            CustomTimeScaleGroup.Instance.AddTarget(this);
    }

    private void OnDisable()
    {
        GameEventBus.OnInitLoadLevel -= Cancel;
        if (CustomTimeScaleGroup.Instance != null)
            CustomTimeScaleGroup.Instance.RemoveTarget(this);
        Cancel();
    }

    public void SetCustomTimeScale(float timeScale)
    {
        _currentTimeScale = Mathf.Max(0f, timeScale);
    }

    public float GetTotalDuration(int cubeCount)
    {
        return cubeCount <= 0 ? 0f : (cubeCount - 1) * staggerDelay + customDuration;
    }

    public void Play(Block block, Action onComplete = null)
    {
        Cancel();
        _animationVersion++;
        _animationRoutine = StartCoroutine(PlayRoutine(block, onComplete, _animationVersion));
    }

    public void Cancel()
    {
        _animationVersion++;
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }
        if (_animatedBlock != null)
        {
            _animatedBlock.SetPhysicsCollidersEnabled(true);
            _animatedBlock = null;
        }
        ClearSpawnedCubes();
        IsAnimating = false;
    }

    private IEnumerator PlayRoutine(Block block, Action onComplete, int version)
    {
        if (block == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        var cubeCount = block.GetCurrentCubes();
        var color = block.GetBlockColorType();
        var prefab = ConfigManager.Instance != null
            ? ConfigManager.Instance.GetAnimCubePrefab()
            : null;
        if (cubeCount <= 0 || color == EBlockColorType.None || prefab == null)
        {
            block.transform.localScale = Vector3.one;
            block.SetVisualCubes(cubeCount, true);
            onComplete?.Invoke();
            yield break;
        }

        IsAnimating = true;
        _animatedBlock = block;
        block.transform.localPosition = Vector3.zero;
        block.SetPhysicsCollidersEnabled(false);
        block.transform.localScale = Vector3.zero;
        block.SetVisualCubes(0, true);

        var arrived = 0;
        var targetPosition = block.transform.position;
        if (block.AnimationPivot != null)
        {
            // The block is deliberately scaled to zero while the cubes fly in.
            // Transform.position on the pivot would therefore collapse to the
            // block root (inside the bottom of the spawner). Reconstruct the
            // unscaled pivot position exactly like the original gameplay.
            targetPosition += block.transform.rotation
                              * block.AnimationPivot.localPosition;
        }
        var spawnPosition = startPoint != null ? startPoint.position : transform.position;

        for (var i = 0; i < cubeCount; i++)
        {
            if (version != _animationVersion) yield break;
            var cube = PoolManagerNew.Instance.PopFromPool(prefab);
            if (cube != null)
            {
                cube.transform.position = spawnPosition;
                cube.InitCube(color);
                _spawnedCubes.Add(cube);
                if (CustomTimeScaleGroup.Instance != null)
                    CustomTimeScaleGroup.Instance.AddTarget(cube);

                cube.FlyToTarget(targetPosition, () =>
                {
                    if (version != _animationVersion) return;
                    arrived++;
                    if (arrived == 1)
                    {
                        block.transform.localScale = Vector3.one;
                        block.SetPhysicsCollidersEnabled(true);
                    }
                    block.SetVisualCubes(arrived, false);
                    ReturnCube(cube);
                    if (arrived == cubeCount) block.PlayMergeVfx();
                }, flightHeight, customDuration);
            }

            if (i < cubeCount - 1 && staggerDelay > 0f)
                yield return WaitCustomSeconds(staggerDelay, version);
        }

        while (version == _animationVersion && arrived < cubeCount)
            yield return null;

        if (version != _animationVersion) yield break;
        block.SetPhysicsCollidersEnabled(true);
        _animatedBlock = null;
        IsAnimating = false;
        _animationRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator WaitCustomSeconds(float duration, int version)
    {
        var elapsed = 0f;
        while (elapsed < duration && version == _animationVersion)
        {
            elapsed += Time.unscaledDeltaTime * _currentTimeScale;
            yield return null;
        }
    }

    private void ReturnCube(AnimCube cube)
    {
        if (cube == null) return;
        _spawnedCubes.Remove(cube);
        if (CustomTimeScaleGroup.Instance != null)
            CustomTimeScaleGroup.Instance.RemoveTarget(cube);
        if (PoolManagerNew.Instance != null)
            PoolManagerNew.Instance.PushToPool(cube);
    }

    private void ClearSpawnedCubes()
    {
        for (var i = _spawnedCubes.Count - 1; i >= 0; i--)
        {
            var cube = _spawnedCubes[i];
            if (cube == null) continue;
            if (CustomTimeScaleGroup.Instance != null)
                CustomTimeScaleGroup.Instance.RemoveTarget(cube);
            if (PoolManagerNew.Instance != null)
                PoolManagerNew.Instance.PushToPool(cube);
        }
        _spawnedCubes.Clear();
    }
}
