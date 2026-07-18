using System.Collections;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEngine.Splines;
#endif

public class ConveyorManager : MonoBehaviour
{
    private const string PreviewHolderName = "EditorCarrierPreview";
#if UNITY_EDITOR
    [SerializeField] private SplineContainer conveyorContainer;
    [SerializeField] private SplineInstantiate splineInstantiate;
#endif
    [SerializeField] private ConveyorMeshBuilder conveyorMeshBuilder;
    [SerializeField] private ConveyorCornerDetector conveyorCornerDetector;
    [SerializeField] private ConveyorPortal conveyorPortalPrefab;
    [SerializeField] private Transform portalHolder;
    [SerializeField] private Transform conveyorRoot;

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

    private readonly ConveyorPathRuntime _path = new ConveyorPathRuntime();

    public ConveyorPathRuntime Path => _path;

    private void Awake()
    {
        CacheInstantiatedSegments();
        SetRevealProgress(1f);
    }

    public IEnumerator InitConveyor(SplinePathData splinePathData)
    {
        if (splinePathData == null) yield break;
        ClearPreviewHolder();
        SetupPath(splinePathData);

        if (conveyorMeshBuilder != null)
        {
            conveyorMeshBuilder.SetRevealProgress(0f);
        }

        if (conveyorMeshBuilder != null) conveyorMeshBuilder.Rebuild(_path);
        UpdatePortals(splinePathData);

        yield return null;
#if UNITY_EDITOR
        if (splineInstantiate != null)
        {
            splineInstantiate.UpdateInstances();
        }
#endif

        CacheInstantiatedSegments();
        SetRevealProgress(1f);
        var pathRoot = GetPathRoot();
        if (pathRoot != null) LunaMaterialUtility.NormalizeRenderers(pathRoot.gameObject);
    }

    private void ClearPreviewHolder()
    {
        var root = GetPathRoot();
        var holder = root != null ? root.Find(PreviewHolderName) : null;
        if (holder == null) return;
        Destroy(holder.gameObject);
    }

    private void SetupPath(SplinePathData splinePathData)
    {
        _path.Setup(splinePathData, GetPathRoot());
#if UNITY_EDITOR
        if (conveyorContainer != null)
        {
            var mapPoints = splinePathData.GetMapPointsInOrder();
            BuildBakedSpline(conveyorContainer.Spline, mapPoints, splinePathData.Closed);
        }
#endif
        if (conveyorCornerDetector != null) conveyorCornerDetector.UpdateCornerProgresses(_path, splinePathData.Closed);
    }

#if UNITY_EDITOR
    private static void BuildBakedSpline(Spline spline, List<SplinePointData> source, bool closed)
    {
        if (spline == null) return;
        spline.Clear();
        spline.Closed = closed;
        foreach (var point in source) spline.Add(CreateKnot(point));
        for (var i = 0; i < source.Count; i++) ApplySavedStyle(spline, source[i], i);
    }

    private static BezierKnot CreateKnot(SplinePointData point)
    {
        return new BezierKnot(ToSplinePosition(point), point.TangentInValue, point.TangentOutValue);
    }

    private static void ApplySavedStyle(Spline spline, SplinePointData point, int index)
    {
        var knot = spline[index];
        knot.TangentIn = point.TangentInValue;
        knot.TangentOut = point.TangentOutValue;
        knot.Rotation = quaternion.Euler(math.radians(point.Rotation));
        spline.SetKnot(index, knot);
    }
#endif

    private void CacheInstantiatedSegments()
    {
        _instantiatedSegments.Clear();
        var rootTransform = GetPathRoot();
        if (rootTransform == null || !_path.IsValid) return;

        Transform instancesRoot = null;
        foreach (Transform child in rootTransform)
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
            var localPos = _path.InverseTransformPoint(child.position);
            _path.GetNearestPointGlobal(localPos, out _, out var t, out _);
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

    private static Vector3 ToSplinePosition(SplinePointData pointData)
    {
        return new Vector3(pointData.GridPosition.x, 0f, pointData.GridPosition.y);
    }
    public void RebuildMesh()
    {
        conveyorMeshBuilder.Rebuild(_path);
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
        LunaMaterialUtility.NormalizeRenderers(_spawnedPortalA != null ? _spawnedPortalA.gameObject : null);
        LunaMaterialUtility.NormalizeRenderers(_spawnedPortalB != null ? _spawnedPortalB.gameObject : null);
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

        _spawnedPortalA.Setup(_path, _spawnedPortalB, false);
        _spawnedPortalB.Setup(_path, _spawnedPortalA, true);
    }

    private Vector3 GetPointWorldPosition(SplinePointData point)
    {
        return _path.TransformPoint(ToSplinePosition(point));
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

        var worldDirection = _path.TransformDirection(localDirection.normalized);
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

    private Transform GetPathRoot()
    {
#if UNITY_EDITOR
        if (conveyorContainer != null) return conveyorContainer.transform;
#endif
        return conveyorRoot != null ? conveyorRoot : transform;
    }

    public void SetRevealProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (conveyorMeshBuilder != null)
        {
            conveyorMeshBuilder.SetRevealProgress(progress);
        }

        // Recache nếu segment bị destroy/rebuild bởi hệ thống runtime
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
        SetRevealProgress(1f);
        yield break;
    }
}
