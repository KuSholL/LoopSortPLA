using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Spawner : CarrierBase
{
	[SerializeField]
	private Block blockPrefab;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private Transform centerTrans;

	[SerializeField]
	private Transform remainingBlockCount;

	[SerializeField]
	private MeshRenderer remainingColorMesh;

	[SerializeField]
	private MeshRenderer remainingSlimeMesh;

	[SerializeField]
	private SpawnerRemainingSlimeAnimator remainingSlimeAnimator;

	[SerializeField]
	private SpawnerBlockAnimation spawnAnimator;

	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

	private static readonly int ColorId = Shader.PropertyToID("_Color");

	private readonly List<BlockRuntimeData> _blocksQueue = new List<BlockRuntimeData>();

	private Block _singleBlock;

	private CarrierBlockLayoutBase _blockLayout;

	private CarrierLinkedBlockVisualController _linkedBlockVisualController;

	private Coroutine _delayedVisualRoutine;

	private TextMesh _remainingBlockCountText;

	private int _currentQueueIndex;

	public Block SingleBlock => _singleBlock;

	public List<BlockRuntimeData> BlocksQueue => _blocksQueue;

	public int CurrentQueueIndex => _currentQueueIndex;

	public override CarrierBlockLayoutBase BlockLayout => _blockLayout;

	public override CarrierLinkedBlockVisualController LinkedBlockVisualController => _linkedBlockVisualController;

	public override int MaxBlockCount => 1;

	protected override void EnsureRuntime()
	{
		if (_runtimeState != null)
		{
			return;
		}
		if (_singleBlock == null)
		{
			_singleBlock = GetComponentInChildren<Block>(true);
			if (_singleBlock == null && blockPrefab != null)
			{
				_singleBlock = Object.Instantiate(blockPrefab, (container != null) ? container : base.transform);
				_singleBlock.transform.localPosition = Vector3.zero;
				_singleBlock.transform.localRotation = Quaternion.identity;
			}
		}
		_blockLayout = GetComponent<SingleBlockLayout>();
		if (_blockLayout == null)
		{
			_blockLayout = base.gameObject.AddComponent<SingleBlockLayout>();
		}
		_blockLayout.Blocks.Clear();
		if (_singleBlock != null)
		{
			_blockLayout.Blocks.Add(_singleBlock);
		}
		_runtimeState = new CarrierRuntimeState();
		CarrierBlockFactory factory = new CarrierBlockFactory(base.colorConfigSO);
		_blockController = new SpawnerBlockController(this, _blockLayout, factory, _runtimeState, 1);
		_unloadPort = new CarrierUnloadPort(this);
		_receivePort = new CarrierReceivePort(this);
		AlignRotation();
	}

	public override void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false)
	{
		EnsureRuntime();
		ResetRuntime();
		AlignRotation();
		_singleBlock.SetOwnerCarrier(this);
		if (carrierStack != null && carrierStack.Blocks != null)
		{
			for (int i = 0; i < carrierStack.Blocks.Count; i++)
			{
				BlockData data = carrierStack.Blocks[i];
				if (data != null && data.BlockColor != EBlockColorType.None)
				{
					BlockRuntimeData runtimeData = new BlockRuntimeData
					{
						HasContent = true,
						BlockColorType = data.BlockColor,
						CubeCount = ((MonoSingleton<CapacityManager>.Instance != null) ? MonoSingleton<CapacityManager>.Instance.CubePerBlock : 4),
						IsHiddenRevealed = true,
						Mechanics = Block.CloneMechanics(data.Mechanics)
					};
					ColorEntry entry = ((base.colorConfigSO != null) ? base.colorConfigSO.GetColorEntry(data.BlockColor) : null);
					runtimeData.Color = entry?.Color ?? Color.white;
					runtimeData.ShadowColor = entry?.ShadowColor ?? Color.white;
					_blocksQueue.Add(runtimeData);
				}
			}
		}
		UpdateSingleBlockVisual(false);
	}

	public override void FinishUnloadCarrier()
	{
		EnsureRuntime();
		int result = _blockController.CleanupEmptyBlocks();
		_runtimeState.FinishUnloading();
		if (result < 0)
		{
			UpdateSingleBlockVisual(false);
		}
	}

	public override bool CanPotentiallyReceive(EBlockColorType color)
	{
		return false;
	}

	public override bool CanReceiveByMechanic(EBlockColorType colorType)
	{
		return false;
	}

	public override bool CanUnloadByMechanic()
	{
		return true;
	}

	public override bool CanBeClicked()
	{
		EnsureRuntime();
		return base.RuntimeState.IsIdle && (spawnAnimator == null || !spawnAnimator.IsAnimating) && (MonoSingleton<ConveyorDeliverySystem>.Instance == null || !MonoSingleton<ConveyorDeliverySystem>.Instance.IsReceivingCube(this));
	}

	public override void OnObjectClicked()
	{
		if (_unloadPort.UnloadBlocks() && MonoSingleton<SoundManager>.Instance != null)
		{
			MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
		}
	}

	public override void OnClickBlocked()
	{
	}

	public override bool TryReserveReceive(EBlockColorType blockColorType, out CarrierReceiveReservation reservation, int undoBatchId = 0)
	{
		reservation = default(CarrierReceiveReservation);
		return false;
	}

	public override void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color)
	{
	}

	public override void EvaluateFinishState()
	{
	}

	public override int GetClawTargetBlockCount()
	{
		return 0;
	}

	public override bool CanBeClawTarget()
	{
		return false;
	}

	public override void RefreshMechanicVisualState()
	{
	}

	protected override void ResetRuntime()
	{
		EnsureRuntime();
		if (_delayedVisualRoutine != null)
		{
			StopCoroutine(_delayedVisualRoutine);
			_delayedVisualRoutine = null;
		}
		_runtimeState.Reset();
		_blockController.Reset();
		_blocksQueue.Clear();
		_currentQueueIndex = 0;
		if (_singleBlock != null)
		{
			_singleBlock.ClearContent();
			_singleBlock.SetPhysicsCollidersEnabled(true);
			_singleBlock.transform.localPosition = Vector3.zero;
			_singleBlock.transform.localScale = Vector3.one;
		}
		if (spawnAnimator != null)
		{
			spawnAnimator.Cancel();
		}
	}

	public void AdvanceQueue()
	{
		_currentQueueIndex++;
		UpdateSingleBlockVisual(true);
	}

	public void RegressQueue()
	{
		if (_currentQueueIndex > 0)
		{
			_currentQueueIndex--;
			UpdateSingleBlockVisual(false);
		}
	}

	private void UpdateSingleBlockVisual(bool playAnimation)
	{
		if (_singleBlock == null)
		{
			return;
		}
		bool hasCurrent = _currentQueueIndex < _blocksQueue.Count;
		if (hasCurrent)
		{
			BlockRuntimeData current = _blocksQueue[_currentQueueIndex];
			_singleBlock.ApplyRuntimeData(current, true);
			_singleBlock.gameObject.SetActive(true);
			_singleBlock.SetOwnerCarrier(this);
			int slimeLayer = LayerMask.NameToLayer("Slime1x");
			if (slimeLayer >= 0)
			{
				_singleBlock.SetLayer(slimeLayer);
			}
			SetRemainingBlockCount(_blocksQueue.Count - _currentQueueIndex - 1);
			if (playAnimation && spawnAnimator != null)
			{
				if (spawnAnimator != null)
				{
					spawnAnimator.Cancel();
				}
				_singleBlock.SetPhysicsCollidersEnabled(true);
				_singleBlock.transform.localScale = Vector3.one;
				_singleBlock.transform.localPosition = Vector3.zero;
				_singleBlock.SetVisualCubes(_singleBlock.GetCurrentCubes(), true);
			}
			else
			{
				if (spawnAnimator != null)
				{
					spawnAnimator.Cancel();
				}
				_singleBlock.SetPhysicsCollidersEnabled(true);
				_singleBlock.transform.localScale = Vector3.one;
				_singleBlock.transform.localPosition = Vector3.zero;
			}
		}
		else
		{
			if (spawnAnimator != null)
			{
				spawnAnimator.Cancel();
			}
			_singleBlock.SetPhysicsCollidersEnabled(true);
			_singleBlock.ClearContent();
			_singleBlock.gameObject.SetActive(false);
			SetRemainingBlockCount(0);
		}
		bool hasNext = hasCurrent && _currentQueueIndex + 1 < _blocksQueue.Count;
		BlockRuntimeData next = (hasNext ? _blocksQueue[_currentQueueIndex + 1] : null);
		float loadDuration = ((playAnimation && spawnAnimator != null && hasCurrent) ? spawnAnimator.GetTotalDuration(_blocksQueue[_currentQueueIndex].CubeCount) : 0f);
		UpdateRemainingVisual(hasNext, next, playAnimation, loadDuration);
	}

	private void UpdateRemainingVisual(bool hasNext, BlockRuntimeData next, bool animate, float delay)
	{
		if (_delayedVisualRoutine != null)
		{
			StopCoroutine(_delayedVisualRoutine);
			_delayedVisualRoutine = null;
		}
		if (animate && remainingSlimeAnimator != null)
		{
			remainingSlimeAnimator.PlayScaleDown(delay);
			_delayedVisualRoutine = StartCoroutine(ApplyRemainingVisualAfterDelay(hasNext, next, delay));
			return;
		}
		ApplyRemainingColors(hasNext, next);
		if (remainingSlimeAnimator != null)
		{
			remainingSlimeAnimator.SetScaleImmediate(hasNext);
		}
	}

	private IEnumerator ApplyRemainingVisualAfterDelay(bool hasNext, BlockRuntimeData next, float delay)
	{
		float elapsed = 0f;
		while (elapsed < delay)
		{
			float scale = ((MonoSingleton<CustomTimeScaleGroup>.Instance != null) ? MonoSingleton<CustomTimeScaleGroup>.Instance.CurrentTimeScale : 1f);
			elapsed += Time.unscaledDeltaTime * scale;
			yield return null;
		}
		ApplyRemainingColors(hasNext, next);
		if (hasNext && remainingSlimeAnimator != null)
		{
			remainingSlimeAnimator.PlayScaleUp();
		}
		_delayedVisualRoutine = null;
	}

	private void ApplyRemainingColors(bool hasNext, BlockRuntimeData next)
	{
		Color color = Color.white;
		ColorEntry cubeEntry = null;
		if (hasNext && next != null)
		{
			RemainingColorConfigSO remainingConfig = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetRemainingColorConfig() : null);
			color = ((remainingConfig != null) ? remainingConfig.GetColorEntry(next.BlockColorType) : null)?.Color ?? next.Color;
			ColorConfigSO cubeConfig = ((MonoSingleton<ConfigManager>.Instance != null) ? MonoSingleton<ConfigManager>.Instance.GetCubeColorConfig() : null);
			cubeEntry = ((cubeConfig != null) ? cubeConfig.GetColorEntry(next.BlockColorType) : null);
		}
		if (remainingColorMesh != null)
		{
			remainingColorMesh.ApplyColor(ColorId, color);
			remainingColorMesh.ApplyColor(BaseColorId, color);
			remainingColorMesh.gameObject.SetActive(true);
		}
		if (remainingSlimeMesh != null)
		{
			if (cubeEntry != null)
			{
				remainingSlimeMesh.ApplyColorEntry(cubeEntry);
			}
			else
			{
				remainingSlimeMesh.ApplyColor(ColorId, color);
			}
			remainingSlimeMesh.gameObject.SetActive(true);
		}
	}

	private void SetRemainingBlockCount(int count)
	{
		TextMesh text = GetRemainingBlockCountText();
		if (text != null)
		{
			text.text = count.ToString();
		}
	}

	private TextMesh GetRemainingBlockCountText()
	{
		if (_remainingBlockCountText != null)
		{
			return _remainingBlockCountText;
		}
		if (remainingBlockCount == null)
		{
			remainingBlockCount = FindChildByName(base.transform, "TowerCountText");
		}
		if (remainingBlockCount == null)
		{
			return null;
		}
		_remainingBlockCountText = remainingBlockCount.GetComponent<TextMesh>();
		if (_remainingBlockCountText == null)
		{
			_remainingBlockCountText = remainingBlockCount.gameObject.AddComponent<TextMesh>();
		}
		_remainingBlockCountText.fontSize = 64;
		_remainingBlockCountText.characterSize = 0.08f;
		_remainingBlockCountText.color = Color.white;
		MeshRenderer meshRenderer = remainingBlockCount.GetComponent<MeshRenderer>();
		if (meshRenderer != null && _remainingBlockCountText.font != null)
		{
			meshRenderer.sharedMaterial = _remainingBlockCountText.font.material;
		}
		return _remainingBlockCountText;
	}

	private static Transform FindChildByName(Transform root, string childName)
	{
		if (root == null)
		{
			return null;
		}
		if (root.name == childName)
		{
			return root;
		}
		for (int i = 0; i < root.childCount; i++)
		{
			Transform result = FindChildByName(root.GetChild(i), childName);
			if (result != null)
			{
				return result;
			}
		}
		return null;
	}

	private void Start()
	{
		AlignRotation();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (_delayedVisualRoutine != null)
		{
			StopCoroutine(_delayedVisualRoutine);
			_delayedVisualRoutine = null;
		}
	}

	private void AlignRotation()
	{
		if (centerTrans != null)
		{
			centerTrans.rotation = Quaternion.identity;
		}
	}
}
