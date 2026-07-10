using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConditionManager : MonoBehaviour
{
	[SerializeField]
	private GameConditionConfigSO config;

	private Coroutine _resultRoutine;

	private void OnEnable()
	{
		GameEventBus.OnWinTrigger = (Action)Delegate.Combine(GameEventBus.OnWinTrigger, new Action(OnWin));
		GameEventBus.OnLoseTrigger = (Action<ELoseReason>)Delegate.Combine(GameEventBus.OnLoseTrigger, new Action<ELoseReason>(OnLose));
		GameEventBus.OnCarrierUnloadDone = (Action)Delegate.Combine(GameEventBus.OnCarrierUnloadDone, new Action(CheckWinGuarantee));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Combine(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
	}

	private void OnDisable()
	{
		GameEventBus.OnWinTrigger = (Action)Delegate.Remove(GameEventBus.OnWinTrigger, new Action(OnWin));
		GameEventBus.OnLoseTrigger = (Action<ELoseReason>)Delegate.Remove(GameEventBus.OnLoseTrigger, new Action<ELoseReason>(OnLose));
		GameEventBus.OnCarrierUnloadDone = (Action)Delegate.Remove(GameEventBus.OnCarrierUnloadDone, new Action(CheckWinGuarantee));
		GameEventBus.OnCarrierFinished = (Action<EBlockColorType>)Delegate.Remove(GameEventBus.OnCarrierFinished, new Action<EBlockColorType>(OnCarrierFinished));
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
		LevelManager level = MonoSingleton<LevelManager>.Instance;
		if (level == null || level.IsGameEnded || config == null || !config.EnableWinGuaranteeSpeedUp)
		{
			return;
		}
		ConveyorDeliverySystem delivery = MonoSingleton<ConveyorDeliverySystem>.Instance;
		if (delivery != null && delivery.IsWinGuaranteed() && AreAllCarriersIdle())
		{
			CustomTimeScaleGroup timeScale = MonoSingleton<CustomTimeScaleGroup>.Instance;
			if (timeScale != null)
			{
				timeScale.ApplyTimeScale(config.WinGuaranteeSpeedMultiplier);
			}
		}
	}

	private static bool AreAllCarriersIdle()
	{
		CarrierSystem system = MonoSingleton<CarrierSystem>.Instance;
		IReadOnlyList<CarrierBase> carriers = ((system != null) ? system.SpawnedCarriers : null);
		if (carriers == null)
		{
			return true;
		}
		for (int i = 0; i < carriers.Count; i++)
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
		LevelManager level = MonoSingleton<LevelManager>.Instance;
		if (!(level == null) && !level.IsGameEnded)
		{
			if (_resultRoutine != null)
			{
				StopCoroutine(_resultRoutine);
			}
			_resultRoutine = StartCoroutine(ResultRoutine(isWin, reason));
		}
	}

	private IEnumerator ResultRoutine(bool isWin, ELoseReason loseReason)
	{
		LevelManager level = MonoSingleton<LevelManager>.Instance;
		if (level == null)
		{
			yield break;
		}
		level.IsGameEnded = true;
		level.IsPreloseDelay = !isWin;
		InputController.Disable();
		if (GameEventBus.OnEndGame != null)
		{
			GameEventBus.OnEndGame();
		}
		float delay = ((!isWin) ? ((config != null) ? config.LoseDelaySeconds : 1f) : ((config != null) ? config.WinDelaySeconds : 1f));
		if (isWin)
		{
			CustomTimeScaleGroup timeScale2 = MonoSingleton<CustomTimeScaleGroup>.Instance;
			if (timeScale2 != null)
			{
				timeScale2.ApplyTimeScale(1f);
			}
			if (delay > 0f)
			{
				yield return new WaitForSeconds(delay);
			}
		}
		else
		{
			float targetSpeed = ((config != null) ? config.PreloseTargetSpeedMultiplier : 0.2f);
			float elapsed = 0f;
			MonoSingleton<SoundManager>.Instance.PlayOneShot(AudioClipName.sfx_endgame_lose);
			while (elapsed < delay)
			{
				elapsed += Time.unscaledDeltaTime;
				float progress = ((delay > 0f) ? Mathf.Clamp01(elapsed / delay) : 1f);
				float eased = 1f - Mathf.Pow(1f - progress, 3f);
				CustomTimeScaleGroup timeScale = MonoSingleton<CustomTimeScaleGroup>.Instance;
				if (timeScale != null)
				{
					timeScale.ApplyTimeScale(Mathf.Lerp(1f, targetSpeed, eased));
				}
				yield return null;
			}
			float shakeDuration = ((config != null) ? config.LoseShakeDuration : 0.3f);
			float shakeMagnitude = ((config != null) ? config.LoseShakeMagnitude : 0.15f);
			if (MonoSingleton<CameraManager>.Instance != null && shakeDuration > 0f && shakeMagnitude > 0f)
			{
				yield return MonoSingleton<CameraManager>.Instance.ShakeCamera(shakeDuration, shakeMagnitude);
			}
			level.IsPreloseDelay = false;
		}
		if (isWin)
		{
			if (GameEventBus.OnLevelWin != null)
			{
				GameEventBus.OnLevelWin();
			}
		}
		else if (GameEventBus.OnPlayableLose != null)
		{
			GameEventBus.OnPlayableLose(loseReason);
		}
		_resultRoutine = null;
	}
}
