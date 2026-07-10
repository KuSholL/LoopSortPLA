using System.Collections.Generic;
using UnityEngine;

public class Carrier : CarrierBase
{
	[Header("Block Layout")]
	[SerializeField]
	protected CarrierBlockLayout blockLayout;

	[Header("Render")]
	[SerializeField]
	protected Transform hiddenVisualRoot;

	[SerializeField]
	protected CarrierMechanicVisualConfigSO mechanicVisualConfig;

	[SerializeField]
	protected CarrierLinkedBlockVisualConfigSO linkedBlockVisualConfig;

	[SerializeField]
	protected MeshRenderer[] specialColorReceiverCarrierMeshRenderer;

	protected CarrierVisualController _visualController;

	protected CarrierLinkedBlockVisualController _linkedBlockVisualController;

	protected readonly CarrierMechanicContainer _mechanicContainer = new CarrierMechanicContainer();

	protected readonly CarrierActionGateResolver _actionGateResolver = new CarrierActionGateResolver();

	protected readonly CarrierVisualResolver _visualResolver = new CarrierVisualResolver();

	protected bool _isWaitingHiddenCarrierReveal = false;

	public override CarrierBlockLayoutBase BlockLayout => blockLayout;

	public override CarrierLinkedBlockVisualController LinkedBlockVisualController => _linkedBlockVisualController;

	public override int MaxBlockCount => GetConfiguredBlockCount();

	public override CarrierMechanicContainer MechanicContainer => _mechanicContainer;

	protected override void OnEnable()
	{
		base.OnEnable();
		CarrierMechanicEventHub.OnEvent += HandleMechanicEvent;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		CarrierMechanicEventHub.OnEvent -= HandleMechanicEvent;
		if (_linkedBlockVisualController != null)
		{
			_linkedBlockVisualController.Reset();
		}
		_mechanicContainer.Reset(this);
		if (_visualController != null)
		{
			_visualController.Reset();
		}
		_isWaitingHiddenCarrierReveal = false;
		SetBlockLayoutRootVisible(true);
	}

	public override bool CanBeClicked()
	{
		EnsureRuntime();
		if (_isWaitingHiddenCarrierReveal)
		{
			return false;
		}
		if (!base.RuntimeState.IsIdle)
		{
			return false;
		}
		if (!_actionGateResolver.EvaluateInteract(this).IsAllowed)
		{
			PlayBlockedByFullConveyorFeedback();
			return false;
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.IsReceivingCube(this))
		{
			return false;
		}
		return true;
	}

	public override void OnObjectClicked()
	{
		if (!_unloadPort.UnloadBlocks())
		{
			PlayBlockedByFullConveyorFeedback();
		}
		else
		{
			MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
		}
	}

	public override void OnClickBlocked()
	{
		if (!TryPlayFinishedBlock4XActiveAnimation())
		{
		}
	}

	public override void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false)
	{
		EnsureRuntime();
		ResetRuntime();
		_mechanicContainer.Rebuild(carrierStack?.Mechanics);
		_mechanicContainer.Reset(this);
		_blockController.BuildBlocks(carrierStack?.Blocks, suppressProgressAnimation);
		RefreshMechanicVisualState();
		if (_linkedBlockVisualController != null)
		{
			_linkedBlockVisualController.Refresh(suppressProgressAnimation);
		}
	}

	public override void SetSplineProgress(float progress)
	{
		base.SplineProgress = Mathf.Repeat(progress, 1f);
	}

	public override void FinishUnloadCarrier()
	{
		EnsureRuntime();
		int revealedHiddenBlockIndex = _blockController.CleanupEmptyBlocks();
		_runtimeState.FinishUnloading();
		if (_linkedBlockVisualController != null)
		{
			_linkedBlockVisualController.RefreshAfterUnload(revealedHiddenBlockIndex);
		}
	}

	public override bool TryReserveReceive(EBlockColorType blockColorType, out CarrierReceiveReservation reservation, int undoBatchId = 0)
	{
		EnsureRuntime();
		return _receivePort.TryReserveReceive(blockColorType, out reservation, undoBatchId);
	}

	public override bool CanPotentiallyReceive(EBlockColorType color)
	{
		EnsureRuntime();
		return _receivePort.CanPotentiallyReceive(color);
	}

	public override void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color)
	{
		EnsureRuntime();
		_receivePort.CompleteReceive(reservation, color);
		if (!(reservation.TargetBlock != null) || !reservation.TargetBlock.IsFull())
		{
			return;
		}
		int targetBlockIndex = ((_blockController != null) ? _blockController.GetBlockIndex(reservation.TargetBlock) : (-1));
		if (targetBlockIndex >= 0 && _linkedBlockVisualController != null && _linkedBlockVisualController.CanBlockMergeWithNeighbors(targetBlockIndex))
		{
			reservation.TargetBlock.PlayMergeVfx();
			if (MonoSingleton<SoundManager>.Instance != null)
			{
				MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_merge);
			}
		}
		if (_linkedBlockVisualController != null)
		{
			_linkedBlockVisualController.RefreshAfterReceive(targetBlockIndex);
		}
	}

	public override void EvaluateFinishState()
	{
		EnsureRuntime();
		_receivePort.EvaluateFinishCondition();
	}

	protected override void ResetRuntime()
	{
		EnsureRuntime();
		_runtimeState.Reset();
		_mechanicContainer.Reset(this);
		_linkedBlockVisualController.Reset();
		_visualController.Reset();
		_isWaitingHiddenCarrierReveal = false;
		SetBlockLayoutRootVisible(true);
	}

	public override void RefreshMechanicVisualState()
	{
		CarrierVisualRequest visualRequest = _visualResolver.Resolve(this);
		UpdateBlockLayoutVisibility(visualRequest);
		if (_visualController != null)
		{
			_visualController.ApplyVisualRequest(visualRequest);
		}
		if (_visualController != null)
		{
			_visualController.SetSpecialColorTint(GetSpecialReceiverTintColor());
		}
	}

	public override bool CanUnloadByMechanic()
	{
		if (_isWaitingHiddenCarrierReveal)
		{
			return false;
		}
		return _actionGateResolver.EvaluateUnload(this).IsAllowed;
	}

	public override bool CanReceiveByMechanic(EBlockColorType colorType)
	{
		if (_isWaitingHiddenCarrierReveal)
		{
			return false;
		}
		return _actionGateResolver.EvaluateReceive(this, colorType).IsAllowed;
	}

	public override int GetClawTargetBlockCount()
	{
		if (blockLayout == null)
		{
			return 0;
		}
		int targetBlockCount = 0;
		bool foundTargetRange = false;
		for (int i = 0; i < maxBlockCount; i++)
		{
			Block block = blockLayout.GetBlockByIndex(i);
			if (!foundTargetRange)
			{
				if (!block || !block.IsEmptyAndStable())
				{
					continue;
				}
				foundTargetRange = true;
			}
			else if (!block || !block.IsEmptyAndStable())
			{
				break;
			}
			targetBlockCount++;
		}
		return targetBlockCount;
	}

	public override bool CanBeClawTarget()
	{
		EnsureRuntime();
		if (IsLockedByContainer())
		{
			return false;
		}
		if (IsHiddenByColor())
		{
			return false;
		}
		if (!base.RuntimeState.IsIdle && !base.RuntimeState.IsCompleted)
		{
			return false;
		}
		if (MonoSingleton<ConveyorDeliverySystem>.Instance != null && MonoSingleton<ConveyorDeliverySystem>.Instance.IsReceivingCube(this))
		{
			return false;
		}
		if (HasIncompleteContentBlock())
		{
			return false;
		}
		return GetClawTargetBlockCount() > 0;
	}

	private bool IsHiddenByColor()
	{
		foreach (ICarrierMechanicRuntime mechanic in _mechanicContainer.Mechanics)
		{
			if (mechanic is HiddenCarrierByColorMechanicRuntime hidden && !hidden.IsUnlocked)
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsLockedByContainer()
	{
		CarrierContainerMember member = GetComponent<CarrierContainerMember>();
		return member != null && member.IsLocked;
	}

	public override List<LinkedBlockVisual> GetLinkedBlockVisuals(int size)
	{
		return (_linkedBlockVisualController != null) ? _linkedBlockVisualController.GetVisuals(size) : new List<LinkedBlockVisual>();
	}

	public bool TryPlayFinishedBlock4XActiveAnimation()
	{
		EnsureRuntime();
		return _runtimeState != null && _runtimeState.IsFinished && _linkedBlockVisualController != null && _linkedBlockVisualController.TryPlayFinishedBlock4XActiveAnimation();
	}

	public override bool TryGetSpecialReceiverTargetColor(out EBlockColorType colorType)
	{
		foreach (ICarrierMechanicRuntime mechanic in _mechanicContainer.Mechanics)
		{
			if (!(mechanic is ISpecialColorReceiverMechanic specialReceiver))
			{
				continue;
			}
			colorType = specialReceiver.TargetColor;
			return colorType != EBlockColorType.None;
		}
		colorType = EBlockColorType.None;
		return false;
	}

	public override bool IsSpecialReceiverForColor(EBlockColorType colorType)
	{
		EBlockColorType targetColor;
		return TryGetSpecialReceiverTargetColor(out targetColor) && targetColor == colorType;
	}

	private bool HasIncompleteContentBlock()
	{
		if (blockLayout == null || blockLayout.Blocks == null)
		{
			return false;
		}
		foreach (Block block in blockLayout.Blocks)
		{
			if (block == null || !block.HasContent || block.IsFull())
			{
				continue;
			}
			return true;
		}
		return false;
	}

	private void PlayBlockedByFullConveyorFeedback()
	{
		Block topBlock = ((_blockController != null) ? _blockController.GetTopUnloadCandidateBlock() : null);
		if (!(topBlock == null) && topBlock.IsFull() && (_linkedBlockVisualController == null || !_linkedBlockVisualController.TryPlayBlockedFullAnimation(topBlock)) && !topBlock.IsLinkedVisualSuppressed())
		{
			topBlock.PlayFullRevealAnimation();
		}
	}

	protected override void EnsureRuntime()
	{
		if (_runtimeState == null)
		{
			maxBlockCount = GetConfiguredBlockCount();
			_runtimeState = new CarrierRuntimeState();
			CarrierBlockFactory blockFactory = new CarrierBlockFactory(base.colorConfigSO);
			_blockController = new CarrierBlockController(this, blockLayout, blockFactory, _runtimeState, maxBlockCount);
			_visualController = new CarrierVisualController(base.transform, hiddenVisualRoot, mechanicVisualConfig, specialColorReceiverCarrierMeshRenderer, RevealBlockLayoutForHiddenCarrier, CompleteHiddenCarrierReveal);
			_linkedBlockVisualController = new CarrierLinkedBlockVisualController(this, linkedBlockVisualConfig);
			_unloadPort = new CarrierUnloadPort(this);
			_receivePort = new CarrierReceivePort(this);
		}
	}

	protected virtual void UpdateBlockLayoutVisibility(CarrierVisualRequest visualRequest)
	{
		if (visualRequest != null && visualRequest.Kind == ECarrierVisualKind.HiddenShell)
		{
			_isWaitingHiddenCarrierReveal = true;
			SetBlockLayoutRootVisible(false);
		}
		else if (!_isWaitingHiddenCarrierReveal)
		{
			SetBlockLayoutRootVisible(true);
		}
	}

	protected virtual void RevealBlockLayoutForHiddenCarrier()
	{
		SetBlockLayoutRootVisible(true);
	}

	protected virtual void CompleteHiddenCarrierReveal()
	{
		SetBlockLayoutRootVisible(true);
		_isWaitingHiddenCarrierReveal = false;
	}

	protected virtual void SetBlockLayoutRootVisible(bool isVisible)
	{
		Transform root = ((blockLayout != null) ? blockLayout.Root : null);
		if (!(root == null) && root.gameObject.activeSelf != isVisible)
		{
			List<Block> blocks = blockLayout.Blocks;
			if (!isVisible)
			{
				SetBlocksPreserveRuntimeState(blocks, true);
				root.gameObject.SetActive(false);
			}
			else
			{
				root.gameObject.SetActive(true);
				SetBlocksPreserveRuntimeState(blocks, false);
			}
		}
	}

	private static void SetBlocksPreserveRuntimeState(IReadOnlyList<Block> blocks, bool preserve)
	{
		if (blocks == null)
		{
			return;
		}
		for (int i = 0; i < blocks.Count; i++)
		{
			if (blocks[i] != null)
			{
				blocks[i].PreserveRuntimeStateWhileHidden(preserve);
			}
		}
	}

	private void HandleMechanicEvent(ICarrierMechanicEvent carrierEvent)
	{
		EnsureRuntime();
		_mechanicContainer.DispatchEvent(this, carrierEvent);
		RefreshMechanicVisualState();
	}

	private Color? GetSpecialReceiverTintColor()
	{
		if (!TryGetSpecialReceiverTargetColor(out var colorType))
		{
			return null;
		}
		return ((base.colorConfigSO != null) ? base.colorConfigSO.GetColorEntry(colorType) : null)?.Color;
	}

	public override void SetLayer(int layer)
	{
		base.gameObject.layer = layer;
		if (hiddenVisualRoot != null)
		{
			ApplyLayer(hiddenVisualRoot.gameObject, layer);
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
