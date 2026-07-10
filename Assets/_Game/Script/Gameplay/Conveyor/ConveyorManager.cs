using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;

public class ConveyorManager : MonoBehaviour
{
    private const string PreviewHolderName = "EditorCarrierPreview";
    [SerializeField] private SplineContainer conveyorContainer;
    [SerializeField] private ConveyorMeshBuilder conveyorMeshBuilder;
    [SerializeField] private ConveyorCornerDetector conveyorCornerDetector;
    [SerializeField] private ConveyorPortal conveyorPortalPrefab;
    [SerializeField] private Transform portalHolder;
    [SerializeField] private SplineInstantiate splineInstantiate;

    private ConveyorPortal _spawnedPortalA;
    private ConveyorPortal _spawnedPortalB;

    private struct InstantiatedSegmentData
    {
        public Transform SegmentTransform;
        public Vector3 OriginalScale;
        public Transform ShadowChild;
        public float TStart;
        public float TEnd;
    }

    private readonly List<InstantiatedSegmentData> _instantiatedSegments = new List<InstantiatedSegmentData>();

    private LevelEntryAnimConfigSO EntryConfig =>
        LevelManager.Instance != null ? LevelManager.Instance.LevelEntryAnimConfig : null;
    private float RevealDelay => EntryConfig != null ? EntryConfig.ConveyorRevealDelay : 0.1f;
    private float RevealDuration => EntryConfig != null ? EntryConfig.ConveyorRevealDuration : 1f;
    private DG.Tweening.Ease RevealEase => EntryConfig != null ? EntryConfig.ConveyorRevealEase : DG.Tweening.Ease.OutCubic;

    private Tween _revealTween;

    private void Awake()
    {
        CacheInstantiatedSegments();
        SetRevealProgress(0f);
    }

    private void OnDestroy()
    {
        if (_revealTween != null) _revealTween.Kill();
    }

    public IEnumerator InitConveyor(SplinePathData splinePathData)
    {
        if (conveyorContainer == null || splinePathData == null) yield break;
        ClearPreviewHolder();
        SetupSpline(splinePathData);

        if (conveyorMeshBuilder != null)
        {
            conveyorMeshBuilder.SetRevealProgress(0f);
        }

        conveyorMeshBuilder.Rebuild(conveyorContainer);
        UpdatePortals(splinePathData);

        // Wait 1 frame for spline cache to update before instantiating segments
        yield return null;
        if (splineInstantiate != null)
        {
            splineInstantiate.UpdateInstances();
        }

        CacheInstantiatedSegments();
        SetRevealProgress(0f);
    }

    private void ClearPreviewHolder()
    {
        var holder = conveyorContainer.transform.Find(PreviewHolderName);
        if (holder == null) return;
        Destroy(holder.gameObject);
    }

    private void SetupSpline(SplinePathData splinePathData)
    {
        var mapPoints = splinePathData.GetMapPointsInOrder();
        BuildBakedSpline(conveyorContainer.Spline, mapPoints, splinePathData.Closed);
        conveyorCornerDetector.UpdateCornerProgresses(conveyorContainer, splinePathData.Closed);
    }

    private void BuildBakedSpline(Spline spline, List<SplinePointData> source, bool closed)
    {
        spline.Clear();
        spline.Closed = closed;
        foreach (var point in source) spline.Add(CreateKnot(point));
        for (var i = 0; i < source.Count; i++) ApplySavedStyle(spline, source[i], i);
    }

    private void CacheInstantiatedSegments()
    {
        _instantiatedSegments.Clear();
        if (splineInstantiate == null || conveyorContainer == null || conveyorContainer.Spline == null) return;

        Transform instancesRoot = null;
        foreach (Transform child in splineInstantiate.transform)
        {
            if (child.name.StartsWith("root-"))
            {
                instancesRoot = child;
                break;
            }
        }

        if (instancesRoot == null) return;

        var segmentTempList = new List<(Transform transform, float t)>();
        foreach (Transform child in instancesRoot)
        {
            var localPos = conveyorContainer.transform.InverseTransformPoint(child.position);
            SplineUtility.GetNearestPoint(conveyorContainer.Spline, localPos, out _, out var t);
            segmentTempList.Add((child, Mathf.Repeat(t, 1f)));
        }

        segmentTempList.Sort((a, b) => a.t.CompareTo(b.t));

        int count = segmentTempList.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            var current = segmentTempList[i];
            float tStart, tEnd;

            if (count == 1)
            {
                tStart = 0f;
                tEnd = 1f;
            }
            else
            {
                if (i == 0)
                {
                    tStart = 0f;
                    tEnd = (current.t + segmentTempList[1].t) * 0.5f;
                }
                else if (i == count - 1)
                {
                    tStart = (current.t + segmentTempList[i - 1].t) * 0.5f;
                    tEnd = 1f;
                }
                else
                {
                    tStart = (current.t + segmentTempList[i - 1].t) * 0.5f;
                    tEnd = (current.t + segmentTempList[i + 1].t) * 0.5f;
                }
            }

            // Tìm shadow child (nếu có)
            Transform shadowChild = null;
            foreach (Transform child in current.transform)
            {
                if (child.name.Contains("Shadow"))
                {
                    shadowChild = child;
                    break;
                }
            }

            // Cache scale gốc, sau đó ẩn ngay bằng scale = zero
            var originalScale = current.transform.localScale;
            current.transform.localScale = Vector3.zero;
            if (shadowChild != null) shadowChild.gameObject.SetActive(false);

            _instantiatedSegments.Add(new InstantiatedSegmentData
            {
                SegmentTransform = current.transform,
                OriginalScale = originalScale,
                ShadowChild = shadowChild,
                TStart = tStart,
                TEnd = tEnd
            });
        }
    }

    private static BezierKnot CreateKnot(SplinePointData point)
    {
        return new BezierKnot(ToSplinePosition(point), point.TangentInValue, point.TangentOutValue);
    }

    private static void ApplySavedStyle(Spline spline, SplinePointData point, int index)
    {
        spline.SetTangentMode(index, point.TangentMode);
        var knot = spline[index];
        knot.TangentIn = point.TangentInValue;
        knot.TangentOut = point.TangentOutValue;
        knot.Rotation = quaternion.Euler(math.radians(point.Rotation));
        spline.SetKnot(index, knot);
    }

    private static Vector3 ToSplinePosition(SplinePointData pointData)
    {
        return new Vector3(pointData.GridPosition.x, 0f, pointData.GridPosition.y);
    }
    public void RebuildMesh()
    {
        conveyorMeshBuilder.Rebuild(conveyorContainer);
    }

    private void UpdatePortals(SplinePathData splinePathData)
    {
        if (splinePathData == null || splinePathData.Closed)
        {
            DespawnPortals();
            return;
        }

        var mapPoints = splinePathData.GetMapPointsInOrder();
        if (mapPoints.Count < 2 || conveyorPortalPrefab == null)
        {
            DespawnPortals();
            return;
        }

        if (_spawnedPortalA == null)
            _spawnedPortalA = PoolManagerNew.Instance.PopFromPool(conveyorPortalPrefab, GetPortalHolder());
        if (_spawnedPortalB == null)
            _spawnedPortalB = PoolManagerNew.Instance.PopFromPool(conveyorPortalPrefab, GetPortalHolder());
        _spawnedPortalA.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        _spawnedPortalB.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        SetupPortalAtStart(_spawnedPortalA, mapPoints);
        SetupPortalAtEnd(_spawnedPortalB, mapPoints);
        LinkPortals();
    }

    private void SetupPortalAtStart(ConveyorPortal portal, List<SplinePointData> mapPoints)
    {
        if (portal == null || mapPoints == null || mapPoints.Count < 2)
        {
            return;
        }

        var start = mapPoints[0];
        var next = mapPoints[1];
        portal.transform.SetPositionAndRotation(GetPointWorldPosition(start), GetPortalRotation(start, next));
    }

    private void SetupPortalAtEnd(ConveyorPortal portal, List<SplinePointData> mapPoints)
    {
        if (portal == null || mapPoints == null || mapPoints.Count < 2)
        {
            return;
        }

        var previous = mapPoints[mapPoints.Count - 2];
        var end = mapPoints[mapPoints.Count - 1];
        portal.transform.SetPositionAndRotation(GetPointWorldPosition(end), GetPortalRotation(end, previous));
    }

    private void LinkPortals()
    {
        if (_spawnedPortalA == null || _spawnedPortalB == null)
        {
            return;
        }

        _spawnedPortalA.Setup(conveyorContainer, _spawnedPortalB, false);
        _spawnedPortalB.Setup(conveyorContainer, _spawnedPortalA, true);
    }

    private Vector3 GetPointWorldPosition(SplinePointData point)
    {
        return conveyorContainer.transform.TransformPoint(ToSplinePosition(point));
    }

    private Quaternion GetPortalRotation(SplinePointData fromPoint, SplinePointData toPoint)
    {
        if (fromPoint == null || toPoint == null)
        {
            return Quaternion.LookRotation(transform.forward, Vector3.up);
        }

        var localDirection = ToSplinePosition(toPoint) - ToSplinePosition(fromPoint);
        if (localDirection.sqrMagnitude <= 0.001f)
        {
            localDirection = Vector3.forward;
        }

        var worldDirection = conveyorContainer.transform.TransformDirection(localDirection.normalized);
        return Quaternion.LookRotation(worldDirection, Vector3.up);
    }

    private void DespawnPortals()
    {
        if (_spawnedPortalA != null)
        {
            PoolManagerNew.Instance.PushToPool(_spawnedPortalA);
            _spawnedPortalA = null;
        }

        if (_spawnedPortalB != null)
        {
            PoolManagerNew.Instance.PushToPool(_spawnedPortalB);
            _spawnedPortalB = null;
        }
    }

    private Transform GetPortalHolder()
    {
        return portalHolder != null ? portalHolder : transform;
    }

    public void SetRevealProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (conveyorMeshBuilder != null)
        {
            conveyorMeshBuilder.SetRevealProgress(progress);
        }

        // Recache nếu segment bị destroy/rebuild bởi SplineInstantiate
        bool needsRecache = _instantiatedSegments.Count == 0;
        if (!needsRecache)
        {
            foreach (var segment in _instantiatedSegments)
            {
                if (segment.SegmentTransform == null)
                {
                    needsRecache = true;
                    break;
                }
            }
        }

        if (needsRecache)
        {
            CacheInstantiatedSegments();
        }

        foreach (var segment in _instantiatedSegments)
        {
            if (segment.SegmentTransform == null) continue;

            var showSegment = progress > segment.TStart;
            segment.SegmentTransform.localScale = showSegment ? segment.OriginalScale : Vector3.zero;

            if (segment.ShadowChild != null && segment.ShadowChild.gameObject.activeSelf != showSegment)
            {
                segment.ShadowChild.gameObject.SetActive(showSegment);
            }
        }
    }

    public IEnumerator PlayRevealAnimation()
    {
        if (_revealTween != null) _revealTween.Kill();
        SetRevealProgress(0f);
        if (RevealDelay > 0f)
        {
            yield return new WaitForSeconds(RevealDelay);
        }

        _revealTween = DOTween.To(SetRevealProgress, 0f, 1f, RevealDuration)
            .SetEase(RevealEase)
            .SetUpdate(true)
            .SetTarget(this);

        var elapsed = 0f;
        var timeout = Mathf.Max(0.05f, RevealDuration + 0.25f);
        while (_revealTween != null
               && _revealTween.IsActive()
               && !_revealTween.IsComplete()
               && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_revealTween != null && _revealTween.IsActive() && !_revealTween.IsComplete())
        {
            _revealTween.Kill();
        }

        _revealTween = null;
        SetRevealProgress(1f);
    }
}
