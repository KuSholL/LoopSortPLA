using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ConveyorCornerDetector : MonoBehaviour
{
	private struct CornerScanState
	{
		public static CornerScanState None => new CornerScanState(false, 0f, 0f);

		public bool IsOpen
		{
			
			get;
		}

		private float Start
		{
			
			get;
		}

		private float End
		{
			
			get;
		}

		public float CenterProgress => (Start + End) * 0.5f;

		private CornerScanState(bool isOpen, float start, float end)
		{
			IsOpen = isOpen;
			Start = start;
			End = end;
		}

		public static CornerScanState Open(float progress)
		{
			return new CornerScanState(true, progress, progress);
		}

		public CornerScanState WithEnd(float progress)
		{
			return new CornerScanState(true, Start, progress);
		}
	}

	[SerializeField]
	private ConveyorCornerDetectorConfigSO config;

	[SerializeField]
	private List<float> cornerProgresses = new List<float>();

	private ConveyorPathRuntime _path;

	private bool _hasLoggedMissingConfig;

	public IReadOnlyList<float> CornerProgresses => cornerProgresses;

	public void UpdateCornerProgresses(ConveyorPathRuntime container, bool closed)
	{
		_path = container;
		if (!HasValidConfig())
		{
			cornerProgresses.Clear();
		}
		else
		{
			cornerProgresses = BuildCornerProgresses(container, closed, config.CornerProgressOffset);
		}
	}

	private List<float> BuildCornerProgresses(ConveyorPathRuntime spline, bool closed, float progressOffset)
	{
		List<float> progresses = new List<float>();
		if (spline == null)
		{
			return progresses;
		}
		ScanCornerProgresses(progresses, spline, closed);
		ApplyProgressOffset(progresses, progressOffset);
		return progresses;
	}

	private void ScanCornerProgresses(List<float> progresses, ConveyorPathRuntime spline, bool closed)
	{
		CornerScanState state = CornerScanState.None;
		foreach (float progress in GetScanProgresses(closed))
		{
			UpdateCornerCluster(progresses, spline, closed, progress, ref state);
		}
		TryCloseCluster(progresses, ref state);
	}

	private IEnumerable<float> GetScanProgresses(bool closed)
	{
		float start = (closed ? 0f : config.CornerSampleOffset);
		float end = (closed ? 1f : (1f - config.CornerSampleOffset));
		for (float progress = start; progress < end; progress += config.CornerScanStep)
		{
			yield return progress;
		}
	}

	private void UpdateCornerCluster(List<float> progresses, ConveyorPathRuntime spline, bool closed, float progress, ref CornerScanState state)
	{
		if (HasDirectionChange(spline, progress, closed))
		{
			state = (state.IsOpen ? state.WithEnd(progress) : CornerScanState.Open(progress));
		}
		else if (state.IsOpen)
		{
			progresses.Add(state.CenterProgress);
			state = CornerScanState.None;
		}
	}

	private void TryCloseCluster(List<float> progresses, ref CornerScanState state)
	{
		if (state.IsOpen)
		{
			progresses.Add(state.CenterProgress);
			state = CornerScanState.None;
		}
	}

	private bool HasDirectionChange(ConveyorPathRuntime spline, float progress, bool closed)
	{
		if (!TryGetSampleRange(progress, closed, out var before, out var after))
		{
			return false;
		}
		Vector3 incoming = GetSplineTangent(spline, before);
		Vector3 outgoing = GetSplineTangent(spline, after);
		return Vector3.Angle(incoming, outgoing) >= config.CornerAngleThreshold;
	}

	private bool TryGetSampleRange(float progress, bool closed, out float before, out float after)
	{
		before = (closed ? Mathf.Repeat(progress - config.CornerSampleOffset, 1f) : (progress - config.CornerSampleOffset));
		after = (closed ? Mathf.Repeat(progress + config.CornerSampleOffset, 1f) : (progress + config.CornerSampleOffset));
		if (closed)
		{
			return true;
		}
		return before > 0f && after < 1f;
	}

	private void ApplyProgressOffset(List<float> progresses, float progressOffset)
	{
		for (int i = 0; i < progresses.Count; i++)
		{
			progresses[i] = Mathf.Repeat(progresses[i] + progressOffset, 1f);
		}
	}

	private static Vector3 GetSplineTangent(ConveyorPathRuntime spline, float progress)
	{
		Vector3 tangent = spline?.EvaluateWorldTangent(progress) ?? Vector3.forward;
		return (tangent.sqrMagnitude > 1E-06f) ? tangent.normalized : Vector3.forward;
	}

	private void OnDrawGizmosSelected()
	{
		if (!HasValidConfig() || _path == null || !_path.IsValid || cornerProgresses.Count == 0)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		foreach (float progress in cornerProgresses)
		{
			DrawCornerPoint(progress);
		}
	}

	private void DrawCornerPoint(float progress)
	{
		Vector3 basePoint = GetCornerWorldPoint(progress);
		Vector3 topPoint = basePoint + Vector3.up * config.GizmoHeight;
		Gizmos.DrawSphere(basePoint, config.GizmoRadius);
		Gizmos.DrawLine(basePoint, topPoint);
	}

	private Vector3 GetCornerWorldPoint(float progress)
	{
		return _path.EvaluateWorldPosition(progress);
	}

	private bool HasValidConfig()
	{
		if (config != null)
		{
			_hasLoggedMissingConfig = false;
			return true;
		}
		if (_hasLoggedMissingConfig)
		{
			return false;
		}
		Debug.LogError("[ConveyorCornerDetector] Chua gan ConveyorCornerDetectorConfigSO tren object " + base.name + ".", this);
		_hasLoggedMissingConfig = true;
		return false;
	}
}
