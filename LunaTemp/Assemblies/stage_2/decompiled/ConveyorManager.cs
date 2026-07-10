using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class ConveyorManager : MonoBehaviour
{
	private struct InstantiatedSegmentData
	{
		public Transform SegmentTransform;

		public Vector3 OriginalScale;

		public Transform ShadowChild;

		public float TStart;

		public float TEnd;
	}

	private const string PreviewHolderName = "EditorCarrierPreview";

	[SerializeField]
	private SplineContainer conveyorContainer;

	[SerializeField]
	private ConveyorMeshBuilder conveyorMeshBuilder;

	[SerializeField]
	private ConveyorCornerDetector conveyorCornerDetector;

	[SerializeField]
	private ConveyorPortal conveyorPortalPrefab;

	[SerializeField]
	private Transform portalHolder;

	[SerializeField]
	private SplineInstantiate splineInstantiate;

	private ConveyorPortal _spawnedPortalA;

	private ConveyorPortal _spawnedPortalB;

	private readonly List<InstantiatedSegmentData> _instantiatedSegments = new List<InstantiatedSegmentData>();

	private Tween _revealTween;

	private LevelEntryAnimConfigSO EntryConfig => (MonoSingleton<LevelManager>.Instance != null) ? MonoSingleton<LevelManager>.Instance.LevelEntryAnimConfig : null;

	private float RevealDelay => (EntryConfig != null) ? EntryConfig.ConveyorRevealDelay : 0.1f;

	private float RevealDuration => (EntryConfig != null) ? EntryConfig.ConveyorRevealDuration : 1f;

	private Ease RevealEase => (EntryConfig != null) ? EntryConfig.ConveyorRevealEase : Ease.OutCubic;

	private void Awake()
	{
		CacheInstantiatedSegments();
		SetRevealProgress(0f);
	}

	private void OnDestroy()
	{
		if (_revealTween != null)
		{
			_revealTween.Kill();
		}
	}

	public IEnumerator InitConveyor(SplinePathData splinePathData)
	{
		if (!(conveyorContainer == null) && splinePathData != null)
		{
			ClearPreviewHolder();
			SetupSpline(splinePathData);
			if (conveyorMeshBuilder != null)
			{
				conveyorMeshBuilder.SetRevealProgress(0f);
			}
			conveyorMeshBuilder.Rebuild(conveyorContainer);
			UpdatePortals(splinePathData);
			yield return null;
			if (splineInstantiate != null)
			{
				splineInstantiate.UpdateInstances();
			}
			CacheInstantiatedSegments();
			SetRevealProgress(0f);
		}
	}

	private void ClearPreviewHolder()
	{
		Transform holder = conveyorContainer.transform.Find("EditorCarrierPreview");
		if (!(holder == null))
		{
			Object.Destroy(holder.gameObject);
		}
	}

	private void SetupSpline(SplinePathData splinePathData)
	{
		List<SplinePointData> mapPoints = splinePathData.GetMapPointsInOrder();
		BuildBakedSpline(conveyorContainer.Spline, mapPoints, splinePathData.Closed);
		conveyorCornerDetector.UpdateCornerProgresses(conveyorContainer, splinePathData.Closed);
	}

	private void BuildBakedSpline(Spline spline, List<SplinePointData> source, bool closed)
	{
		spline.Clear();
		spline.Closed = closed;
		foreach (SplinePointData point in source)
		{
			spline.Add(CreateKnot(point));
		}
		for (int i = 0; i < source.Count; i++)
		{
			ApplySavedStyle(spline, source[i], i);
		}
	}

	private void CacheInstantiatedSegments()
	{
		_instantiatedSegments.Clear();
		if (splineInstantiate == null || conveyorContainer == null || conveyorContainer.Spline == null)
		{
			return;
		}
		Transform instancesRoot = null;
		foreach (Transform child in splineInstantiate.transform)
		{
			if (child.name.StartsWith("root-"))
			{
				instancesRoot = child;
				break;
			}
		}
		if (instancesRoot == null)
		{
			return;
		}
		List<(Transform, float)> segmentTempList = new List<(Transform, float)>();
		foreach (Transform child3 in instancesRoot)
		{
			Vector3 localPos = conveyorContainer.transform.InverseTransformPoint(child3.position);
			SplineUtility.GetNearestPoint(conveyorContainer.Spline, localPos, out var _, out var t);
			segmentTempList.Add((child3, Mathf.Repeat(t, 1f)));
		}
		segmentTempList.Sort(((Transform transform, float t) a, (Transform transform, float t) b) => a.t.CompareTo(b.t));
		int count = segmentTempList.Count;
		if (count == 0)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			(Transform, float) current = segmentTempList[i];
			float tStart;
			float tEnd;
			if (count == 1)
			{
				tStart = 0f;
				tEnd = 1f;
			}
			else if (i == 0)
			{
				tStart = 0f;
				tEnd = (current.Item2 + segmentTempList[1].Item2) * 0.5f;
			}
			else if (i == count - 1)
			{
				tStart = (current.Item2 + segmentTempList[i - 1].Item2) * 0.5f;
				tEnd = 1f;
			}
			else
			{
				tStart = (current.Item2 + segmentTempList[i - 1].Item2) * 0.5f;
				tEnd = (current.Item2 + segmentTempList[i + 1].Item2) * 0.5f;
			}
			Transform shadowChild = null;
			foreach (Transform child2 in current.Item1)
			{
				if (child2.name.Contains("Shadow"))
				{
					shadowChild = child2;
					break;
				}
			}
			Vector3 originalScale = current.Item1.localScale;
			current.Item1.localScale = Vector3.zero;
			if (shadowChild != null)
			{
				shadowChild.gameObject.SetActive(false);
			}
			_instantiatedSegments.Add(new InstantiatedSegmentData
			{
				SegmentTransform = current.Item1,
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
		BezierKnot knot = spline[index];
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
		List<SplinePointData> mapPoints = splinePathData.GetMapPointsInOrder();
		if (mapPoints.Count < 2 || conveyorPortalPrefab == null)
		{
			DespawnPortals();
			return;
		}
		if (_spawnedPortalA == null)
		{
			_spawnedPortalA = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(conveyorPortalPrefab, GetPortalHolder());
		}
		if (_spawnedPortalB == null)
		{
			_spawnedPortalB = MonoSingleton<PoolManagerNew>.Instance.PopFromPool(conveyorPortalPrefab, GetPortalHolder());
		}
		_spawnedPortalA.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		_spawnedPortalB.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		SetupPortalAtStart(_spawnedPortalA, mapPoints);
		SetupPortalAtEnd(_spawnedPortalB, mapPoints);
		LinkPortals();
	}

	private void SetupPortalAtStart(ConveyorPortal portal, List<SplinePointData> mapPoints)
	{
		if (!(portal == null) && mapPoints != null && mapPoints.Count >= 2)
		{
			SplinePointData start = mapPoints[0];
			SplinePointData next = mapPoints[1];
			portal.transform.SetPositionAndRotation(GetPointWorldPosition(start), GetPortalRotation(start, next));
		}
	}

	private void SetupPortalAtEnd(ConveyorPortal portal, List<SplinePointData> mapPoints)
	{
		if (!(portal == null) && mapPoints != null && mapPoints.Count >= 2)
		{
			SplinePointData previous = mapPoints[mapPoints.Count - 2];
			SplinePointData end = mapPoints[mapPoints.Count - 1];
			portal.transform.SetPositionAndRotation(GetPointWorldPosition(end), GetPortalRotation(end, previous));
		}
	}

	private void LinkPortals()
	{
		if (!(_spawnedPortalA == null) && !(_spawnedPortalB == null))
		{
			_spawnedPortalA.Setup(conveyorContainer, _spawnedPortalB, false);
			_spawnedPortalB.Setup(conveyorContainer, _spawnedPortalA, true);
		}
	}

	private Vector3 GetPointWorldPosition(SplinePointData point)
	{
		return conveyorContainer.transform.TransformPoint(ToSplinePosition(point));
	}

	private Quaternion GetPortalRotation(SplinePointData fromPoint, SplinePointData toPoint)
	{
		if (fromPoint == null || toPoint == null)
		{
			return Quaternion.LookRotation(base.transform.forward, Vector3.up);
		}
		Vector3 localDirection = ToSplinePosition(toPoint) - ToSplinePosition(fromPoint);
		if (localDirection.sqrMagnitude <= 0.001f)
		{
			localDirection = Vector3.forward;
		}
		Vector3 worldDirection = conveyorContainer.transform.TransformDirection(localDirection.normalized);
		return Quaternion.LookRotation(worldDirection, Vector3.up);
	}

	private void DespawnPortals()
	{
		if (_spawnedPortalA != null)
		{
			MonoSingleton<PoolManagerNew>.Instance.PushToPool(_spawnedPortalA);
			_spawnedPortalA = null;
		}
		if (_spawnedPortalB != null)
		{
			MonoSingleton<PoolManagerNew>.Instance.PushToPool(_spawnedPortalB);
			_spawnedPortalB = null;
		}
	}

	private Transform GetPortalHolder()
	{
		return (portalHolder != null) ? portalHolder : base.transform;
	}

	public void SetRevealProgress(float progress)
	{
		progress = Mathf.Clamp01(progress);
		if (conveyorMeshBuilder != null)
		{
			conveyorMeshBuilder.SetRevealProgress(progress);
		}
		bool needsRecache = _instantiatedSegments.Count == 0;
		if (!needsRecache)
		{
			foreach (InstantiatedSegmentData instantiatedSegment in _instantiatedSegments)
			{
				if (instantiatedSegment.SegmentTransform == null)
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
		foreach (InstantiatedSegmentData segment in _instantiatedSegments)
		{
			if (!(segment.SegmentTransform == null))
			{
				bool showSegment = progress > segment.TStart;
				segment.SegmentTransform.localScale = (showSegment ? segment.OriginalScale : Vector3.zero);
				if (segment.ShadowChild != null && segment.ShadowChild.gameObject.activeSelf != showSegment)
				{
					segment.ShadowChild.gameObject.SetActive(showSegment);
				}
			}
		}
	}

	public IEnumerator PlayRevealAnimation()
	{
		if (_revealTween != null)
		{
			_revealTween.Kill();
		}
		SetRevealProgress(0f);
		if (RevealDelay > 0f)
		{
			yield return new WaitForSeconds(RevealDelay);
		}
		_revealTween = DOTween.To(SetRevealProgress, 0f, 1f, RevealDuration).SetEase(RevealEase).SetUpdate(true)
			.SetTarget(this);
		float elapsed = 0f;
		float timeout = Mathf.Max(0.05f, RevealDuration + 0.25f);
		while (_revealTween != null && _revealTween.IsActive() && !_revealTween.IsComplete() && elapsed < timeout)
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
