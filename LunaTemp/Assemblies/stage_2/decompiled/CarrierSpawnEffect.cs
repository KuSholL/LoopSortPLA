using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CarrierSpawnEffect : MonoBehaviour
{
	[Header("VFX Settings")]
	[SerializeField]
	private ParticleSystem spawnVfxPrefab;

	[SerializeField]
	private float duration = 0.5f;

	private Tween _spawnTween;

	private Coroutine _spawnRoutine;

	private void OnDisable()
	{
		if (_spawnTween != null)
		{
			_spawnTween.Kill();
			_spawnTween = null;
		}
		if (_spawnRoutine != null)
		{
			StopCoroutine(_spawnRoutine);
			_spawnRoutine = null;
		}
	}

	public void PlaySpawnEffect()
	{
		if (_spawnRoutine != null)
		{
			StopCoroutine(_spawnRoutine);
			_spawnRoutine = null;
		}
		_spawnRoutine = StartCoroutine(PlaySpawnEffectRoutine());
	}

	private IEnumerator PlaySpawnEffectRoutine()
	{
		base.transform.localScale = Vector3.one;
		yield break;
	}
}
