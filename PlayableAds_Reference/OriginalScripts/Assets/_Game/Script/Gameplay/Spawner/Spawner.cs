using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Spawner : CarrierBase
{
    [SerializeField] private Block blockPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private Transform centerTrans;
    [SerializeField] private TextMeshPro remainingBlockCount;
    [SerializeField] private MeshRenderer remainingColorMesh;
    [SerializeField] private MeshRenderer remainingSlimeMesh;
    [SerializeField] private SpawnerRemainingSlimeAnimator remainingSlimeAnimator;
    [SerializeField] private SpawnerBlockAnimation spawnAnimator;

    private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropId = Shader.PropertyToID("_Color");

    private Block _singleBlock;
    private CarrierBlockLayoutBase _blockLayout;
    private CarrierLinkedBlockVisualController _linkedBlockVisualController;

    private readonly List<BlockRuntimeData> _blocksQueue = new();
    private int _currentQueueIndex;
    private CancellationTokenSource _slimeAnimCts;
    private MaterialPropertyBlock _remainingColorPropBlock;
    private MaterialPropertyBlock _remainingSlimePropBlock;

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
            _singleBlock = GetComponentInChildren<Block>();
            if (_singleBlock == null && blockPrefab != null)
            {
                _singleBlock = Instantiate(blockPrefab, container != null ? container : transform);
                _singleBlock.transform.localPosition = Vector3.zero;
                _singleBlock.transform.localRotation = Quaternion.identity;
                _singleBlock.transform.localScale = Vector3.one;
            }
        }

        if (_blockLayout == null)
        {
            _blockLayout = GetComponent<SingleBlockLayout>();
            if (_blockLayout == null)
            {
                _blockLayout = gameObject.AddComponent<SingleBlockLayout>();
            }
        }

        if (_blockLayout != null)
        {
            _blockLayout.Blocks.Clear();
            if (_singleBlock != null) _blockLayout.Blocks.Add(_singleBlock);
        }

        maxBlockCount = GetConfiguredBlockCount();
        _runtimeState = new CarrierRuntimeState();
        var blockFactory = new CarrierBlockFactory(colorConfigSO);
        _blockController = new SpawnerBlockController(this, _blockLayout, blockFactory, _runtimeState, 1);
        _unloadPort = new CarrierUnloadPort(this);
        _receivePort = new CarrierReceivePort(this);

        AlignRotation();
    }

    public override void CreateBlocks(CarrierStackData carrierStack, bool suppressProgressAnimation = false)
    {
        EnsureRuntime();
        ResetRuntime();
        AlignRotation();

        if (_singleBlock != null)
        {
            _singleBlock.SetOwnerCarrier(this);
        }

        _blocksQueue.Clear();
        _currentQueueIndex = 0;

        if (carrierStack != null && carrierStack.Blocks != null)
        {
            foreach (var blockData in carrierStack.Blocks)
            {
                if (blockData == null || blockData.BlockColor == EBlockColorType.None) continue;

                var runtimeData = new BlockRuntimeData
                {
                    HasContent = true,
                    BlockColorType = blockData.BlockColor,
                    CubeCount = CapacityManager.Instance != null ? CapacityManager.Instance.CubePerBlock : 4,
                    IsHiddenRevealed = true,
                    Mechanics = Block.CloneMechanics(blockData.Mechanics)
                };

                if (colorConfigSO != null)
                {
                    var entry = colorConfigSO.GetColorEntry(blockData.BlockColor);
                    runtimeData.Color = entry?.Color ?? Color.white;
                    runtimeData.ShadowColor = entry?.ShadowColor ?? Color.white;
                }

                _blocksQueue.Add(runtimeData);
            }
        }

        UpdateSingleBlockVisual();
    }

    public override void FinishUnloadCarrier()
    {
        EnsureRuntime();
        var result = _blockController.CleanupEmptyBlocks();
        _runtimeState.FinishUnloading();
        if (result < 0)
        {
            UpdateSingleBlockVisual(playAnimation: false);
        }
    }

    public override bool CanPotentiallyReceive(EBlockColorType color) => false;
    public override bool CanReceiveByMechanic(EBlockColorType colorType) => false;
    public override bool CanUnloadByMechanic() => true;

    public override bool CanBeClicked()
    {
        EnsureRuntime();
        if (!RuntimeState.IsIdle) return false;
        if (spawnAnimator != null && spawnAnimator.IsAnimating) return false;
        if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsReceivingCube(this)) return false;
        return true;
    }

    public override void OnObjectClicked()
    {
        if (BoosterSystem.Instance != null && BoosterSystem.Instance.UseClawBooster)
        {
            BoosterSystem.Instance.SelectCarrier(Pivot.position, this);
            return;
        }
        if (!_unloadPort.UnloadBlocks())
        {
            return;
        }
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_touch_box);
    }

    public override void OnClickBlocked() {}

    public override bool TryReserveReceive(EBlockColorType blockColorType, out CarrierReceiveReservation reservation, int undoBatchId = 0)
    {
        reservation = default;
        return false;
    }

    public override void CompleteReceiveCube(CarrierReceiveReservation reservation, Color color) {}

    public override void EvaluateFinishState() {}

    public override int GetClawTargetBlockCount() => 0;

    public override bool CanBeClawTarget() => false;

    public override void RefreshMechanicVisualState() {}

    protected override void ResetRuntime()
    {
        EnsureRuntime();
        if (_slimeAnimCts != null)
        {
            _slimeAnimCts.Cancel();
            _slimeAnimCts.Dispose();
            _slimeAnimCts = null;
        }
        _runtimeState.Reset();
        if (_blockController != null)
        {
            _blockController.Reset();
        }
        _currentQueueIndex = 0;
        _blocksQueue.Clear();
        if (_singleBlock != null)
        {
            _singleBlock.ClearContent();
            _singleBlock.transform.localPosition = Vector3.zero;
            _singleBlock.transform.localScale = Vector3.one;
        }
        if (spawnAnimator != null)
        {
            spawnAnimator.Cancel();
        }
        UpdateSingleBlockVisual(playAnimation: false);
    }

    public void AdvanceQueue()
    {
        _currentQueueIndex++;
        UpdateSingleBlockVisual(playAnimation: true);
    }

    public void RegressQueue()
    {
        if (_currentQueueIndex > 0)
        {
            _currentQueueIndex--;
            UpdateSingleBlockVisual(playAnimation: false);
        }
    }

    private void UpdateSingleBlockVisual(bool playAnimation = false)
    {
        if (_singleBlock == null) return;

        bool hasCurrent = _currentQueueIndex < _blocksQueue.Count;
        UpdateCurrentBlockVisual(hasCurrent, playAnimation);

        bool hasNext = hasCurrent && (_currentQueueIndex + 1 < _blocksQueue.Count);
        BlockRuntimeData nextBlock = hasNext ? _blocksQueue[_currentQueueIndex + 1] : null;

        float loadDuration = 0f;
        if (playAnimation && spawnAnimator != null && hasCurrent)
        {
            var currentBlock = _blocksQueue[_currentQueueIndex];
            if (currentBlock != null)
            {
                loadDuration = spawnAnimator.GetTotalDuration(currentBlock.CubeCount);
            }
        }

        if (!playAnimation)
        {
            UpdateRemainingColorMeshVisual(hasNext, nextBlock);
        }
        UpdateRemainingSlimeMeshVisual(hasNext, nextBlock, playAnimation, loadDuration);
    }

    private void UpdateCurrentBlockVisual(bool hasCurrent, bool playAnimation)
    {
        if (hasCurrent)
        {
            var currentData = _blocksQueue[_currentQueueIndex];
            if (playAnimation && spawnAnimator != null)
            {
                _singleBlock.transform.localScale = Vector3.zero;
            }
            _singleBlock.ApplyRuntimeData(currentData, suppressProgressAnimation: true);
            int slime1xLayer = LayerMask.NameToLayer("Slime1x");
            if (slime1xLayer >= 0)
            {
                _singleBlock.SetLayer(slime1xLayer);
            }
            if (playAnimation && spawnAnimator != null)
            {
                _singleBlock.SetVisualCubes(0, suppressProgressAnimation: true);
            }
            _singleBlock.gameObject.SetActive(true);
            _singleBlock.SetOwnerCarrier(this);

            if (remainingBlockCount != null)
            {
                remainingBlockCount.text = (_blocksQueue.Count - 1 - _currentQueueIndex).ToString();
            }

            if (playAnimation && spawnAnimator != null)
            {
                spawnAnimator.Play(_singleBlock);
            }
            else
            {
                if (spawnAnimator != null)
                {
                    spawnAnimator.Cancel();
                }
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
            _singleBlock.ClearContent();
            _singleBlock.gameObject.SetActive(false);

            if (remainingBlockCount != null)
            {
                remainingBlockCount.text = "0";
            }
        }
    }

    private void UpdateRemainingColorMeshVisual(bool hasNext, BlockRuntimeData nextBlock)
    {
        if (remainingColorMesh == null) return;

        _remainingColorPropBlock ??= new MaterialPropertyBlock();
        remainingColorMesh.GetPropertyBlock(_remainingColorPropBlock);

        if (hasNext && nextBlock != null)
        {
            var nextColorType = nextBlock.BlockColorType;
            RemainingColorConfigSO remainingConfig = null;
            if (ConfigManager.Instance != null)
            {
                remainingConfig = ConfigManager.Instance.GetRemainingColorConfig();
            }
#if UNITY_EDITOR
            if (remainingConfig == null)
            {
                remainingConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<RemainingColorConfigSO>("Assets/_Game/Config/CoreGameConfig/RemainingColorConfigSO.asset");
            }
#endif
            var remainingEntry = remainingConfig != null ? remainingConfig.GetColorEntry(nextColorType) : null;
            if (remainingEntry != null)
            {
                var col = remainingEntry.Color;
                _remainingColorPropBlock.SetColor(ColorPropId, col);
                _remainingColorPropBlock.SetColor(BaseColorPropId, col);
            }
            else
            {
                // Fallback to standard colorConfigSO
                var entry = colorConfigSO != null ? colorConfigSO.GetColorEntry(nextColorType) : null;
                if (entry != null)
                {
                    _remainingColorPropBlock.SetColorEntry(entry);
                    _remainingColorPropBlock.SetColor(BaseColorPropId, entry.Color);
                }
                else
                {
                    var col = nextBlock.Color;
                    _remainingColorPropBlock.SetColor(ColorPropId, col);
                    _remainingColorPropBlock.SetColor(BaseColorPropId, col);
                }
            }
        }
        else
        {
            Color noneCol = Color.white;
            RemainingColorConfigSO remainingConfig = null;
            if (ConfigManager.Instance != null)
            {
                remainingConfig = ConfigManager.Instance.GetRemainingColorConfig();
            }
#if UNITY_EDITOR
            if (remainingConfig == null)
            {
                remainingConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<RemainingColorConfigSO>("Assets/_Game/Config/CoreGameConfig/RemainingColorConfigSO.asset");
            }
#endif
            if (remainingConfig != null)
            {
                noneCol = remainingConfig.noneColor;
            }

            _remainingColorPropBlock.SetColor(ColorPropId, noneCol);
            _remainingColorPropBlock.SetColor(BaseColorPropId, noneCol);
        }

        remainingColorMesh.SetPropertyBlock(_remainingColorPropBlock);
        remainingColorMesh.gameObject.SetActive(true);
    }

    private void ApplySlimeColor(bool hasNext, BlockRuntimeData nextBlock)
    {
        if (remainingSlimeMesh == null) return;

        _remainingSlimePropBlock ??= new MaterialPropertyBlock();
        remainingSlimeMesh.GetPropertyBlock(_remainingSlimePropBlock);

        if (hasNext && nextBlock != null)
        {
            var nextColorType = nextBlock.BlockColorType;
            var config = ConfigManager.Instance != null ? ConfigManager.Instance.GetCubeColorConfig() : null;
            var entry = config != null ? config.GetColorEntry(nextColorType) : null;
            if (entry != null)
            {
                _remainingSlimePropBlock.SetColorEntry(entry);
            }
            else
            {
                var col = nextBlock.Color;
                _remainingSlimePropBlock.SetColor(ColorPropId, col);
            }
        }
        else
        {
            _remainingSlimePropBlock.SetColorWhite();
        }

        remainingSlimeMesh.SetPropertyBlock(_remainingSlimePropBlock);
        remainingSlimeMesh.gameObject.SetActive(true);
    }

    private void UpdateRemainingSlimeMeshVisual(bool hasNext, BlockRuntimeData nextBlock, bool playAnimation = false, float loadDuration = 0f)
    {
        if (remainingSlimeMesh == null) return;

        if (_slimeAnimCts != null)
        {
            _slimeAnimCts.Cancel();
            _slimeAnimCts.Dispose();
            _slimeAnimCts = null;
        }

        if (playAnimation && remainingSlimeAnimator != null)
        {
            remainingSlimeAnimator.PlayScaleDown(loadDuration);

            if (hasNext && nextBlock != null)
            {
                _slimeAnimCts = new CancellationTokenSource();
                UpdateSlimeVisualAfterDelay(nextBlock, loadDuration, _slimeAnimCts.Token).Forget();
            }
            else
            {
                _slimeAnimCts = new CancellationTokenSource();
                UpdateColorMeshAfterDelay(hasNext, nextBlock, loadDuration, _slimeAnimCts.Token).Forget();
            }
        }
        else
        {
            ApplySlimeColor(hasNext, nextBlock);
            if (remainingSlimeAnimator != null)
            {
                remainingSlimeAnimator.Cancel();
                remainingSlimeAnimator.SetScaleImmediate(hasNext);
            }
        }
    }

    private bool IsClawBoosterTransferring()
    {
        bool hasPopup = LayerManager.Instance != null && LayerManager.Instance.IsAnyPopupShowing();
        return !hasPopup 
            && BoosterSystem.Instance != null 
            && BoosterSystem.Instance.UseClawBooster 
            && BoosterSystem.Instance.IsClawAnimating;
    }

    private async UniTaskVoid UpdateSlimeVisualAfterDelay(BlockRuntimeData nextBlock, float delaySeconds, CancellationToken cancellationToken)
    {
        float elapsed = 0f;
        while (elapsed < delaySeconds)
        {
            if (cancellationToken.IsCancellationRequested) return;

            float ts = CustomTimeScaleGroup.Instance != null ? CustomTimeScaleGroup.Instance.CurrentTimeScale : 1f;
            if (IsClawBoosterTransferring())
            {
                ts = 1f;
            }
            
            await UniTask.Yield(PlayerLoopTiming.Update);
            elapsed += Time.unscaledDeltaTime * ts;
        }

        if (cancellationToken.IsCancellationRequested) return;
        if (remainingSlimeMesh == null || remainingSlimeAnimator == null) return;

        ApplySlimeColor(true, nextBlock);
        UpdateRemainingColorMeshVisual(true, nextBlock);
        remainingSlimeAnimator.PlayScaleUp();
    }

    private async UniTaskVoid UpdateColorMeshAfterDelay(bool hasNext, BlockRuntimeData nextBlock, float delaySeconds, CancellationToken cancellationToken)
    {
        float elapsed = 0f;
        while (elapsed < delaySeconds)
        {
            if (cancellationToken.IsCancellationRequested) return;

            float ts = CustomTimeScaleGroup.Instance != null ? CustomTimeScaleGroup.Instance.CurrentTimeScale : 1f;
            if (IsClawBoosterTransferring())
            {
                ts = 1f;
            }
            
            await UniTask.Yield(PlayerLoopTiming.Update);
            elapsed += Time.unscaledDeltaTime * ts;
        }

        if (cancellationToken.IsCancellationRequested) return;
        UpdateRemainingColorMeshVisual(hasNext, nextBlock);
    }

    private void Start()
    {
        AlignRotation();
    }

    private void OnDisable()
    {
        if (_slimeAnimCts != null)
        {
            _slimeAnimCts.Cancel();
            _slimeAnimCts.Dispose();
            _slimeAnimCts = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AlignRotation();
    }
#endif

    private void AlignRotation()
    {
        if (centerTrans != null)
        {
            centerTrans.rotation = Quaternion.Euler(0f, 0, 0f);
        }
    }
}

public class SpawnerBlockController : CarrierBlockController
{
    private readonly Spawner _spawner;
    private bool _hasCollectedVisibleBlock;

    public SpawnerBlockController(
        Spawner spawner,
        CarrierBlockLayoutBase layout,
        CarrierBlockFactory factory,
        CarrierRuntimeState runtimeState,
        int maxBlockCount) : base(spawner, layout, factory, runtimeState, maxBlockCount)
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
        if (_hasCollectedVisibleBlock) return null;
        if (_spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count) return null;
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
        {
            result.Add(block);
        }
        return result;
    }

    public override Block GetReceiveBlock(EBlockColorType blockColorType) => null;

    public override int GetBlockIndex(Block targetBlock)
    {
        if (targetBlock == _spawner.SingleBlock) return 0;
        return -1;
    }

    public override int CleanupEmptyBlocks()
    {
        _hasCollectedVisibleBlock = false;
        var singleBlock = _spawner.SingleBlock;
        if (singleBlock != null && singleBlock.HasContent && singleBlock.GetCurrentCubes() == 0)
        {
            singleBlock.ClearContent();
            _spawner.AdvanceQueue();
            if (_spawner.CurrentQueueIndex < _spawner.BlocksQueue.Count)
            {
                return 0;
            }
            else
            {
                _spawner.RuntimeState.MarkCompleted();
                return 1;
            }
        }
        return -1;
    }

    public override int RevealHiddenBlockAfterRelease(int releasedBlockIndex) => -1;
    public override void RefreshVisibleBlockVisuals()
    {
        _hasCollectedVisibleBlock = false;
        var singleBlock = _spawner.SingleBlock;
        if (singleBlock != null)
        {
            if (BoosterUndoSystem.Instance != null && BoosterUndoSystem.Instance.IsUndoAnimating)
            {
                if (_spawner.CurrentQueueIndex > 0)
                {
                    _spawner.RegressQueue();
                }
            }
            else if (!singleBlock.HasContent)
            {
                _spawner.AdvanceQueue();
                if (_spawner.CurrentQueueIndex >= _spawner.BlocksQueue.Count)
                {
                    _spawner.RuntimeState.MarkCompleted();
                }
            }
        }
    }
}
