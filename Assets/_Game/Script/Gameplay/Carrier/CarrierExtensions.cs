using UnityEngine;

public static class CarrierExtensions
{
    public static void SetPoseOnSpline(
        this Carrier carrier,
        ConveyorPathRuntime spline,
        float progress,
        Vector3 position,
        float rotationY)
    {
        if (carrier == null || spline == null || !spline.IsValid) return;
        var t = Mathf.Repeat(progress, 1f);
        var worldPosition = spline.TransformPoint(position);
        var splinePosition = GetSplinePosition(spline, t);
        var tangent = GetSplineTangent(spline, t);
        var right = GetCarrierRight(tangent);
        carrier.transform.position = worldPosition;
        carrier.transform.rotation = GetCarrierRotation(worldPosition, splinePosition, right, rotationY);
        carrier.SetSplineProgress(t);
    }

    private static Vector3 GetSplinePosition(ConveyorPathRuntime spline, float t)
    {
        return spline.EvaluateWorldPosition(t);
    }

    private static Vector3 GetSplineTangent(ConveyorPathRuntime spline, float t)
    {
        return spline.EvaluateWorldTangent(t);
    }

    private static Vector3 GetCarrierRight(Vector3 tangent)
    {
        var right = Vector3.Cross(Vector3.up, tangent).normalized;
        return right.sqrMagnitude < 0.001f ? Vector3.right : right;
    }

    private static Quaternion GetCarrierRotation(
        Vector3 worldPosition,
        Vector3 splinePosition,
        Vector3 right,
        float rotationY)
    {
        var lateral = Vector3.Dot(worldPosition - splinePosition, right);
        var facing = lateral < 0f ? -right : right;
        var baseRotation = Quaternion.LookRotation(facing, Vector3.up);
        return baseRotation * Quaternion.Euler(0f, rotationY, 0f);
    }
}
