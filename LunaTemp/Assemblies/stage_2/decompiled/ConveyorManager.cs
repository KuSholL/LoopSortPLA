using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	private ConveyorMeshBuilder conveyorMeshBuilder;

	[SerializeField]
	private ConveyorCornerDetector conveyorCornerDetector;

	[SerializeField]
	private ConveyorPortal conveyorPortalPrefab;

	[SerializeField]
	private Transform portalHolder;

	[SerializeField]
	private Transform conveyorRoot;

	private ConveyorPortal _spawnedPortalA;

	private ConveyorPortal _spawnedPortalB;

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
		if (splinePathData != null)
		{
			ClearPreviewHolder();
			SetupPath(splinePathData);
			if (conveyorMeshBuilder != null)
			{
				conveyorMeshBuilder.SetRevealProgress(0f);
			}
			if (conveyorMeshBuilder != null)
			{
				conveyorMeshBuilder.Rebuild(_path);
			}
			UpdatePortals(splinePathData);
			yield return null;
			CacheInstantiatedSegments();
			SetRevealProgress(1f);
		}
	}

	private void ClearPreviewHolder()
	{
		Transform root = GetPathRoot();
		Transform holder = ((root != null) ? root.Find("EditorCarrierPreview") : null);
		if (!(holder == null))
		{
			Object.Destroy(holder.gameObject);
		}
	}

	private void SetupPath(SplinePathData splinePathData)
	{
		_path.Setup(splinePathData, GetPathRoot());
		if (conveyorCornerDetector != null)
		{
			conveyorCornerDetector.UpdateCornerProgresses(_path, splinePathData.Closed);
		}
	}

	private void CacheInstantiatedSegments()
	{
		_instantiatedSegments.Clear();
		Transform rootTransform = GetPathRoot();
		if (rootTransform == null || !_path.IsValid)
		{
			return;
		}
		Transform instancesRoot = null;
		foreach (Transform child2 in rootTransform)
		{
			if (child2.name.StartsWith("root-"))
			{
				instancesRoot = child2;
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
			Vector3 localPos = _path.InverseTransformPoint(child3.position);
			_path.GetNearestPointGlobal(localPos, out var _, out var t, out var _);
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
			foreach (Transform child in current.Item1)
			{
				if (child.name.Contains("Shadow"))
				{
					shadowChild = child;
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
		LunaMaterialUtility.NormalizeRenderers((_spawnedPortalA != null) ? _spawnedPortalA.gameObject : null);
		LunaMaterialUtility.NormalizeRenderers((_spawnedPortalB != null) ? _spawnedPortalB.gameObject : null);
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
			_spawnedPortalA.Setup(_path, _spawnedPortalB, false);
			_spawnedPortalB.Setup(_path, _spawnedPortalA, true);
		}
	}

	private Vector3 GetPointWorldPosition(SplinePointData point)
	{
		return _path.TransformPoint(ToSplinePosition(point));
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
		Vector3 worldDirection = _path.TransformDirection(localDirection.normalized);
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

	private Transform GetPathRoot()
	{
		return (conveyorRoot != null) ? conveyorRoot : base.transform;
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
		SetRevealProgress(1f);
		yield break;
	}
}
