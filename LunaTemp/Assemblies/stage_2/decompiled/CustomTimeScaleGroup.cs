using System.Collections.Generic;
using UnityEngine;

public class CustomTimeScaleGroup : MonoSingleton<CustomTimeScaleGroup>
{
	[SerializeField]
	private List<MonoBehaviour> targets = new List<MonoBehaviour>();

	public float CurrentTimeScale { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		CurrentTimeScale = 1f;
		ApplyTimeScale(1f);
	}

	public void AddTarget(MonoBehaviour target)
	{
		if (!(target == null) && !targets.Contains(target))
		{
			targets.Add(target);
			if (target is ICustomTimeScaleTarget scaleTarget)
			{
				scaleTarget.SetCustomTimeScale(CurrentTimeScale);
			}
		}
	}

	public void RemoveTarget(MonoBehaviour target)
	{
		if (target != null)
		{
			targets.Remove(target);
		}
	}

	public void ClearTargets()
	{
		targets.Clear();
	}

	public void ApplyTimeScale(float timeScale)
	{
		timeScale = Mathf.Max(0f, timeScale);
		if (!Mathf.Approximately(CurrentTimeScale, timeScale) && GameEventBus.OnCustomTimeScaleChanged != null)
		{
			GameEventBus.OnCustomTimeScaleChanged(timeScale);
		}
		CurrentTimeScale = timeScale;
		for (int i = targets.Count - 1; i >= 0; i--)
		{
			MonoBehaviour behaviour = targets[i];
			if (behaviour == null)
			{
				targets.RemoveAt(i);
			}
			else if (behaviour is ICustomTimeScaleTarget target)
			{
				target.SetCustomTimeScale(timeScale);
			}
		}
	}
}
