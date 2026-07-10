using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Spawner : CarrierBase
{
    [SerializeField] private Block blockPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private Transform centerTrans;
    [SerializeField] private Transform remainingBlockCount;
    [SerializeField] private MeshRenderer remainingColorMesh;
    [SerializeField] private MeshRenderer remainingSlimeMesh;
    [SerializeField] private SpawnerRemainingSlimeAnimator remainingSlimeAnimator;
    [SerializeField] private SpawnerBlockAnimation spawnAnimator;

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
        if (_runtimeState != null) return;
        if (_singleBlock == null)
        {
            _singleBlock = GetComponentInChildren<Block>(true);
            if (_singleBlock == null && blockPrefab != null)
            {
                _singleBlock = Instantiate(blockPrefab, container != null ? container : transform);
                _singleBlock.transform.localPosition = Vector3.zero;
                _singleBlock.transform.localRotation = Quaternion.identity;
            }
        }

        _blockLayout = GetComponent<SingleBlockLayout>();
        if (_blockLayout == null) _blockLayout = gameObject.AddComponent<SingleBlockLayout>();
        _blockLayout.Blocks.Clear();
        if (_singleBlock != null) _blockLayout.Blocks.Add(_singleBlock);

        _runtimeState = new CarrierRuntimeState();
        var factory = new CarrierBlockFactory(colorConfigSO);
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
            for (var i = 0; i < carrierStack.Blocks.Count; i++)
            {
                var data = carrierStack.Blocks[i];
                if (data == null || data.BlockColor == EBlockColorType.None) continue;
                var runtimeData = new BlockRuntimeData
                {
                    HasContent = true,
                    BlockColorType = data.BlockColor,
                    CubeCount = CapacityManager.Instance != null
                        ? CapacityManager.Instance.CubePerBlock
                        : 4,
                    IsHiddenRevealed = true,
                    Mechanics = Block.CloneMechanics(data.Mechanics)
                };
                var entry = colorConfigSO != null
                    ? colorConfigSO.GetColorEntry(data.BlockColor)
                    : null;
                runtimeData.Color = entry != null ? entry.Color : Color.white;
                runtimeData.ShadowColor = entry != null ? entry.ShadowColor : Color.white;
                _blocksQueue.Add(runtimeData);
            }
        }
        UpdateSingleBlockVisual(false);
    }

    public override void FinishUnloadCarrier()
    {
        EnsureRuntime();
        var result = _blockController.CleanupEmptyBlocks();
        _runtimeState.FinishUnloading();
        if (result < 0) UpdateSingleBlockVisual(false);
    }

    public override bool CanPotentiallyReceive(EBlockColorType color) => false;
    public override bool CanReceiveByMechanic(EBlockColorType colorType) => false;
    public override bool CanUnloadByMechanic() => true;

    public override bool CanBeClicked()
    {
        EnsureRuntime();
        return RuntimeState.IsIdle
               && (spawnAnimator == null || !spawnAnimator.IsAnimating)
               && (ConveyorDeliverySystem.Instance == null
                   || !ConveyorDeliverySystem.Instance.IsReceivingCube(this));
    }

    public override void OnObjectClicked()
    {
        if (!_unloadPort.UnloadBlocks()) return;
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
    }

    public override void OnClickBlocked()
    {
    }

    public override bool TryReserveReceive(
        EBlockColorType blockColorType,
        out CarrierReceiveReservation reservation,
        int undoBatchId = 0)
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

    public override int GetClawTargetBlockCount() => 0;
    public override bool CanBeClawTarget() => false;
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
        if (spawnAnimator != null) spawnAnimator.Cancel();
    }

    public void AdvanceQueue()
    {
        _currentQueueIndex++;
        UpdateSingleBlockVisual(true);
    }

    public void RegressQueue()
    {
        if (_currentQueueIndex <= 0) return;
        _currentQueueIndex--;
        UpdateSingleBlockVisual(false);
    }

    private void UpdateSingleBlockVisual(bool playAnimation)
    {
        if (_singleBlock == null) return;
        var hasCurrent = _currentQueueIndex < _blocksQueue.Count;
        if (hasCurrent)
        {
            var current = _blocksQueue[_currentQueueIndex];
            _singleBlock.ApplyRuntimeData(current, true);
            _singleBlock.gameObject.SetActive(true);
            _singleBlock.SetOwnerCarrier(this);
            var slimeLayer = LayerMask.NameToLayer("Slime1x");
            if (slimeLayer >= 0) _singleBlock.SetLayer(slimeLayer);

            SetRemainingBlockCount(_blocksQueue.Count - _currentQueueIndex - 1);

            if (playAnimation && spawnAnimator != null)
            {
#if UNITY_LUNA
                if (spawnAnimator != null) spawnAnimator.Cancel();
                _singleBlock.SetPhysicsCollidersEnabled(true);
                _singleBlock.transform.localScale = Vector3.one;
                _singleBlock.transform.localPosition = Vector3.zero;
                _singleBlock.SetVisualCubes(_singleBlock.GetCurrentCubes(), true);
#else
                _singleBlock.SetPhysicsCollidersEnabled(false);
                _singleBlock.transform.localScale = Vector3.zero;
                _singleBlock.SetVisualCubes(0, true);
                spawnAnimator.Play(_singleBlock);
#endif
            }
            else
            {
                if (spawnAnimator != null) spawnAnimator.Cancel();
                _singleBlock.SetPhysicsCollidersEnabled(true);
                _singleBlock.transform.localScale = Vector3.one;
                _singleBlock.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            if (spawnAnimator != null) spawnAnimator.Cancel();
            _singleBlock.SetPhysicsCollidersEnabled(true);
            _singleBlock.ClearContent();
            _singleBlock.gameObject.SetActive(false);
            SetRemainingBlockCount(0);
        }

        var hasNext = hasCurrent && _currentQueueIndex + 1 < _blocksQueue.Count;
        var next = hasNext ? _blocksQueue[_currentQueueIndex + 1] : null;
        var loadDuration = playAnimation && spawnAnimator != null && hasCurrent
            ? spawnAnimator.GetTotalDuration(_blocksQueue[_currentQueueIndex].CubeCount)
            : 0f;
        UpdateRemainingVisual(hasNext, next, playAnimation, loadDuration);
    }

    private void UpdateRemainingVisual(
        bool hasNext,
        BlockRuntimeData next,
        bool animate,
        float delay)
    {
        if (_delayedVisualRoutine != null)
        {
            StopCoroutine(_delayedVisualRoutine);
            _delayedVisualRoutine = null;
        }

        if (animate && remainingSlimeAnimator != null)
        {
            remainingSlimeAnimator.PlayScaleDown(delay);
            _delayedVisualRoutine = StartCoroutine(
                ApplyRemainingVisualAfterDelay(hasNext, next, delay));
            return;
        }

        ApplyRemainingColors(hasNext, next);
        if (remainingSlimeAnimator != null)
            remainingSlimeAnimator.SetScaleImmediate(hasNext);
    }

    private IEnumerator ApplyRemainingVisualAfterDelay(
        bool hasNext,
        BlockRuntimeData next,
        float delay)
    {
        var elapsed = 0f;
        while (elapsed < delay)
        {
            var scale = CustomTimeScaleGroup.Instance != null
                ? CustomTimeScaleGroup.Instance.CurrentTimeScale
                : 1f;
            elapsed += Time.unscaledDeltaTime * scale;
            yield return null;
        }
        ApplyRemainingColors(hasNext, next);
        if (hasNext && remainingSlimeAnimator != null)
            remainingSlimeAnimator.PlayScaleUp();
        _delayedVisualRoutine = null;
    }

    private void ApplyRemainingColors(bool hasNext, BlockRuntimeData next)
    {
        var color = Color.white;
        ColorEntry cubeEntry = null;
        if (hasNext && next != null)
        {
            var remainingConfig = ConfigManager.Instance != null
                ? ConfigManager.Instance.GetRemainingColorConfig()
                : null;
            var remainingEntry = remainingConfig != null
                ? remainingConfig.GetColorEntry(next.BlockColorType)
                : null;
            color = remainingEntry != null ? remainingEntry.Color : next.Color;
            var cubeConfig = ConfigManager.Instance != null
                ? ConfigManager.Instance.GetCubeColorConfig()
                : null;
            cubeEntry = cubeConfig != null
                ? cubeConfig.GetColorEntry(next.BlockColorType)
                : null;
        }

        if (remainingColorMesh != null)
        {
            remainingColorMesh.ApplyColor(ColorId, color);
            remainingColorMesh.ApplyColor(BaseColorId, color);
            remainingColorMesh.gameObject.SetActive(true);
        }
        if (remainingSlimeMesh != null)
        {
            if (cubeEntry != null) remainingSlimeMesh.ApplyColorEntry(cubeEntry);
            else remainingSlimeMesh.ApplyColor(ColorId, color);
            remainingSlimeMesh.gameObject.SetActive(true);
        }
    }

    private void SetRemainingBlockCount(int count)
    {
        var text = GetRemainingBlockCountText();
        if (text != null) text.text = count.ToString();
    }

    private TextMesh GetRemainingBlockCountText()
    {
        if (_remainingBlockCountText != null) return _remainingBlockCountText;

        if (remainingBlockCount == null)
            remainingBlockCount = FindChildByName(transform, "TowerCountText");

        if (remainingBlockCount == null) return null;

        _remainingBlockCountText = remainingBlockCount.GetComponent<TextMesh>();
        if (_remainingBlockCountText == null)
            _remainingBlockCountText = remainingBlockCount.gameObject.AddComponent<TextMesh>();

        _remainingBlockCountText.fontSize = 64;
        _remainingBlockCountText.characterSize = 0.08f;
        _remainingBlockCountText.color = Color.white;

        var meshRenderer = remainingBlockCount.GetComponent<MeshRenderer>();
        if (meshRenderer != null && _remainingBlockCountText.font != null)
            meshRenderer.sharedMaterial = _remainingBlockCountText.font.material;

        return _remainingBlockCountText;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindChildByName(root.GetChild(i), childName);
            if (result != null) return result;
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
        if (centerTrans != null) centerTrans.rotation = Quaternion.identity;
    }
}

public sealed class SpawnerBlockController : CarrierBlockController
{
    private readonly Spawner _spawner;
    private bool _hasCollectedVisibleBlock;

    public SpawnerBlockController(
        Spawner spawner,
        CarrierBlockLayoutBase layout,
        CarrierBlockFactory factory,
        CarrierRuntimeState runtimeState,
        int maxBlockCount)
        : base(spawner, layout, factory, runtimeState, maxBlockCount)
    {
        _spawner = spawner;
    }

    public override void Reset()
    {
        base.Reset();
        _hasCollectedVisibleBlock = false;
    }

    public override Block GetCurrentBlock()
    {
        if (_hasCollectedVisibleBlock || _spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count)
            return null;
        return _spawner.SingleBlock;
    }

    public override bool TryGetCurrentMatchingBlock(EBlockColorType colorType, out Block block)
    {
        block = GetCurrentBlock();
        return block != null && block.GetBlockColorType() == colorType;
    }

    public override void CompleteCurrentBlock()
    {
        _hasCollectedVisibleBlock = true;
    }

    public override Block GetTopUnloadCandidateBlock()
    {
        var block = GetCurrentBlock();
        return block != null && block.CanBeginUnload() ? block : null;
    }

    public override List<Block> GetPotentialUnloadBlocks(EBlockColorType colorType)
    {
        var result = new List<Block>();
        var block = GetCurrentBlock();
        if (block != null && block.GetBlockColorType() == colorType && block.CanBeginUnload())
            result.Add(block);
        return result;
    }

    public override Block GetReceiveBlock(EBlockColorType blockColorType) => null;
    public override int GetBlockIndex(Block targetBlock) => targetBlock == _spawner.SingleBlock ? 0 : -1;

    public override int CleanupEmptyBlocks()
    {
        _hasCollectedVisibleBlock = false;
        var block = _spawner.SingleBlock;
        if (block == null || !block.HasContent || block.GetCurrentCubes() != 0) return -1;
        block.ClearContent();
        _spawner.AdvanceQueue();
        if (_spawner.CurrentQueueIndex < _spawner.BlocksQueue.Count) return 0;
        _spawner.RuntimeState.MarkCompleted();
        return 1;
    }

    public override int RevealHiddenBlockAfterRelease(int releasedBlockIndex) => -1;

    public override void RefreshVisibleBlockVisuals()
    {
        _hasCollectedVisibleBlock = false;
        if (_spawner.SingleBlock != null && !_spawner.SingleBlock.HasContent)
        {
            _spawner.AdvanceQueue();
            if (_spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count)
                _spawner.RuntimeState.MarkCompleted();
        }
    }
}
