using System.Collections;
using UnityEngine;

public sealed class BlockLinkVisual : MonoBehaviour
{
	[SerializeField]
	private float linkWidth = 0.15f;

	[SerializeField]
	private float lengthScale = 1f;

	[SerializeField]
	private float lengthOffset;

	[SerializeField]
	private ParticleSystem vfxSplashDecayPrefab;

	[SerializeField]
	private float vfxSpacing = 1.5f;

	private static readonly int LeftColor = Shader.PropertyToID("_LeftColor");

	private static readonly int RightColor = Shader.PropertyToID("_RightColor");

	private static readonly int LeftShadowColor = Shader.PropertyToID("_LeftShadowColor");

	private static readonly int RightShadowColor = Shader.PropertyToID("_RightShadowColor");

	private static readonly int LeftOutlineColor = Shader.PropertyToID("_LeftOutlineColor");

	private static readonly int RightOutlineColor = Shader.PropertyToID("_RightOutlineColor");

	private CarrierBase _carrierA;

	private CarrierBase _carrierB;

	private Block _blockA;

	private Block _blockB;

	public void Setup(CarrierBase carrierA, Block blockA, CarrierBase carrierB, Block blockB)
	{
		_carrierA = carrierA;
		_carrierB = carrierB;
		_blockA = blockA;
		_blockB = blockB;
		ApplyColors();
		UpdatePositions();
	}

	public void UpdatePositions()
	{
		if (!IsValid())
		{
			HideWithUnloadVfx();
			return;
		}
		if (_blockA.IsOpened || _blockB.IsOpened)
		{
			HideWithUnloadVfx();
			return;
		}
		GetClosestAnchors(out var positionA, out var positionB);
		Vector3 direction = positionB - positionA;
		float distance = direction.magnitude;
		if (distance <= 0.001f)
		{
			base.gameObject.SetActive(false);
			return;
		}
		base.gameObject.SetActive(true);
		base.transform.position = (positionA + positionB) * 0.5f;
		base.transform.right = direction / distance;
		base.transform.localScale = new Vector3(Mathf.Max(0f, distance * lengthScale + lengthOffset), linkWidth, linkWidth);
	}

	private void HideWithUnloadVfx()
	{
		if (base.gameObject.activeSelf)
		{
			PlayUnloadVFX();
			base.gameObject.SetActive(false);
		}
	}

	public void PlayUnloadVFX()
	{
		if (vfxSplashDecayPrefab == null || MonoSingleton<PoolManagerNew>.Instance == null)
		{
			return;
		}
		GetClosestAnchors(out var positionA, out var positionB);
		float distance = Vector3.Distance(positionA, positionB);
		int count = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.1f, vfxSpacing)));
		for (int i = 0; i <= count; i++)
		{
			ParticleSystem particle = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(vfxSplashDecayPrefab);
			if (!(particle == null))
			{
				particle.transform.position = Vector3.Lerp(positionA, positionB, (float)i / (float)count);
				particle.Play(true);
				StartCoroutine(ReturnParticle(particle));
			}
		}
	}

	private IEnumerator ReturnParticle(ParticleSystem particle)
	{
		ParticleSystem.MainModule main = particle.main;
		yield return new WaitForSeconds(main.duration + main.startLifetime.constantMax);
		if (particle != null && MonoSingleton<PoolManagerNew>.Instance != null)
		{
			MonoSingleton<PoolManagerNew>.Instance.PushToPool(particle);
		}
	}

	private bool IsValid()
	{
		return _carrierA != null && _carrierB != null && _blockA != null && _blockB != null && _blockA.HasContent && _blockB.HasContent && _blockA.HasLinkGroupId() && _blockB.HasLinkGroupId();
	}

	private void ApplyColors()
	{
		Renderer renderer = GetComponentInChildren<Renderer>();
		if (!(renderer == null))
		{
			ColorEntry entryA = GetColorEntry(_carrierA, _blockA);
			ColorEntry entryB = GetColorEntry(_carrierB, _blockB);
			if (entryA != null)
			{
				renderer.ApplyColor(RightColor, entryA.Color);
				renderer.ApplyColor(RightShadowColor, entryA.ShadowColor);
				renderer.ApplyColor(RightOutlineColor, entryA.OutlineColor);
			}
			if (entryB != null)
			{
				renderer.ApplyColor(LeftColor, entryB.Color);
				renderer.ApplyColor(LeftShadowColor, entryB.ShadowColor);
				renderer.ApplyColor(LeftOutlineColor, entryB.OutlineColor);
			}
		}
	}

	private static ColorEntry GetColorEntry(CarrierBase carrier, Block block)
	{
		if (block == null || block.GetBlockColorType() == EBlockColorType.None)
		{
			return null;
		}
		ColorConfigSO config = ((carrier != null) ? carrier.ColorConfig : null);
		return (config != null) ? config.GetColorEntry(block.GetBlockColorType()) : PlayableColorFallback.CreateColorEntry(block.GetBlockColorType());
	}

	private void GetClosestAnchors(out Vector3 positionA, out Vector3 positionB)
	{
		Vector3 aLeft = GetAnchor(_carrierA, _blockA, true);
		Vector3 aRight = GetAnchor(_carrierA, _blockA, false);
		Vector3 bLeft = GetAnchor(_carrierB, _blockB, true);
		Vector3 bRight = GetAnchor(_carrierB, _blockB, false);
		positionA = aLeft;
		positionB = bLeft;
		float bestDistance = Vector3.SqrMagnitude(aLeft - bLeft);
		TrySelect(aLeft, bRight, ref bestDistance, ref positionA, ref positionB);
		TrySelect(aRight, bLeft, ref bestDistance, ref positionA, ref positionB);
		TrySelect(aRight, bRight, ref bestDistance, ref positionA, ref positionB);
	}

	private static void TrySelect(Vector3 first, Vector3 second, ref float bestDistance, ref Vector3 selectedFirst, ref Vector3 selectedSecond)
	{
		float distance = Vector3.SqrMagnitude(first - second);
		if (!(distance >= bestDistance))
		{
			bestDistance = distance;
			selectedFirst = first;
			selectedSecond = second;
		}
	}

	private static Vector3 GetAnchor(CarrierBase carrier, Block block, bool left)
	{
		if (carrier != null && carrier.LinkedBlockVisualController != null)
		{
			LinkedBlockVisual visual = carrier.LinkedBlockVisualController.GetLinkedVisualContainingBlock(block);
			if (visual != null)
			{
				Transform anchor = (left ? visual.LeftLinkAnchor : visual.RightLinkAnchor);
				if (anchor != null)
				{
					return anchor.position;
				}
			}
		}
		Transform blockAnchor = (left ? block.LeftLinkAnchor : block.RightLinkAnchor);
		if (blockAnchor != null)
		{
			return blockAnchor.position;
		}
		return (block.AnimationPivot != null) ? block.AnimationPivot.position : block.transform.position;
	}
}
