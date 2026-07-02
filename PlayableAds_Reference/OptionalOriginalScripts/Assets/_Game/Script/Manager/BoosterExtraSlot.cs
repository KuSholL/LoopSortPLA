using Cysharp.Threading.Tasks;
using Alchemy.Inspector;
using UnityEngine;

public class BoosterExtraSlot : MonoBehaviour
{
    private LevelData _levelData;
    private int _nextCarrierIndex;
    private bool _isAddCarrierFlowPlaying;

    public void Init(LevelData levelData)
    {
        _levelData = levelData;
        ResetRuntimeState();
    }

    public bool CanUse()
    {
        if (_levelData == null) return false;

        var boosterCarriers = _levelData.CarrierLayout?.BoosterCarriers;
        return boosterCarriers != null
            && _nextCarrierIndex < boosterCarriers.Count
            && !_isAddCarrierFlowPlaying;
    }

    public bool TryUse()
    {
        if (!CanUse()) return false;
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
        PlayAnimAddCarrierFlowAsync().Forget();
        return true;
    }

    private async UniTaskVoid PlayAnimAddCarrierFlowAsync()
    {
        _isAddCarrierFlowPlaying = true;
        NotifyExtraSlotStateChanged();

        var buttonBooster = GameEventBus.GetTransBoosterTarget?.Invoke(BoosterType.ExtraSlotBooster);
        var boosterCarriers = _levelData?.CarrierLayout?.BoosterCarriers;

        try
        {
            if (buttonBooster == null || boosterCarriers == null || _nextCarrierIndex >= boosterCarriers.Count)
                return;

            var carrierStack = boosterCarriers[_nextCarrierIndex];
            if (carrierStack == null) return;

            _nextCarrierIndex++;

            var carrierSpawner = CarrierSystem.Instance?.CarrierSpawner;
            var mainCamera = CameraManager.Instance?.MainCamera;
            if (carrierSpawner == null || mainCamera == null || ExtraSlotAnimController.Instance == null)
                return;

            var startWorldPos = buttonBooster.position;
            var carrierPos = carrierSpawner.GetCarrierWorldPosition(carrierStack);
            var targetWorldPos = mainCamera.WorldToScreenPoint(carrierPos);

            await ExtraSlotAnimController.Instance.PlayAnimAddCarrierFlow(startWorldPos, targetWorldPos, carrierStack);
        }
        finally
        {
            _isAddCarrierFlowPlaying = false;
            NotifyExtraSlotStateChanged();
        }
    }

    private void ResetRuntimeState()
    {
        _nextCarrierIndex = 0;
        _isAddCarrierFlowPlaying = false;
    }

    private static void NotifyExtraSlotStateChanged()
    {
        GameEventBus.OnBoosterDataChanged?.Invoke(RewardType.ExtraSlotBooster);
    }

#if UNITY_EDITOR
    [Button]
    public void Play()
    {
        TryUse();
    }
#endif
}
