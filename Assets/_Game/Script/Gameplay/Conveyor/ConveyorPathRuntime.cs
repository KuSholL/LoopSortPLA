using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Splines;
#endif

public sealed class ConveyorPathRuntime
{
    private const int LengthSamplesPerSegment = 12;
    private const int NearestSamples = 240;

    private readonly List<SplinePointData> _points = new List<SplinePointData>();
    private Transform _root;
    private bool _closed;
    private float _cachedLength = -1f;
#if UNITY_EDITOR
    private SplineContainer _editorSplineContainer;
#endif

    public Transform Root
    {
        get
        {
#if UNITY_EDITOR
            if (_editorSplineContainer != null) return _editorSplineContainer.transform;
#endif
            return _root;
        }
    }

    public bool Closed
    {
        get
        {
#if UNITY_EDITOR
            if (_editorSplineContainer != null && _editorSplineContainer.Spline != null) return _editorSplineContainer.Spline.Closed;
#endif
            return _closed;
        }
    }

    public int Count
    {
        get
        {
#if UNITY_EDITOR
            if (_editorSplineContainer != null && _editorSplineContainer.Spline != null) return _editorSplineContainer.Spline.Count;
#endif
            return _points.Count;
        }
    }

    public bool IsValid
    {
        get
        {
#if UNITY_EDITOR
            if (_editorSplineContainer != null && _editorSplineContainer.Spline != null)
            {
                return _editorSplineContainer.Spline.Count >= 2;
            }
#endif
            return _root != null && _points.Count >= 2 && GetSegmentCount() > 0;
        }
    }

    public void Setup(SplinePathData data, Transform root)
    {
        _root = root;
        _closed = data != null && data.Closed;
#if UNITY_EDITOR
        _editorSplineContainer = null;
#endif
        _points.Clear();
        if (data != null)
        {
            var ordered = data.GetMapPointsInOrder();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] != null) _points.Add(ordered[i]);
            }
        }
        _cachedLength = -1f;
    }

#if UNITY_EDITOR
    public void UseEditorSpline(SplineContainer splineContainer)
    {
        _editorSplineContainer = splineContainer;
        _cachedLength = -1f;
    }
#endif

    public Vector3 EvaluateLocalPosition(float progress)
    {
        if (!IsValid) return Vector3.zero;
#if UNITY_EDITOR
        if (_editorSplineContainer != null && _editorSplineContainer.Spline != null)
        {
            return _editorSplineContainer.Spline.EvaluatePosition(_editorSplineContainer.Spline.Closed ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress));
        }
#endif
        GetSegment(progress, out var current, out var next, out var t);
        return EvaluateBezier(GetPointPosition(current), GetOutHandle(current), GetInHandle(next), GetPointPosition(next), t);
    }

    public Vector3 EvaluateLocalTangent(float progress)
    {
        if (!IsValid) return Vector3.forward;
#if UNITY_EDITOR
        if (_editorSplineContainer != null && _editorSplineContainer.Spline != null)
        {
            var editorTangent = (Vector3)_editorSplineContainer.Spline.EvaluateTangent(_editorSplineContainer.Spline.Closed ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress));
            return editorTangent.sqrMagnitude > 0.000001f ? editorTangent.normalized : Vector3.forward;
        }
#endif
        GetSegment(progress, out var current, out var next, out var t);
        var tangent = EvaluateBezierDerivative(GetPointPosition(current), GetOutHandle(current), GetInHandle(next), GetPointPosition(next), t);
        return tangent.sqrMagnitude > 0.000001f ? tangent.normalized : Vector3.forward;
    }

    public void Evaluate(float progress, out Vector3 localPosition, out Vector3 localTangent, out Vector3 localUp)
    {
        localPosition = EvaluateLocalPosition(progress);
        localTangent = EvaluateLocalTangent(progress);
        localUp = Vector3.up;
    }

    public Vector3 TransformPoint(Vector3 localPosition)
    {
        return _root != null ? _root.TransformPoint(localPosition) : localPosition;
    }

    public Vector3 InverseTransformPoint(Vector3 worldPosition)
    {
        return _root != null ? _root.InverseTransformPoint(worldPosition) : worldPosition;
    }

    public Vector3 TransformDirection(Vector3 localDirection)
    {
        return _root != null ? _root.TransformDirection(localDirection) : localDirection;
    }

    public Vector3 EvaluateWorldPosition(float progress)
    {
        return TransformPoint(EvaluateLocalPosition(progress));
    }

    public Vector3 EvaluateWorldTangent(float progress)
    {
        var tangent = TransformDirection(EvaluateLocalTangent(progress));
        return tangent.sqrMagnitude > 0.000001f ? tangent.normalized : Vector3.forward;
    }

    public float CalculateLength()
    {
        if (_cachedLength >= 0f) return _cachedLength;
#if UNITY_EDITOR
        if (_editorSplineContainer != null && _editorSplineContainer.Spline != null)
        {
            _cachedLength = Mathf.Max(0.0001f, _editorSplineContainer.Spline.CalculateLength(_editorSplineContainer.transform.localToWorldMatrix));
            return _cachedLength;
        }
#endif
        if (!IsValid)
        {
            _cachedLength = 0f;
            return _cachedLength;
        }

        var segmentCount = GetSegmentCount();
        var length = 0f;
        var previous = EvaluateWorldPosition(0f);
        var sampleCount = Mathf.Max(2, segmentCount * LengthSamplesPerSegment);
        for (var i = 1; i <= sampleCount; i++)
        {
            var progress = i / (float)sampleCount;
            var current = EvaluateWorldPosition(progress);
            length += Vector3.Distance(previous, current);
            previous = current;
        }

        _cachedLength = Mathf.Max(0.0001f, length);
        return _cachedLength;
    }

    public void GetNearestPointGlobal(Vector3 localPosition, out Vector3 nearestPoint, out float progress, out Vector3 tangent)
    {
        GetNearestPointInternal(localPosition, 0f, 1f, NearestSamples, out nearestPoint, out progress, out tangent);
    }

    public void GetNearestPointLocal(Vector3 localPosition, float centerProgress, int searchRange, out Vector3 nearestPoint, out float progress, out Vector3 tangent)
    {
        if (!IsValid)
        {
            nearestPoint = Vector3.zero;
            progress = 0f;
            tangent = Vector3.forward;
            return;
        }

        var window = Mathf.Clamp01(searchRange / (float)NearestSamples);
        GetNearestPointInternal(localPosition, centerProgress - window, centerProgress + window, Mathf.Max(8, searchRange * 2 + 1), out nearestPoint, out progress, out tangent);
    }

    private void GetNearestPointInternal(Vector3 localPosition, float startProgress, float endProgress, int samples, out Vector3 nearestPoint, out float progress, out Vector3 tangent)
    {
        nearestPoint = Vector3.zero;
        progress = 0f;
        tangent = Vector3.forward;
        if (!IsValid) return;

        var bestDistance = float.MaxValue;
        samples = Mathf.Max(2, samples);
        for (var i = 0; i <= samples; i++)
        {
            var rawProgress = Mathf.Lerp(startProgress, endProgress, i / (float)samples);
            var sampleProgress = _closed ? Mathf.Repeat(rawProgress, 1f) : Mathf.Clamp01(rawProgress);
            var point = EvaluateLocalPosition(sampleProgress);
            var distance = (point - localPosition).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            nearestPoint = point;
            progress = sampleProgress;
        }

        tangent = EvaluateLocalTangent(progress);
    }

    private int GetSegmentCount()
    {
        if (_points.Count < 2) return 0;
        return _closed ? _points.Count : _points.Count - 1;
    }

    private void GetSegment(float progress, out SplinePointData current, out SplinePointData next, out float t)
    {
        var segmentCount = GetSegmentCount();
        progress = _closed ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);
        var scaled = progress * segmentCount;
        var index = Mathf.FloorToInt(scaled);
        if (index >= segmentCount)
        {
            index = segmentCount - 1;
            t = 1f;
        }
        else
        {
            t = scaled - index;
        }

        current = _points[index];
        next = _points[(index + 1) % _points.Count];
    }

    private static Vector3 GetPointPosition(SplinePointData point)
    {
        return point == null ? Vector3.zero : new Vector3(point.GridPosition.x, 0f, point.GridPosition.y);
    }

    private static Vector3 GetOutHandle(SplinePointData point)
    {
        return point == null ? Vector3.zero : GetPointPosition(point) + point.TangentOutValue;
    }

    private static Vector3 GetInHandle(SplinePointData point)
    {
        return point == null ? Vector3.zero : GetPointPosition(point) + point.TangentInValue;
    }

    private static Vector3 EvaluateBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var u = 1f - t;
        return u * u * u * p0
               + 3f * u * u * t * p1
               + 3f * u * t * t * p2
               + t * t * t * p3;
    }

    private static Vector3 EvaluateBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var u = 1f - t;
        return 3f * u * u * (p1 - p0)
               + 6f * u * t * (p2 - p1)
               + 3f * t * t * (p3 - p2);
    }
}
