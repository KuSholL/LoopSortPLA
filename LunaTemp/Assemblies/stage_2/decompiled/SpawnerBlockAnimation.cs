using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpawnerBlockAnimation : MonoBehaviour, ICustomTimeScaleTarget
{
	[SerializeField]
	private Transform startPoint;

	[SerializeField]
	private float staggerDelay = 0.05f;

	[SerializeField]
	private float flightHeight = 0.2f;

	[SerializeField]
	private float customDuration = 0.2f;

	private readonly List<AnimCube> _spawnedCubes = new List<AnimCube>();

	private Coroutine _animationRoutine;

	private float _currentTimeScale = 1f;

	private int _animationVersion;

	private Block _animatedBlock;

	public bool IsAnimating { get; private set; }

	private void OnEnable()
	{
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Combine(GameEventBus.OnInitLoadLevel, new Action(Cancel));
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(this);
		}
	}

	private void OnDisable()
	{
		GameEventBus.OnInitLoadLevel = (Action)Delegate.Remove(GameEventBus.OnInitLoadLevel, new Action(Cancel));
		if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
		{
			MonoSingleton<CustomTimeScaleGroup>.Instance.RemoveTarget(this);
		}
		Cancel();
	}

	public void SetCustomTimeScale(float timeScale)
	{
		_currentTimeScale = Mathf.Max(0f, timeScale);
	}

	public float GetTotalDuration(int cubeCount)
	{
		return (cubeCount <= 0) ? 0f : ((float)(cubeCount - 1) * staggerDelay + customDuration);
	}

	public void Play(Block block, Action onComplete = null)
	{
		Cancel();
		if (block != null)
		{
			block.transform.localPosition = Vector3.zero;
			block.transform.localScale = Vector3.one;
			block.SetPhysicsCollidersEnabled(true);
			block.SetVisualCubes(block.GetCurrentCubes(), true);
		}
		onComplete?.Invoke();
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
		int cubeCount = block.GetCurrentCubes();
		EBlockColorType color = block.GetBlockColorType();
		AnimCube prefab = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetAnimCubePrefab() : null);
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
		int arrived = 0;
		Vector3 targetPosition = block.transform.position;
		if (block.AnimationPivot != null)
		{
			targetPosition += block.transform.rotation * block.AnimationPivot.localPosition;
		}
		Vector3 spawnPosition = ((startPoint != null) ? startPoint.position : base.transform.position);
		for (int i = 0; i < cubeCount; i++)
		{
			if (version != _animationVersion)
			{
				yield break;
			}
			AnimCube cube = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(prefab);
			if (cube != null)
			{
				cube.transform.position = spawnPosition;
				cube.InitCube(color);
				_spawnedCubes.Add(cube);
				if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
				{
					MonoSingleton<CustomTimeScaleGroup>.Instance.AddTarget(cube);
				}
				cube.FlyToTarget(targetPosition, delegate
				{
					if (version == _animationVersion)
					{
						int num = arrived;
						arrived = num + 1;
						if (arrived == 1)
						{
							block.transform.localScale = Vector3.one;
							block.SetPhysicsCollidersEnabled(true);
						}
						block.SetVisualCubes(arrived);
						ReturnCube(cube);
						if (arrived == cubeCount)
						{
							block.PlayMergeVfx();
						}
					}
				}, flightHeight, customDuration);
			}
			if (i < cubeCount - 1 && staggerDelay > 0f)
			{
				yield return WaitCustomSeconds(staggerDelay, version);
			}
		}
		while (version == _animationVersion && arrived < cubeCount)
		{
			yield return null;
		}
		if (version == _animationVersion)
		{
			block.SetPhysicsCollidersEnabled(true);
			_animatedBlock = null;
			IsAnimating = false;
			_animationRoutine = null;
			onComplete?.Invoke();
		}
	}

	private IEnumerator WaitCustomSeconds(float duration, int version)
	{
		float elapsed = 0f;
		while (elapsed < duration && version == _animationVersion)
		{
			elapsed += Time.unscaledDeltaTime * _currentTimeScale;
			yield return null;
		}
	}

	private void ReturnCube(AnimCube cube)
	{
		if (!(cube == null))
		{
			_spawnedCubes.Remove(cube);
			if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
			{
				MonoSingleton<CustomTimeScaleGroup>.Instance.RemoveTarget(cube);
			}
			if (MonoSingleton<PoolManagerNew>.Instance != null)
			{
				MonoSingleton<PoolManagerNew>.Instance.PushToPool(cube);
			}
		}
	}

	private void ClearSpawnedCubes()
	{
		for (int i = _spawnedCubes.Count - 1; i >= 0; i--)
		{
			AnimCube cube = _spawnedCubes[i];
			if (!(cube == null))
			{
				if (MonoSingleton<CustomTimeScaleGroup>.Instance != null)
				{
					MonoSingleton<CustomTimeScaleGroup>.Instance.RemoveTarget(cube);
				}
				if (MonoSingleton<PoolManagerNew>.Instance != null)
				{
					MonoSingleton<PoolManagerNew>.Instance.PushToPool(cube);
				}
			}
		}
		_spawnedCubes.Clear();
	}
}
