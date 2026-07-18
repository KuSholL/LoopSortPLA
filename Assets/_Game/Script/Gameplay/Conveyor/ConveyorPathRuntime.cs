using System.Collections.Generic;
using UnityEngine;

public sealed class ConveyorPathRuntime
{
    private const int LengthSamplesPerSegment = 12;
    private const int NearestSamples = 240;

    private readonly List<PathKnot> _knots = new List<PathKnot>();
    private readonly Vector3[] _samplePositions = new Vector3[NearestSamples];
    private readonly Vector3[] _sampleTangents = new Vector3[NearestSamples];
    private Transform _root;
    private bool _closed;
    private float _cachedLength = -1f;

    public Transform Root => _root;

    public bool Closed => _closed;

    public int Count => _knots.Count;

    public bool IsValid => _root != null && _knots.Count >= 2 && GetSegmentCount() > 0;

    public void Setup(SplinePathData data, Transform root)
    {
        _root = root;
        _closed = data != null && data.Closed;
        _knots.Clear();
        if (data != null)
        {
            var ordered = data.GetMapPointsInOrder();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] != null) _knots.Add(CreateKnot(ordered[i]));
            }
        }
        _cachedLength = -1f;
        BuildNearestSampleCache();
    }

    public Vector3 EvaluateLocalPosition(float progress)
    {
        if (!IsValid) return Vector3.zero;
        GetSegment(progress, out var current, out var next, out var t);
        return EvaluateBezier(
            _knots[current].Position,
            _knots[current].OutHandle,
            _knots[next].InHandle,
            _knots[next].Position,
            t);
    }

    public Vector3 EvaluateLocalTangent(float progress)
    {
        if (!IsValid) return Vector3.forward;
        GetSegment(progress, out var current, out var next, out var t);
        var tangent = EvaluateBezierDerivative(
            _knots[current].Position,
            _knots[current].OutHandle,
            _knots[next].InHandle,
            _knots[next].Position,
            t);
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
        if (!IsValid)
        {
            nearestPoint = Vector3.zero;
            progress = 0f;
            tangent = Vector3.forward;
            return;
        }

        var bestIndex = 0;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < NearestSamples; i++)
        {
            var distance = (localPosition - _samplePositions[i]).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            bestIndex = i;
        }

        InterpolateNearestSample(localPosition, bestIndex, out nearestPoint, out progress, out tangent);
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

        var normalizedCenter = _closed ? Mathf.Repeat(centerProgress, 1f) : Mathf.Clamp01(centerProgress);
        var approximateIndex = _closed
            ? Mathf.RoundToInt(normalizedCenter * NearestSamples) % NearestSamples
            : Mathf.RoundToInt(normalizedCenter * (NearestSamples - 1));
        approximateIndex = Mathf.Clamp(approximateIndex, 0, NearestSamples - 1);
        if ((localPosition - _samplePositions[approximateIndex]).sqrMagnitude > 4f)
        {
            GetNearestPointGlobal(localPosition, out nearestPoint, out progress, out tangent);
            return;
        }

        var bestIndex = approximateIndex;
        var bestDistance = float.MaxValue;
        var actualSearchRange = Mathf.Min(Mathf.Max(1, searchRange), NearestSamples / 2);
        for (var offset = -actualSearchRange; offset <= actualSearchRange; offset++)
        {
            var index = approximateIndex + offset;
            if (_closed)
            {
                index = (index + NearestSamples) % NearestSamples;
            }
            else
            {
                index = Mathf.Clamp(index, 0, NearestSamples - 1);
            }

            var distance = (localPosition - _samplePositions[index]).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            bestIndex = index;
        }

        InterpolateNearestSample(localPosition, bestIndex, out nearestPoint, out progress, out tangent);
    }

    private void BuildNearestSampleCache()
    {
        if (!IsValid) return;
        for (var i = 0; i < NearestSamples; i++)
        {
            var progress = GetSampleProgress(i);
            _samplePositions[i] = EvaluateLocalPosition(progress);
            _sampleTangents[i] = EvaluateLocalTangent(progress);
        }
    }

    private void InterpolateNearestSample(
        Vector3 localPosition,
        int bestIndex,
        out Vector3 nearestPoint,
        out float progress,
        out Vector3 tangent)
    {
        var previousIndex = bestIndex - 1;
        var nextIndex = bestIndex + 1;
        if (_closed)
        {
            previousIndex = (previousIndex + NearestSamples) % NearestSamples;
            nextIndex %= NearestSamples;
        }
        else
        {
            previousIndex = Mathf.Clamp(previousIndex, 0, NearestSamples - 1);
            nextIndex = Mathf.Clamp(nextIndex, 0, NearestSamples - 1);
        }

        var previousDistance = (localPosition - _samplePositions[previousIndex]).sqrMagnitude;
        var nextDistance = (localPosition - _samplePositions[nextIndex]).sqrMagnitude;
        var neighborIndex = previousDistance < nextDistance ? previousIndex : nextIndex;
        var bestPoint = _samplePositions[bestIndex];
        var neighborPoint = _samplePositions[neighborIndex];
        var segment = neighborPoint - bestPoint;
        var segmentLengthSqr = segment.sqrMagnitude;

        var bestProgress = GetSampleProgress(bestIndex);
        if (segmentLengthSqr <= 0.000001f)
        {
            nearestPoint = bestPoint;
            progress = Mathf.Repeat(bestProgress, 1f);
            tangent = _sampleTangents[bestIndex];
            return;
        }

        var projection = Mathf.Clamp01(Vector3.Dot(localPosition - bestPoint, segment) / segmentLengthSqr);
        var neighborProgress = GetSampleProgress(neighborIndex);
        var progressDelta = neighborProgress - bestProgress;
        if (_closed)
        {
            if (progressDelta > 0.5f) progressDelta -= 1f;
            else if (progressDelta < -0.5f) progressDelta += 1f;
        }

        nearestPoint = bestPoint + segment * projection;
        progress = _closed
            ? Mathf.Repeat(bestProgress + progressDelta * projection, 1f)
            : Mathf.Clamp01(bestProgress + progressDelta * projection);
        tangent = Vector3.Lerp(
            _sampleTangents[bestIndex],
            _sampleTangents[neighborIndex],
            projection).normalized;
    }

    private float GetSampleProgress(int index)
    {
        return _closed
            ? index / (float)NearestSamples
            : index / (float)(NearestSamples - 1);
    }

    private int GetSegmentCount()
    {
        if (_knots.Count < 2) return 0;
        return _closed ? _knots.Count : _knots.Count - 1;
    }

    private void GetSegment(float progress, out int current, out int next, out float t)
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

        current = index;
        next = (index + 1) % _knots.Count;
    }

    private static PathKnot CreateKnot(SplinePointData point)
    {
        var position = new Vector3(point.GridPosition.x, 0f, point.GridPosition.y);
        var rotation = Quaternion.Euler(point.Rotation);
        return new PathKnot(
            position,
            position + rotation * point.TangentInValue,
            position + rotation * point.TangentOutValue);
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

    private struct PathKnot
    {
        public readonly Vector3 Position;
        public readonly Vector3 InHandle;
        public readonly Vector3 OutHandle;

        public PathKnot(Vector3 position, Vector3 inHandle, Vector3 outHandle)
        {
            Position = position;
            InHandle = inHandle;
            OutHandle = outHandle;
        }
    }
}
