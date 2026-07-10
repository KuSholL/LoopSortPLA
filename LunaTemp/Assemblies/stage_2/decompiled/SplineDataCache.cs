using UnityEngine;
using UnityEngine.Splines;

public class SplineDataCache
{
	public struct SamplePoint
	{
		public float Progress;

		public Vector3 LocalPosition;

		public Vector3 LocalTangent;
	}

	private readonly SamplePoint[] _samples;

	private readonly int _sampleCount;

	private readonly bool _isClosed;

	public SplineDataCache(Spline spline, int sampleCount = 200)
	{
		_sampleCount = sampleCount;
		_samples = new SamplePoint[sampleCount];
		_isClosed = spline.Closed;
		for (int i = 0; i < sampleCount; i++)
		{
			float progress = (float)i / (float)(sampleCount - 1);
			_samples[i] = new SamplePoint
			{
				Progress = progress,
				LocalPosition = spline.EvaluatePosition(progress),
				LocalTangent = ((Vector3)spline.EvaluateTangent(progress)).normalized
			};
		}
	}

	public void GetNearestPointLocal(Vector3 localPosition, float startProgress, int searchRange, out Vector3 nearestLocalPoint, out float progress, out Vector3 localTangent)
	{
		int approxIndex = Mathf.RoundToInt(startProgress * (float)(_sampleCount - 1));
		approxIndex = Mathf.Clamp(approxIndex, 0, _sampleCount - 1);
		float distToGuessSqr = (localPosition - _samples[approxIndex].LocalPosition).sqrMagnitude;
		if (distToGuessSqr > 4f)
		{
			GetNearestPointGlobal(localPosition, out nearestLocalPoint, out progress, out localTangent);
			return;
		}
		float minDistanceSqr = float.MaxValue;
		int bestIndex = approxIndex;
		int actualSearchRange = Mathf.Min(searchRange, _sampleCount / 2);
		for (int i = -actualSearchRange; i <= actualSearchRange; i++)
		{
			int index = approxIndex + i;
			index = ((!_isClosed) ? Mathf.Clamp(index, 0, _sampleCount - 1) : ((index + _sampleCount) % _sampleCount));
			float distSqr = (localPosition - _samples[index].LocalPosition).sqrMagnitude;
			if (distSqr < minDistanceSqr)
			{
				minDistanceSqr = distSqr;
				bestIndex = index;
			}
		}
		InterpolateSample(localPosition, bestIndex, out nearestLocalPoint, out progress, out localTangent);
	}

	public void GetNearestPointGlobal(Vector3 localPosition, out Vector3 nearestLocalPoint, out float progress, out Vector3 localTangent)
	{
		float minDistanceSqr = float.MaxValue;
		int bestIndex = 0;
		for (int i = 0; i < _sampleCount; i++)
		{
			float distSqr = (localPosition - _samples[i].LocalPosition).sqrMagnitude;
			if (distSqr < minDistanceSqr)
			{
				minDistanceSqr = distSqr;
				bestIndex = i;
			}
		}
		InterpolateSample(localPosition, bestIndex, out nearestLocalPoint, out progress, out localTangent);
	}

	private void InterpolateSample(Vector3 localPosition, int bestIndex, out Vector3 nearestLocalPoint, out float progress, out Vector3 localTangent)
	{
		SamplePoint bestSample = _samples[bestIndex];
		int prevIndex = bestIndex - 1;
		int nextIndex = bestIndex + 1;
		if (_isClosed)
		{
			prevIndex = (prevIndex + _sampleCount) % _sampleCount;
			nextIndex = (nextIndex + _sampleCount) % _sampleCount;
		}
		else
		{
			prevIndex = Mathf.Clamp(prevIndex, 0, _sampleCount - 1);
			nextIndex = Mathf.Clamp(nextIndex, 0, _sampleCount - 1);
		}
		SamplePoint prevSample = _samples[prevIndex];
		SamplePoint nextSample = _samples[nextIndex];
		SamplePoint neighbor = (((localPosition - prevSample.LocalPosition).sqrMagnitude < (localPosition - nextSample.LocalPosition).sqrMagnitude) ? prevSample : nextSample);
		Vector3 segment = neighbor.LocalPosition - bestSample.LocalPosition;
		float segmentLengthSqr = segment.sqrMagnitude;
		if (segmentLengthSqr > 1E-06f)
		{
			Vector3 toPos = localPosition - bestSample.LocalPosition;
			float tProj = Mathf.Clamp01(Vector3.Dot(toPos, segment) / segmentLengthSqr);
			nearestLocalPoint = bestSample.LocalPosition + tProj * segment;
			float progressDiff = neighbor.Progress - bestSample.Progress;
			if (progressDiff > 0.5f)
			{
				progressDiff -= 1f;
			}
			else if (progressDiff < -0.5f)
			{
				progressDiff += 1f;
			}
			progress = Mathf.Repeat(bestSample.Progress + tProj * progressDiff, 1f);
			localTangent = Vector3.Lerp(bestSample.LocalTangent, neighbor.LocalTangent, tProj).normalized;
		}
		else
		{
			nearestLocalPoint = bestSample.LocalPosition;
			progress = bestSample.Progress;
			localTangent = bestSample.LocalTangent;
		}
	}
}
