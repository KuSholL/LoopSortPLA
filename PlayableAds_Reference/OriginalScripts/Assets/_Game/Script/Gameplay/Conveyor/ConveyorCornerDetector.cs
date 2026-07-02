using System.Collections.Generic;
using Alchemy.Inspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorCornerDetector : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ConveyorCornerDetectorConfigSO config;
    [SerializeField, ReadOnly] private List<float> cornerProgresses = new();
    private bool _hasLoggedMissingConfig;

    public IReadOnlyList<float> CornerProgresses => cornerProgresses;

    public void UpdateCornerProgresses(SplineContainer container, bool closed)
    {
        splineContainer = container;
        if (!HasValidConfig())
        {
            cornerProgresses.Clear();
            return;
        }

        cornerProgresses = BuildCornerProgresses(
            container ? container.Spline : null,
            closed,
            config.CornerProgressOffset);
    }

    private List<float> BuildCornerProgresses(Spline spline, bool closed, float progressOffset)
    {
        var progresses = new List<float>();
        if (spline == null) return progresses;
        ScanCornerProgresses(progresses, spline, closed);
        ApplyProgressOffset(progresses, progressOffset);
        return progresses;
    }

    private void ScanCornerProgresses(List<float> progresses, Spline spline, bool closed)
    {
        var state = CornerScanState.None;
        foreach (var progress in GetScanProgresses(closed))
            UpdateCornerCluster(progresses, spline, closed, progress, ref state);
        TryCloseCluster(progresses, ref state);
    }

    private IEnumerable<float> GetScanProgresses(bool closed)
    {
        var start = closed ? 0f : config.CornerSampleOffset;
        var end = closed ? 1f : 1f - config.CornerSampleOffset;
        for (var progress = start; progress < end; progress += config.CornerScanStep)
            yield return progress;
    }

    private void UpdateCornerCluster(
        List<float> progresses,
        Spline spline,
        bool closed,
        float progress,
        ref CornerScanState state)
    {
        if (HasDirectionChange(spline, progress, closed))
        {
            state = state.IsOpen ? state.WithEnd(progress) : CornerScanState.Open(progress);
            return;
        }

        if (!state.IsOpen) return;
        progresses.Add(state.CenterProgress);
        state = CornerScanState.None;
    }

    private void TryCloseCluster(List<float> progresses, ref CornerScanState state)
    {
        if (!state.IsOpen) return;
        progresses.Add(state.CenterProgress);
        state = CornerScanState.None;
    }

    private bool HasDirectionChange(Spline spline, float progress, bool closed)
    {
        if (!TryGetSampleRange(progress, closed, out var before, out var after)) return false;
        var incoming = GetSplineTangent(spline, before);
        var outgoing = GetSplineTangent(spline, after);
        return Vector3.Angle(incoming, outgoing) >= config.CornerAngleThreshold;
    }

    private bool TryGetSampleRange(float progress, bool closed, out float before, out float after)
    {
        before = closed
            ? Mathf.Repeat(progress - config.CornerSampleOffset, 1f)
            : progress - config.CornerSampleOffset;
        after = closed
            ? Mathf.Repeat(progress + config.CornerSampleOffset, 1f)
            : progress + config.CornerSampleOffset;
        if (closed) return true;
        return before > 0f && after < 1f;
    }

    private void ApplyProgressOffset(List<float> progresses, float progressOffset)
    {
        for (var i = 0; i < progresses.Count; i++)
            progresses[i] = Mathf.Repeat(progresses[i] + progressOffset, 1f);
    }

    private static Vector3 GetSplineTangent(Spline spline, float progress)
    {
        return math.normalizesafe(spline.EvaluateTangent(progress), math.forward());
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasValidConfig() || splineContainer == null || cornerProgresses.Count == 0) return;
        Gizmos.color = Color.yellow;
        foreach (var progress in cornerProgresses) DrawCornerPoint(progress);
    }

    private void DrawCornerPoint(float progress)
    {
        var basePoint = GetCornerWorldPoint(progress);
        var topPoint = basePoint + Vector3.up * config.GizmoHeight;
        Gizmos.DrawSphere(basePoint, config.GizmoRadius);
        Gizmos.DrawLine(basePoint, topPoint);
    }

    private Vector3 GetCornerWorldPoint(float progress)
    {
        var localPoint = splineContainer.Spline.EvaluatePosition(progress);
        return splineContainer.transform.TransformPoint(localPoint);
    }

    private bool HasValidConfig()
    {
        if (config != null)
        {
            _hasLoggedMissingConfig = false;
            return true;
        }

        if (_hasLoggedMissingConfig) return false;
        Debug.LogError(
            $"[{nameof(ConveyorCornerDetector)}] Chua gan {nameof(ConveyorCornerDetectorConfigSO)} tren object {name}.",
            this);
        _hasLoggedMissingConfig = true;
        return false;
    }

    private readonly struct CornerScanState
    {
        public static CornerScanState None => new(false, 0f, 0f);
        public bool IsOpen { get; }
        private float Start { get; }
        private float End { get; }
        public float CenterProgress => (Start + End) * .5f;

        private CornerScanState(bool isOpen, float start, float end)
        {
            IsOpen = isOpen;
            Start = start;
            End = end;
        }

        public static CornerScanState Open(float progress) => new(true, progress, progress);
        public CornerScanState WithEnd(float progress) => new(true, Start, progress);
    }
}
