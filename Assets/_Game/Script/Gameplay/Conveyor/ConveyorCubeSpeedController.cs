using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorCubeSpeedController
{
    private const int GizmoStepCount = 24;
    private const float MarkerRadius = 0.08f;
    private static readonly Color AheadColor = new Color(1f, 0.85f, 0.1f, 1f);
    private static readonly Color BoostColor = new Color(0.1f, 0.9f, 1f, 1f);
    private static readonly Color AnchorColor = new Color(1f, 0.35f, 0.2f, 1f);
    private static readonly Color BehindLockColor = new Color(1f, 0.25f, 0.8f, 1f);
    private readonly List<DeliveryCubeState> _deliveryStates;
    private readonly ConveyorSpeedBoostConfigSO _boostConfig;

    public ConveyorCubeSpeedController(
        List<DeliveryCubeState> deliveryStates,
        ConveyorSpeedBoostConfigSO boostConfig)
    {
        _deliveryStates = deliveryStates;
        _boostConfig = boostConfig;
    }

    public void BoostCubesAheadFromProgress(float spawnProgress)
    {
        if (!CanBoostAhead()) return;
        foreach (var state in _deliveryStates)
            TryBoostCube(state, spawnProgress);
    }

    private bool CanBoostAhead()
    {
        return _boostConfig != null
               && _boostConfig.AheadExtraSpeed > 0f
               && _boostConfig.AheadBoostDistance > 0f
               && _boostConfig.AheadRange > 0f;
    }

    private void TryBoostCube(DeliveryCubeState state, float spawnProgress)
    {
        var cube = state?.Cube;
        if (cube == null) return;
        if (!IsAheadInRange(spawnProgress, cube.GetProgress())) return;
        cube.ApplyTemporarySpeedBoost(
            _boostConfig.AheadExtraSpeed,
            _boostConfig.AheadBoostDistance);
    }

    private bool IsAheadInRange(float spawnProgress, float targetProgress)
    {
        var delta = Mathf.Repeat(targetProgress - spawnProgress, 1f);
        return delta > 0f && delta <= _boostConfig.AheadRange;
    }

    public void LockCubesAroundSpawnTemporarily(float spawnProgress, int excludedUndoBatchId)
    {
        if (!CanLockAroundSpawn()) return;
        foreach (var state in _deliveryStates)
            TryLockCubeAroundSpawn(state, spawnProgress, excludedUndoBatchId);
    }

    private void TryLockCubeAroundSpawn(DeliveryCubeState state, float spawnProgress, int excludedUndoBatchId)
    {
        var cube = state?.Cube;
        if (cube == null) return;
        if (state.UndoBatchId == excludedUndoBatchId) return;
        if (!IsAroundSpawnInRange(GetSpawnLockCenterProgress(spawnProgress), cube.GetProgress())) return;
        cube.LockYAxisTemporarily(
            _boostConfig.PostUnloadLockYDuration,
            _boostConfig.PostUnloadFreezeSpeedMultiplier,
            _boostConfig.PostUnloadSlowDuration);
    }

    private bool CanLockAroundSpawn()
    {
        return _boostConfig != null
               && _boostConfig.PostUnloadFreezeAroundRange > 0f
               && _boostConfig.PostUnloadFreezeSpeedMultiplier >= 0f
               && _boostConfig.PostUnloadFreezeSpeedMultiplier <= 1f
               && (_boostConfig.PostUnloadLockYDuration > 0f || _boostConfig.PostUnloadSlowDuration > 0f);
    }

    private bool IsAroundSpawnInRange(float spawnProgress, float targetProgress)
    {
        var forwardDelta = Mathf.Repeat(targetProgress - spawnProgress, 1f);
        var backwardDelta = Mathf.Repeat(spawnProgress - targetProgress, 1f);
        var nearestDelta = Mathf.Min(forwardDelta, backwardDelta);
        return nearestDelta > 0f && nearestDelta <= _boostConfig.PostUnloadFreezeAroundRange;
    }

    private float GetSpawnLockCenterProgress(float spawnProgress)
    {
        return Mathf.Repeat(spawnProgress + _boostConfig.PostUnloadFreezeAroundOffset, 1f);
    }

    public void BoostCubesPassingCorners(IReadOnlyList<float> cornerProgresses)
    {
        if (!CanBoostCorners(cornerProgresses)) return;
        foreach (var state in _deliveryStates) TryBoostCubeAtCorner(state, cornerProgresses);
    }

    private bool CanBoostCorners(IReadOnlyList<float> cornerProgresses)
    {
        return _boostConfig != null
               && _boostConfig.CornerExtraSpeed > 0f
               && _boostConfig.CornerBoostDistance > 0f
               && cornerProgresses != null
               && cornerProgresses.Count > 0;
    }

    private void TryBoostCubeAtCorner(DeliveryCubeState state, IReadOnlyList<float> cornerProgresses)
    {
        var cube = state?.Cube;
        if (cube == null) return;
        var currentProgress = cube.GetProgress();
        var previousProgress = state.PreviousProgressCorner;
        if (DidPassCorner(previousProgress, currentProgress, cornerProgresses))
            ApplyCornerBoost(cube);
        state.PreviousProgressCorner = currentProgress;
    }

    private bool DidPassCorner(float previousProgress, float currentProgress, IReadOnlyList<float> cornerProgresses)
    {
        foreach (var cornerProgress in cornerProgresses)
            if (WasProgressPassed(previousProgress, currentProgress, cornerProgress)) return true;
        return false;
    }

    private bool WasProgressPassed(float previousProgress, float currentProgress, float targetProgress)
    {
        var travelDelta = Mathf.Repeat(currentProgress - previousProgress, 1f);
        if (travelDelta <= 0f) return false;
        var targetDelta = Mathf.Repeat(targetProgress - previousProgress, 1f);
        return targetDelta > 0f && targetDelta <= travelDelta;
    }

    private void ApplyCornerBoost(Cube cube)
    {
        cube.ApplyTemporarySpeedBoost(
            _boostConfig.CornerExtraSpeed,
            _boostConfig.CornerBoostDistance);
    }



    public void DrawPickupRanges(SplineContainer splineContainer, List<CarrierBase> carriers)
    {
        if (!CanDrawPickupRanges(splineContainer, carriers)) return;
        foreach (var carrier in carriers) DrawCarrierRanges(splineContainer, carrier);
    }

    private bool CanDrawPickupRanges(SplineContainer splineContainer, List<CarrierBase> carriers)
    {
        return _boostConfig != null
               && (CanDrawSpawnLockRange() || CanDrawBoostRanges())
               && splineContainer != null
               && splineContainer.Spline != null
               && splineContainer.Spline.Count > 0
               && carriers != null;
    }

    private void DrawCarrierRanges(SplineContainer splineContainer, CarrierBase carrier)
    {
        if (carrier == null) return;
        var progress = carrier.SplineProgress;
        var startPoint = GetSplinePoint(splineContainer, progress);
        DrawAnchor(startPoint, AnchorColor, 1.25f);
        if (CanDrawSpawnLockRange()) DrawSpawnLockRange(splineContainer, progress);
        if (CanDrawBoostRanges()) DrawBoostRanges(splineContainer, progress);
    }

    private bool CanDrawSpawnLockRange()
    {
        return _boostConfig != null
               && _boostConfig.DrawBehindLockRange
               && _boostConfig.PostUnloadFreezeAroundRange > 0f;
    }

    private bool CanDrawBoostRanges()
    {
        return _boostConfig != null
               && _boostConfig.DrawBoostRanges
               && _boostConfig.AheadRange > 0f
               && _boostConfig.AheadBoostDistance > 0f;
    }

    private void DrawSpawnLockRange(SplineContainer splineContainer, float progress)
    {
        var centerProgress = GetSpawnLockCenterProgress(progress);
        var centerPoint = GetSplinePoint(splineContainer, centerProgress);
        var rangeStartProgress = centerProgress - _boostConfig.PostUnloadFreezeAroundRange;
        var rangeEndProgress = centerProgress + _boostConfig.PostUnloadFreezeAroundRange;
        var rangeStartPoint = GetSplinePoint(splineContainer, rangeStartProgress);
        var rangeEndPoint = GetSplinePoint(splineContainer, rangeEndProgress);
        DrawRange(
            splineContainer,
            rangeStartProgress,
            _boostConfig.PostUnloadFreezeAroundRange * 2f,
            BehindLockColor);
        DrawAnchor(centerPoint, BehindLockColor, 1.15f);
        DrawAnchor(rangeStartPoint, BehindLockColor, 1f);
        DrawAnchor(rangeEndPoint, BehindLockColor, 1f);
    }

    private void DrawBoostRanges(SplineContainer splineContainer, float progress)
    {
        var aheadEndProgress = progress + _boostConfig.AheadRange;
        var aheadEndPoint = GetSplinePoint(splineContainer, aheadEndProgress);
        DrawRange(splineContainer, progress, _boostConfig.AheadRange, AheadColor);
        DrawAnchor(aheadEndPoint, AheadColor, 1f);
        DrawRange(
            splineContainer,
            aheadEndProgress,
            _boostConfig.AheadBoostDistance,
            BoostColor);
        DrawAnchor(
            GetSplinePoint(
                splineContainer,
                aheadEndProgress + _boostConfig.AheadBoostDistance),
            BoostColor,
            1f);
    }

    private void DrawRange(SplineContainer splineContainer, float startProgress, float length, Color color)
    {
        if (length <= 0f) return;
        Gizmos.color = color;
        var previous = GetSplinePoint(splineContainer, startProgress);
        for (var i = 1; i <= GizmoStepCount; i++)
        {
            var progress = startProgress + length * i / GizmoStepCount;
            var next = GetSplinePoint(splineContainer, progress);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private static void DrawAnchor(Vector3 position, Color color, float scale)
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(position, MarkerRadius * scale);
        Gizmos.DrawWireSphere(position, MarkerRadius * (scale + 0.35f));
    }

    private static Vector3 GetSplinePoint(SplineContainer splineContainer, float progress)
    {
        var point = splineContainer.Spline.EvaluatePosition(Mathf.Repeat(progress, 1f));
        return splineContainer.transform.TransformPoint(point);
    }
}
