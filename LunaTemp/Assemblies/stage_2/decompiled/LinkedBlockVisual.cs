using UnityEngine;

public sealed class LinkedBlockVisual : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer meshRenderer;

	[SerializeField]
	private SkinnedMeshRenderer skinnedMeshRenderer;

	[SerializeField]
	private GameObject modelGO;

	[SerializeField]
	private EBlockShapeType shapeType;

	[SerializeField]
	private SpriteRenderer catFace;

	[SerializeField]
	private BlockSolidProgressAnimator progressAnimator;

	[SerializeField]
	private Renderer[] keyRenderers;

	[Header("Link Anchors")]
	[SerializeField]
	private Transform leftLinkAnchor;

	[SerializeField]
	private Transform rightLinkAnchor;

	private CarrierBase _carrier;

	private Block _anchorBlock;

	private MaterialPropertyBlock _materialBlock;

	private Collider[] _visualColliders;

	public Transform LeftLinkAnchor => leftLinkAnchor;

	public Transform RightLinkAnchor => rightLinkAnchor;

	private bool IsBlock4X => shapeType == EBlockShapeType.Block4x;

	public Renderer KeyRenderer => (keyRenderers != null && keyRenderers.Length != 0) ? keyRenderers[0] : null;

	private void Awake()
	{
		DisableVisualColliders();
	}

	private void OnEnable()
	{
		DisableVisualColliders();
	}

	public void Apply(ColorEntry colorEntry, CatColorEntry catEntry, bool suppressProgressAnimation = false, bool forceFullAnimation = false, bool hasKey = false, EBlockColorType keyColorType = EBlockColorType.None)
	{
		SetupMaterial(colorEntry);
		SetProgress(1f, suppressProgressAnimation, forceFullAnimation);
		SetVisible(true);
		if ((bool)catFace)
		{
			catFace.color = catEntry.Color;
		}
		SetKeyVisible(hasKey);
		if (hasKey && keyColorType != EBlockColorType.None)
		{
			SetKeyTexture(keyColorType);
		}
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

	private void SetKeyTexture(EBlockColorType colorType)
	{
		if (keyRenderers == null)
		{
			return;
		}
		StylizedColorConfigSO config = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetStylizedColorConfig() : null);
		if (config == null)
		{
			return;
		}
		StylizedColorEntry entry = config.GetColorEntry(colorType);
		MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
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
					r.GetPropertyBlock(propertyBlock, i);
					propertyBlock.SetColor(colorId, entry.Color);
					propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
					propertyBlock.SetColor(specularColorId, entry.SpecularColor);
					propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
					r.SetPropertyBlock(propertyBlock, i);
				}
			}
			else
			{
				r.GetPropertyBlock(propertyBlock, 0);
				propertyBlock.SetColor(colorId, entry.Color);
				propertyBlock.SetColor(shadowColorId, entry.ShadowColor);
				propertyBlock.SetColor(specularColorId, entry.SpecularColor);
				propertyBlock.SetColor(reflectColorId, entry.ReflectColor);
				r.SetPropertyBlock(propertyBlock, 0);
			}
		}
	}

	public Vector3 GetKeyVisualPosition()
	{
		return (KeyRenderer != null) ? KeyRenderer.transform.position : base.transform.position;
	}

	public void BindSelectionContext(CarrierBase carrier, Block anchorBlock)
	{
		_carrier = carrier;
		_anchorBlock = anchorBlock;
	}

	public bool MatchesSelection(CarrierBase carrier, Block anchorBlock)
	{
		return _carrier == carrier && _anchorBlock == anchorBlock;
	}

	public void SetVisible(bool isVisible)
	{
		DisableVisualColliders();
		if (!isVisible)
		{
			SetProgress(1f, true);
		}
		if (modelGO != null)
		{
			modelGO.SetActive(isVisible);
		}
		else
		{
			base.gameObject.SetActive(isVisible);
		}
		if (!isVisible)
		{
			BindSelectionContext(null, null);
		}
	}

	public void SetProgress(float progress, bool suppressAnimation = false, bool forceFullAnimation = false)
	{
		progress = Mathf.Clamp01(progress);
		Transform targetTransform = GetVisualTransform();
		if (targetTransform != null)
		{
			targetTransform.localScale = new Vector3((!(progress <= 0f)) ? 1 : 0, (!(progress <= 0f)) ? 1 : 0, progress);
		}
		progressAnimator?.SetProgress(progress, suppressAnimation, forceFullAnimation);
		DisableVisualColliders();
	}

	public void SetLayer(int layer)
	{
		ApplyLayer(base.gameObject, layer);
	}

	public void PlayBlockedFullAnimation()
	{
		progressAnimator?.ReplayCurrentProgressAnimation(true);
	}

	public void PlayTriggerActiveAnimation()
	{
		progressAnimator?.PlayTriggerActiveAnimation();
	}

	private void SetupMaterial(ColorEntry colorEntry)
	{
		Renderer targetRenderer = GetTargetRenderer();
		if (!(targetRenderer == null) && colorEntry != null)
		{
			if (_materialBlock == null)
			{
				_materialBlock = new MaterialPropertyBlock();
			}
			targetRenderer.GetPropertyBlock(_materialBlock);
			_materialBlock.SetColorEntry(colorEntry);
			targetRenderer.SetPropertyBlock(_materialBlock);
		}
	}

	private Renderer GetTargetRenderer()
	{
		if (IsBlock4X && skinnedMeshRenderer != null)
		{
			return skinnedMeshRenderer;
		}
		if (meshRenderer != null)
		{
			return meshRenderer;
		}
		return skinnedMeshRenderer;
	}

	private Transform GetVisualTransform()
	{
		Renderer targetRenderer = GetTargetRenderer();
		return (targetRenderer != null) ? targetRenderer.transform : null;
	}

	private void DisableVisualColliders()
	{
		if (_visualColliders == null || _visualColliders.Length == 0)
		{
			_visualColliders = GetComponentsInChildren<Collider>(true);
		}
		if (_visualColliders == null)
		{
			return;
		}
		for (int i = 0; i < _visualColliders.Length; i++)
		{
			Collider target = _visualColliders[i];
			if (target != null)
			{
				target.enabled = false;
			}
		}
	}

	private static void ApplyLayer(GameObject target, int layer)
	{
		if (!(target == null))
		{
			target.layer = layer;
			Transform transform = target.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				ApplyLayer(transform.GetChild(i).gameObject, layer);
			}
		}
	}
}
