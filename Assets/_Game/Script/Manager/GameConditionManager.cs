using System.Collections;
using UnityEngine;

public class GameConditionManager : MonoBehaviour
{
    [SerializeField] private GameConditionConfigSO config;
    private Coroutine _resultRoutine;

    private void OnEnable()
    {
        GameEventBus.OnWinTrigger += OnWin;
        GameEventBus.OnLoseTrigger += OnLose;
        GameEventBus.OnCarrierUnloadDone += CheckWinGuarantee;
        GameEventBus.OnCarrierFinished += OnCarrierFinished;
    }

    private void OnDisable()
    {
        GameEventBus.OnWinTrigger -= OnWin;
        GameEventBus.OnLoseTrigger -= OnLose;
        GameEventBus.OnCarrierUnloadDone -= CheckWinGuarantee;
        GameEventBus.OnCarrierFinished -= OnCarrierFinished;
        if (_resultRoutine != null)
        {
            StopCoroutine(_resultRoutine);
            _resultRoutine = null;
        }
    }

    private void OnCarrierFinished(EBlockColorType color)
    {
        CheckWinGuarantee();
    }

    private void CheckWinGuarantee()
    {
        var level = LevelManager.Instance;
        if (level == null || level.IsGameEnded || config == null || !config.EnableWinGuaranteeSpeedUp)
        {
            return;
        }

        var delivery = ConveyorDeliverySystem.Instance;
        if (delivery != null && delivery.IsWinGuaranteed() && AreAllCarriersIdle())
        {
            var timeScale = CustomTimeScaleGroup.Instance;
            if (timeScale != null)
            {
                timeScale.ApplyTimeScale(config.WinGuaranteeSpeedMultiplier);
            }
        }
    }

    private static bool AreAllCarriersIdle()
    {
        var system = CarrierSystem.Instance;
        var carriers = system != null ? system.SpawnedCarriers : null;
        if (carriers == null) return true;
        for (var i = 0; i < carriers.Count; i++)
        {
            if (carriers[i] != null && carriers[i].IsDelivering)
            {
                return false;
            }
        }
        return true;
    }

    private void OnWin()
    {
        BeginResult(true, ELoseReason.None);
    }

    private void OnLose(ELoseReason reason)
    {
        BeginResult(false, reason);
    }

    private void BeginResult(bool isWin, ELoseReason reason)
    {
        var level = LevelManager.Instance;
        if (level == null || level.IsGameEnded) return;
        if (_resultRoutine != null) StopCoroutine(_resultRoutine);
        _resultRoutine = StartCoroutine(ResultRoutine(isWin, reason));
    }

    private IEnumerator ResultRoutine(bool isWin, ELoseReason loseReason)
    {
        var level = LevelManager.Instance;
        if (level == null) yield break;

        level.IsGameEnded = true;
        level.IsPreloseDelay = !isWin;
        InputController.Disable();
        if (GameEventBus.OnEndGame != null) GameEventBus.OnEndGame();

        var delay = isWin
            ? (config != null ? config.WinDelaySeconds : 1f)
            : (config != null ? config.LoseDelaySeconds : 1f);

        if (isWin)
        {
            var timeScale = CustomTimeScaleGroup.Instance;
            if (timeScale != null) timeScale.ApplyTimeScale(1f);
            if (delay > 0f) yield return new WaitForSeconds(delay);
        }
        else
        {
            var targetSpeed = config != null ? config.PreloseTargetSpeedMultiplier : 0.2f;
            var elapsed = 0f;
            SoundManager.Instance.PlayOneShot(AudioClipName.sfx_endgame_lose);
            while (elapsed < delay)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = delay > 0f ? Mathf.Clamp01(elapsed / delay) : 1f;
                var eased = 1f - Mathf.Pow(1f - progress, 3f);
                var timeScale = CustomTimeScaleGroup.Instance;
                if (timeScale != null) timeScale.ApplyTimeScale(Mathf.Lerp(1f, targetSpeed, eased));
                yield return null;
            }

            var shakeDuration = config != null ? config.LoseShakeDuration : 0.3f;
            var shakeMagnitude = config != null ? config.LoseShakeMagnitude : 0.15f;
            if (CameraManager.Instance != null && shakeDuration > 0f && shakeMagnitude > 0f)
            {
                yield return CameraManager.Instance.ShakeCamera(shakeDuration, shakeMagnitude);
            }
            level.IsPreloseDelay = false;
        }

        if (isWin)
        {
            if (GameEventBus.OnLevelWin != null) GameEventBus.OnLevelWin();
        }
        else
        {
            if (GameEventBus.OnPlayableLose != null) GameEventBus.OnPlayableLose(loseReason);
        }
        _resultRoutine = null;
    }
}
