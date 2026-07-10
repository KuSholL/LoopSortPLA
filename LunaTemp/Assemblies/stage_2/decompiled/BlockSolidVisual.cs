using DG.Tweening;
using UnityEngine;

public class BlockSolidVisual : MonoBehaviour
{
	[SerializeField]
	private Block parentBlock;

	[SerializeField]
	private MeshRenderer meshRenderer;

	[SerializeField]
	private SpriteRenderer questionMarkRenderer;

	[SerializeField]
	private Renderer[] keyRenderers;

	[SerializeField]
	private Renderer swapArrowRenderer;

	[SerializeField]
	private BlockSolidProgressAnimator progressAnimator;

	[SerializeField]
	private float progressTweenDuration = 0.2f;

	[Header("First Cube Scale Config")]
	[SerializeField]
	private bool useFirstCubeScaleConfig = true;

	[SerializeField]
	[Range(0f, 1f)]
	private float firstCubeScalePercent = 0.05f;

	private Tween _progressTween;

	private Tween _swapRotateTween;

	private Collider[] _modelColliders;

	private float _currentProgress;

	public Renderer KeyRenderer => (keyRenderers != null && keyRenderers.Length != 0) ? keyRenderers[0] : null;

	private void OnValidate()
	{
		parentBlock = GetComponentInParent<Block>();
	}

	private void Awake()
	{
		if (parentBlock == null)
		{
			parentBlock = GetComponentInParent<Block>();
		}
		if ((bool)meshRenderer)
		{
			_currentProgress = Mathf.Clamp01(meshRenderer.transform.localScale.z);
		}
	}

	private void OnDisable()
	{
		CancelProgressTween();
	}

	private void Start()
	{
		questionMarkRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
	}

	private void SetupMaterial(ColorEntry colorEntry)
	{
		if (colorEntry != null)
		{
			meshRenderer.ApplyColorEntry(colorEntry);
		}
	}

	public void ApplyNormalVisual(Material material, ColorEntry colorEntry)
	{
		SetSharedMaterial(material);
		SetupMaterial(colorEntry);
	}

	public void ApplyMechanicVisual(Material material)
	{
		SetSharedMaterial(material);
		ClearPropertyBlocks();
	}

	public void ResetVisual(Material defaultMaterial)
	{
		SetSharedMaterial(defaultMaterial);
		ClearPropertyBlocks();
	}

	public void SetVisible(bool isVisible)
	{
		meshRenderer.enabled = isVisible;
		SetModelCollidersEnabled(isVisible && _currentProgress > 0f);
	}

	public void SetQuestionMark(bool isHiddenBlock)
	{
		questionMarkRenderer.enabled = isHiddenBlock;
		questionMarkRenderer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
	}

	public void SetKeyVisible(bool isVisible)
	{
		if (keyRenderers == null)
		{
			return;
		}
		Renderer[] array = keyRenderers;
		foreach (Renderer r in array)
		{
			if ((bool)r)
			{
				r.gameObject.SetActive(isVisible);
				r.enabled = isVisible;
			}
		}
	}

	public void SetSwapArrowVisible(bool isVisible)
	{
		if (swapArrowRenderer != null)
		{
			swapArrowRenderer.gameObject.SetActive(isVisible);
			swapArrowRenderer.enabled = isVisible;
		}
	}

	public void SetSwapArrowColor(EBlockColorType colorType, ColorConfigSO colorConfig)
	{
		if (!(swapArrowRenderer == null))
		{
			ColorEntry entry = ((colorConfig != null) ? colorConfig.GetColorEntry(colorType) : PlayableColorFallback.CreateColorEntry(colorType));
			if (entry != null)
			{
				swapArrowRenderer.ApplyColorEntry(entry);
			}
		}
	}

	public void PlaySwapRotateAnimation()
	{
		if (!(swapArrowRenderer == null))
		{
			Transform arrowTransform = swapArrowRenderer.transform;
			Vector3 currentRotation = arrowTransform.localEulerAngles;
			if (_swapRotateTween != null)
			{
				_swapRotateTween.Kill();
			}
			_swapRotateTween = arrowTransform.DOLocalRotate(new Vector3(90f, currentRotation.y + 180f, 0f), 0.3f).SetEase(Ease.OutQuad).OnComplete(delegate
			{
				_swapRotateTween = null;
			});
		}
	}

	public void SetKeyTexture(EBlockColorType colorType)
	{
		if (keyRenderers == null)
		{
			return;
		}
		StylizedColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetStylizedColorConfig() : null);
		StylizedColorEntry entry = ((config != null) ? config.GetColorEntry(colorType) : PlayableStylizedColorFallback.CreateColorEntry(colorType));
		if (entry == null)
		{
			return;
		}
		int colorId = Shader.PropertyToID("_Color");
		int shadowColorId = Shader.PropertyToID("_ShadowColor");
		int specularColorId = Shader.PropertyToID("_SpecularColor");
		int reflectColorId = Shader.PropertyToID("_ReflectColor");
		Renderer[] array = keyRenderers;
		foreach (Renderer r in array)
		{
			if (!r)
			{
				continue;
			}
			int matCount = ((r.sharedMaterials != null) ? r.sharedMaterials.Length : 0);
			if (matCount >= 3)
			{
				int[] targetIndices = new int[2] { 0, 2 };
				int[] array2 = targetIndices;
				foreach (int i in array2)
				{
					r.ApplyColor(colorId, entry.Color, i);
					r.ApplyColor(shadowColorId, entry.ShadowColor, i);
					r.ApplyColor(specularColorId, entry.SpecularColor, i);
					r.ApplyColor(reflectColorId, entry.ReflectColor, i);
				}
			}
			else
			{
				r.ApplyColor(colorId, entry.Color, 0);
				r.ApplyColor(shadowColorId, entry.ShadowColor, 0);
				r.ApplyColor(specularColorId, entry.SpecularColor, 0);
				r.ApplyColor(reflectColorId, entry.ReflectColor, 0);
			}
		}
	}

	public void SetProgress(float progress, bool suppressAnimation = false, bool forceFullAnimation = false)
	{
		if ((bool)meshRenderer)
		{
			progress = Mathf.Clamp01(progress);
			if (suppressAnimation || !Application.isPlaying || progressTweenDuration <= 0f || progress <= 0f)
			{
				CancelProgressTween();
				ApplyScaleProgress(progress);
			}
			else
			{
				StartScaleProgressTween(progress);
			}
			if (suppressAnimation)
			{
				progressAnimator?.SetProgress(progress, true);
			}
			else
			{
				progressAnimator?.SetProgress(progress, false, forceFullAnimation);
			}
		}
	}

	private void StartScaleProgressTween(float targetProgress)
	{
		CancelProgressTween();
		_progressTween = DOTween.To(() => _currentProgress, ApplyScaleProgress, targetProgress, progressTweenDuration).SetEase(Ease.OutQuad).SetTarget(this);
	}

	private void ApplyScaleProgress(float progress)
	{
		if ((bool)meshRenderer)
		{
			_currentProgress = progress;
			float mappedProgress = GetMappedProgress(progress);
			bool hasVisibleVolume = progress > 0f && mappedProgress > 0f;
			SetModelCollidersEnabled(hasVisibleVolume);
			meshRenderer.transform.localScale = (hasVisibleVolume ? new Vector3(1f, 1f, mappedProgress) : Vector3.zero);
		}
	}

	private void SetModelCollidersEnabled(bool isEnabled)
	{
		if (!meshRenderer)
		{
			return;
		}
		if (_modelColliders == null)
		{
			_modelColliders = meshRenderer.GetComponentsInChildren<Collider>(true);
		}
		for (int i = 0; i < _modelColliders.Length; i++)
		{
			Collider target = _modelColliders[i];
			if (target != null)
			{
				target.enabled = false;
			}
		}
	}

	private float GetMappedProgress(float progress)
	{
		if (!useFirstCubeScaleConfig)
		{
			return progress;
		}
		if (parentBlock == null)
		{
			parentBlock = GetComponentInParent<Block>();
		}
		if (parentBlock == null)
		{
			return progress;
		}
		int maxCubes = parentBlock.GetMaxCubes();
		if (maxCubes <= 1)
		{
			return progress;
		}
		float p1 = 1f / (float)maxCubes;
		if (progress <= p1)
		{
			return (p1 > 0f) ? (progress / p1 * firstCubeScalePercent) : 0f;
		}
		float denominator = 1f - p1;
		return (denominator > 0f) ? (firstCubeScalePercent + (progress - p1) / denominator * (1f - firstCubeScalePercent)) : progress;
	}

	private void CancelProgressTween()
	{
		if (_progressTween != null)
		{
			_progressTween.Kill();
			_progressTween = null;
		}
	}

	private void SetSharedMaterial(Material material)
	{
		if (!meshRenderer)
		{
			return;
		}
		Material[] sharedMaterials = meshRenderer.sharedMaterials;
		if (sharedMaterials == null || sharedMaterials.Length == 0)
		{
			meshRenderer.sharedMaterial = material;
			return;
		}
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			sharedMaterials[i] = material;
		}
		meshRenderer.sharedMaterials = sharedMaterials;
	}

	private void ClearPropertyBlocks()
	{
	}
}
