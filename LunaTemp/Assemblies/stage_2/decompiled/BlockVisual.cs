using System.Collections;
using UnityEngine;

public class BlockVisual : MonoBehaviour
{
	[SerializeField]
	private BlockSolidVisual fixedVisual;

	[SerializeField]
	private Material normalMaterial;

	[SerializeField]
	private Material mechanicMaterial;

	[SerializeField]
	private ParticleSystem hiddenRevealVfxPrefab;

	[SerializeField]
	private ParticleSystem mergeVfx;

	[SerializeField]
	private Transform parrentVFX;

	private bool _wasShowingMechanicVisual;

	private Block _parentBlock;

	public BlockSolidVisual FixedVisual => fixedVisual;

	private Block ParentBlock
	{
		get
		{
			if (_parentBlock == null)
			{
				_parentBlock = GetComponent<Block>();
			}
			return _parentBlock;
		}
	}

	public void ApplyVisualState(bool hasContent, bool showSolid, EBlockColorType colorType, bool hasMechanicVisual, bool hasKeyVisual, float progress, bool suppressProgressAnimation = false, bool forceFullAnimation = false, EBlockColorType keyColorType = EBlockColorType.None)
	{
		BlockSolidVisual visual = fixedVisual;
		if (!visual)
		{
			return;
		}
		bool shouldShow = hasContent && (showSolid || hasMechanicVisual);
		bool shouldShowKey = hasContent && hasKeyVisual && showSolid;
		bool shouldPlayHiddenRevealVfx = shouldShow && _wasShowingMechanicVisual && !hasMechanicVisual;
		visual.SetQuestionMark(hasMechanicVisual);
		visual.SetKeyVisible(shouldShowKey);
		bool shouldShowSwapArrow = hasContent && ParentBlock != null && ParentBlock.HasSwappingMechanic();
		visual.SetSwapArrowVisible(shouldShowSwapArrow);
		if (shouldShowKey && keyColorType != EBlockColorType.None)
		{
			visual.SetKeyTexture(keyColorType);
		}
		if (!shouldShow)
		{
			visual.ResetVisual(normalMaterial);
			visual.SetVisible(false);
			_wasShowingMechanicVisual = false;
			return;
		}
		if (hasMechanicVisual)
		{
			visual.ApplyMechanicVisual(mechanicMaterial);
		}
		else
		{
			ColorEntry entry = GetColorEntry(colorType);
			visual.ApplyNormalVisual(normalMaterial, entry);
		}
		ApplyColorToMergeVfx(colorType);
		visual.SetProgress(progress, suppressProgressAnimation, forceFullAnimation);
		visual.SetVisible(true);
		if (shouldPlayHiddenRevealVfx)
		{
			PlayHiddenRevealVfx();
			MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_hiddencube);
		}
		_wasShowingMechanicVisual = hasMechanicVisual;
	}

	public void SetSwapArrowColor(EBlockColorType colorType, ColorConfigSO colorConfig)
	{
		if (fixedVisual != null)
		{
			fixedVisual.SetSwapArrowColor(colorType, colorConfig);
		}
	}

	private void ApplyColorToMergeVfx(EBlockColorType colorType)
	{
		if (mergeVfx == null || colorType == EBlockColorType.None)
		{
			return;
		}
		ColorEntry entry = GetColorEntry(colorType);
		if (entry != null)
		{
			ParticleSystem.MainModule main = mergeVfx.main;
			main.startColor = entry.Color;
			ParticleSystemRenderer psRenderer = mergeVfx.GetComponent<ParticleSystemRenderer>();
			if (psRenderer != null)
			{
				psRenderer.ApplyColorEntry(entry);
			}
		}
	}

	public void PlayMergeVfx()
	{
		if (!(mergeVfx == null))
		{
			mergeVfx.gameObject.SetActive(false);
			mergeVfx.gameObject.SetActive(true);
			mergeVfx.Play(true);
		}
	}

	private static ColorEntry GetColorEntry(EBlockColorType colorType)
	{
		ColorConfigSO config = MonoSingleton<ConfigManager>.Instance?.GetColorConfig();
		return config ? config.GetColorEntry(colorType) : PlayableColorFallback.CreateColorEntry(colorType);
	}

	private void PlayHiddenRevealVfx()
	{
		if (!(hiddenRevealVfxPrefab == null) && Application.isPlaying && !(MonoSingleton<PoolManagerNew>.Instance == null))
		{
			ParticleSystem spawnedVfx = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(hiddenRevealVfxPrefab, parrentVFX);
			spawnedVfx.transform.localPosition = Vector3.zero;
			spawnedVfx.transform.localRotation = Quaternion.identity;
			spawnedVfx.Play(true);
			StartCoroutine(ReturnHiddenRevealVfxToPool(spawnedVfx));
		}
	}

	private IEnumerator ReturnHiddenRevealVfxToPool(ParticleSystem spawnedVfx)
	{
		if (!(spawnedVfx == null))
		{
			ParticleSystem.MainModule main = spawnedVfx.main;
			float duration = main.duration + main.startLifetime.constantMax;
			if (duration > 0f)
			{
				yield return new WaitForSeconds(duration);
			}
			if (spawnedVfx != null && MonoSingleton<PoolManagerNew>.Instance != null)
			{
				MonoSingleton<PoolManagerNew>.Instance.PushToPool(spawnedVfx);
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)mergeVfx)
		{
			mergeVfx.gameObject.SetActive(false);
		}
	}

	private void OnDisable()
	{
		_wasShowingMechanicVisual = false;
	}
}
