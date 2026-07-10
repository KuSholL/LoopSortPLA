using UnityEngine;
using UnityEngine.Splines;

public static class CarrierExtensions
{
	public static void SetPoseOnSpline(this Carrier carrier, SplineContainer spline, float progress, Vector3 position, float rotationY)
	{
		if (!(carrier == null) && !(spline == null))
		{
			float t = Mathf.Repeat(progress, 1f);
			Vector3 worldPosition = spline.transform.TransformPoint(position);
			Vector3 splinePosition = GetSplinePosition(spline, t);
			Vector3 tangent = GetSplineTangent(spline, t);
			Vector3 right = GetCarrierRight(tangent);
			carrier.transform.position = worldPosition;
			carrier.transform.rotation = GetCarrierRotation(worldPosition, splinePosition, right, rotationY);
			carrier.SetSplineProgress(t);
		}
	}

	private static Vector3 GetSplinePosition(SplineContainer spline, float t)
	{
		return spline.transform.TransformPoint(spline.EvaluatePosition(t));
	}

	private static Vector3 GetSplineTangent(SplineContainer spline, float t)
	{
		return spline.transform.TransformDirection(spline.EvaluateTangent(t)).normalized;
	}

	private static Vector3 GetCarrierRight(Vector3 tangent)
	{
		Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
		return (right.sqrMagnitude < 0.001f) ? Vector3.right : right;
	}

	private static Quaternion GetCarrierRotation(Vector3 worldPosition, Vector3 splinePosition, Vector3 right, float rotationY)
	{
		float lateral = Vector3.Dot(worldPosition - splinePosition, right);
		Vector3 facing = ((lateral < 0f) ? (-right) : right);
		Quaternion baseRotation = Quaternion.LookRotation(facing, Vector3.up);
		return baseRotation * Quaternion.Euler(0f, rotationY, 0f);
	}
}
