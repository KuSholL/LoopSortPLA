using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Quản lý việc kiểm tra và kích hoạt trạng thái kết thúc game (Thắng/Thua).
/// </summary>
public class GameConditionManager : MonoBehaviour
{
    [SerializeField] private GameConditionConfigSO config;

    private void OnEnable()
    {
        GameEventBus.OnWinTrigger += OnTriggerWin;
        GameEventBus.OnLoseTrigger += OnTriggerLoser;
        GameEventBus.OnCarrierUnloadDone += OnCarrierUnloadDone;
        GameEventBus.OnCarrierFinished += OnCarrierFinished;
        GameEventBus.OnContainerUnlocked += OnContainerUnlocked;
    }

    private void OnDisable()
    {
        GameEventBus.OnWinTrigger -= OnTriggerWin;
        GameEventBus.OnLoseTrigger -= OnTriggerLoser;
        GameEventBus.OnCarrierUnloadDone -= OnCarrierUnloadDone;
        GameEventBus.OnCarrierFinished -= OnCarrierFinished;
        GameEventBus.OnContainerUnlocked -= OnContainerUnlocked;
    }

    private void OnCarrierUnloadDone()
    {
        CheckAndApplyWinGuaranteeSpeedUp();
    }

    private void OnCarrierFinished(EBlockColorType colorType)
    {
        CheckAndApplyWinGuaranteeSpeedUp();
    }

    private void OnContainerUnlocked()
    {
        CheckAndApplyWinGuaranteeSpeedUp();
    }

    private bool AreAllCarriersUnloaded()
    {
        if (CarrierSystem.Instance == null || CarrierSystem.Instance.CarrierSpawner == null) return true;
        var spawnedCarriers = CarrierSystem.Instance.CarrierSpawner.SpawnedCarriers;
        if (spawnedCarriers == null) return true;
        foreach (var carrier in spawnedCarriers)
        {
            if (carrier != null && carrier.IsDelivering)
            {
                return false;
            }
        }
        return true;
    }

    private void CheckAndApplyWinGuaranteeSpeedUp()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsGameEnded) return;

        if (config != null && config.EnableWinGuaranteeSpeedUp)
        {
            if (ConveyorDeliverySystem.Instance != null && ConveyorDeliverySystem.Instance.IsWinGuaranteed())
            {
                if (AreAllCarriersUnloaded())
                {
                    CustomTimeScaleGroup.Instance?.ApplyTimeScale(config.WinGuaranteeSpeedMultiplier);
                }
            }
        }
    }

    private void OnTriggerWin()
    {
        CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
        TriggerResult(isWin: true).Forget();
    }

    private void OnTriggerLoser(ELoseReason reason)
    {
        TriggerResult(isWin: false, reason).Forget();
    }

    private async UniTask TriggerResult(bool isWin, ELoseReason loseReason = ELoseReason.None)
    {
        // Double check để tránh race condition
        if (LevelManager.Instance.IsGameEnded) return;

        if (!isWin)
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.IsPreloseDelay = true;
                LevelManager.Instance.IsPreloseDelayPaused = false;
            }
            BoosterUndoSystem.Instance?.PublishAvailability();
            UserBehaviorTracker.SendInGameStuckTracking();
        }

        LevelManager.Instance.IsGameEnded = true;
        GameEventBus.OnEndGame?.Invoke();

        // Lấy giá trị từ Config SO (nếu chưa gán thì dùng mặc định để tránh lỗi)
        float winDelaySecondsVal = config != null ? config.WinDelaySeconds : 1f;
        float loseDelaySecondsVal = config != null ? config.LoseDelaySeconds : 1f;
        float targetSpeedVal = config != null ? config.PreloseTargetSpeedMultiplier : 0.2f;
        float shakeDurationVal = config != null ? config.LoseShakeDuration : 0.5f;
        float shakeMagnitudeVal = config != null ? config.LoseShakeMagnitude : 0.15f;

        float activeDelaySeconds = isWin ? winDelaySecondsVal : loseDelaySecondsVal;

        if (!isWin)
        {
            float totalDuration = activeDelaySeconds + shakeDurationVal;
            float soundLength = SoundManager.Instance != null ? SoundManager.Instance.GetClipLength(AudioClipName.sfx_endgame_lose) : 0f;
            float startSoundTime = Mathf.Max(0f, totalDuration - soundLength);
            PlayEndgameLoseSoundWithDelay(startSoundTime, this.GetCancellationTokenOnDestroy()).Forget();
        }

        // Delay x seconds before showing popup
        if (activeDelaySeconds > 0f)
        {
            if (!isWin)
            {
                float elapsed = 0f;
                float startSpeed = 1f;
                float targetSpeed = targetSpeedVal;

                while (elapsed < activeDelaySeconds)
                {
                    // Nếu người chơi đã dùng booster để cứu game
                    if (LevelManager.Instance != null && (!LevelManager.Instance.IsGameEnded || !LevelManager.Instance.IsPreloseDelay))
                    {
                        CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
                        SoundManager.Instance?.StopOneShot();
                        return;
                    }

                    // Tạm dừng đếm ngược delay nếu đang mở popup mua/nhận booster
                    if (LevelManager.Instance != null && LevelManager.Instance.IsPreloseDelayPaused)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
                        continue;
                    }

                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / activeDelaySeconds);
                    
                    // Sử dụng Ease-Out Cubic để giảm tốc nhanh lúc đầu, và dừng cực kỳ nhẹ nhàng ở đoạn cuối
                    float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                    
                    float currentSpeed = Mathf.Lerp(startSpeed, targetSpeed, easedProgress);
                    CustomTimeScaleGroup.Instance?.ApplyTimeScale(currentSpeed);

                    await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
                }

                // Ensure precise ending speed
                CustomTimeScaleGroup.Instance?.ApplyTimeScale(targetSpeed);
            }
            else
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(activeDelaySeconds), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        if (!isWin && LevelManager.Instance != null)
        {
            LevelManager.Instance.IsPreloseDelay = false;
            LevelManager.Instance.IsPreloseDelayPaused = false;
            BoosterUndoSystem.Instance?.PublishAvailability();
        }

        // Kiểm tra lại xem game có bị dừng/thua không trước khi thực hiện rung camera và show popup
        if (!isWin && LevelManager.Instance != null && !LevelManager.Instance.IsGameEnded)
        {
            CustomTimeScaleGroup.Instance?.ApplyTimeScale(1f);
            return;
        }

        // Chỉ rung camera khi Thua, sau khi giảm tốc và trước khi hiện UI
        if (!isWin && CameraManager.Instance != null && shakeDurationVal > 0f && shakeMagnitudeVal > 0f)
        {
            await CameraManager.Instance.ShakeCamera(shakeDurationVal, shakeMagnitudeVal);
        }
        
        //Show prelose
        if (BoosterSystem.Instance.CanUseBoosterExtraSlot() && !isWin)
        {
            LayerManager.Instance.ShowPopupPreLose(loseReason);
            return;
        }
        
        //Show end game
        if (LayerManager.Instance != null && GameStateManager.Instance.IsState(GameState.InGame))
        {
            LayerManager.Instance.ShowEndGameLayer(new UIEndGameData
            {
                IsWinMatch = isWin
            });
        }
    }

    private async UniTask PlayEndgameLoseSoundWithDelay(float delaySeconds, System.Threading.CancellationToken cancellationToken)
    {
        if (delaySeconds > 0f)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(delaySeconds), cancellationToken: cancellationToken);
        }
        
        SoundManager.Instance?.PlayOneShot(AudioClipName.sfx_endgame_lose);
    }
}