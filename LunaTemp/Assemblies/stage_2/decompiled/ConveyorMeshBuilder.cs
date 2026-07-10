using System;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorMeshBuilder : MonoBehaviour
{
	[Header("Road Shape")]
	[SerializeField]
	[Min(0.5f)]
	private float roadWidth = 2.8f;

	[SerializeField]
	[Min(0.05f)]
	private float roadThickness = 0.2f;

	[SerializeField]
	[Min(8f)]
	private int sampleCount = 96;

	[Header("Road Visual")]
	[SerializeField]
	private Material roadMaterial;

	[SerializeField]
	private CubeMovementConfigSO cubeMovementConfig;

	[SerializeField]
	[Min(1f)]
	private int roadVisualSampleMultiplier = 4;

	[SerializeField]
	[Min(0f)]
	private float roadVisualSurfaceOffset = 0.01f;

	[SerializeField]
	[Min(0.1f)]
	private float shaderAcceleration = 5f;

	[Header("Side Rails")]
	[SerializeField]
	[Min(0f)]
	private float railWidth = 0.28f;

	[SerializeField]
	[Min(0f)]
	private float railHeight = 0.42f;

	[SerializeField]
	[Min(0f)]
	private float railCornerRadius = 0.12f;

	[SerializeField]
	[Range(1f, 8f)]
	private int railCornerSegments = 4;

	[SerializeField]
	[Range(0f, 1f)]
	private float railLightingFromTop = 0.85f;

	[Header("Collision")]
	[SerializeField]
	private bool generateRoadCollider = true;

	[SerializeField]
	[Min(1f)]
	private float roadColliderScaleMultiplier = 1.1f;

	private const string RoadObjectName = "GeneratedRoad";

	private const string RoadBaseObjectName = "GeneratedRoadBase";

	private const string RailObjectName = "GeneratedRails";

	private const string RailVisualObjectName = "GeneratedRailVisual";

	private const string RoadPlaneObjectName = "GeneratedRoadPlane";

	private const string RoadCubeObjectName = "GeneratedRoadCube";

	private const float RoadCubeHeight = 1f;

	private const float RoadCubeYPosition = 1.6f;

	private static readonly Vector3 RoadVisualOffset = new Vector3(0f, 0.05f, 0f);

	private static readonly int RoadLengthId = Shader.PropertyToID("_RoadLength");

	private static readonly int ClosedLoopId = Shader.PropertyToID("_ClosedLoop");

	private static readonly int FollowerOffsetId = Shader.PropertyToID("_FollowerOffset");

	private static readonly int FollowerSpacingId = Shader.PropertyToID("_FollowerSpacing");

	private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");

	private float _revealProgress = 1f;

	private bool _revealProgressDirty;

	private float _roadVisualLength;

	private float _currentScrollOffset;

	private float _currentShaderSpeed;

	private bool _isShaderSpeedInitialized;

	private float _spacingForWrap = 1f;

	private bool _isClosed;

	private Transform _roadChild;

	private MeshRenderer _roadMeshRenderer;

	private Material _roadRuntimeMaterial;

	private Material _lunaRoadBaseMaterial;

	private Material _lunaRailMaterial;

	public float GetUsableRoadHalfWidth(float edgePadding = 0f)
	{
		float usableHalfWidth = roadWidth * 0.5f - railWidth;
		return Mathf.Max(0f, usableHalfWidth - Mathf.Max(0f, edgePadding));
	}

	public float GetApproximateRoadLength(ConveyorPathRuntime container)
	{
		if (container == null || !container.IsValid)
		{
			return 0f;
		}
		bool isClosed = container.Closed;
		int ringCount = GetRingCount();
		Vector3 previousCenter = Vector3.zero;
		float accumulatedLength = 0f;
		Vector3 right;
		Vector3 up;
		Vector3 forward;
		for (int i = 0; i < ringCount; i++)
		{
			float t = GetSampleTime(i, ringCount, isClosed);
			SampleFrame(container, t, out var center, out right, out up, out forward);
			if (i > 0)
			{
				accumulatedLength += Vector3.Distance(previousCenter, center);
			}
			previousCenter = center;
		}
		if (isClosed && ringCount > 1)
		{
			SampleFrame(container, 0f, out var firstCenter, out forward, out up, out right);
			accumulatedLength += Vector3.Distance(previousCenter, firstCenter);
		}
		return accumulatedLength;
	}

	public float GetProjectedRoadArea(ConveyorPathRuntime container)
	{
		if (container == null || !container.IsValid)
		{
			return 0f;
		}
		Mesh roadMesh = BuildRoadVisualMesh(container);
		float area = CalculateProjectedMeshArea(roadMesh);
		DestroyObject(roadMesh);
		return area;
	}

	[ContextMenu("Rebuild")]
	public void Rebuild(ConveyorPathRuntime splineContainer)
	{
		if (splineContainer != null && splineContainer.IsValid)
		{
			_roadChild = null;
			_roadMeshRenderer = null;
			EnsureRoadRoot(out var roadObject);
			UpdateRoadVisual(splineContainer, roadObject);
			UpdateLunaSolidRoadVisual(splineContainer);
			RemoveLunaGeneratedPhysics(roadObject.transform);
		}
	}

	private Mesh BuildRoadMesh(ConveyorPathRuntime container)
	{
		bool isClosed = container.Closed;
		int ringCount = GetRingCount();
		int segmentCount = (isClosed ? ringCount : (ringCount - 1));
		int vertsPerRing = 4;
		Vector3[] vertices = new Vector3[ringCount * vertsPerRing];
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		int[] triangles = new int[segmentCount * 24];
		float halfWidth = roadWidth * 0.5f;
		float halfThickness = roadThickness * 0.5f;
		float accumulatedLength = 0f;
		Vector3 previousCenter = Vector3.zero;
		for (int j = 0; j < ringCount; j++)
		{
			float t = GetSampleTime(j, ringCount, isClosed);
			SampleFrame(container, t, out var center, out var _, out var up, out var right);
			Vector3 topLeft = center - right * halfWidth + up * halfThickness;
			Vector3 topRight = center + right * halfWidth + up * halfThickness;
			Vector3 bottomLeft = center - right * halfWidth - up * halfThickness;
			Vector3 bottomRight = center + right * halfWidth - up * halfThickness;
			int baseIndex = j * vertsPerRing;
			vertices[baseIndex] = base.transform.InverseTransformPoint(topLeft);
			vertices[baseIndex + 1] = base.transform.InverseTransformPoint(topRight);
			vertices[baseIndex + 2] = base.transform.InverseTransformPoint(bottomLeft);
			vertices[baseIndex + 3] = base.transform.InverseTransformPoint(bottomRight);
			normals[baseIndex] = base.transform.InverseTransformDirection(up);
			normals[baseIndex + 1] = base.transform.InverseTransformDirection(up);
			normals[baseIndex + 2] = base.transform.InverseTransformDirection(-up);
			normals[baseIndex + 3] = base.transform.InverseTransformDirection(-up);
			if (j > 0)
			{
				accumulatedLength += Vector3.Distance(previousCenter, center);
			}
			float v = accumulatedLength;
			uvs[baseIndex] = new Vector2(0f, v);
			uvs[baseIndex + 1] = new Vector2(1f, v);
			uvs[baseIndex + 2] = new Vector2(0f, v);
			uvs[baseIndex + 3] = new Vector2(1f, v);
			previousCenter = center;
		}
		int triangleIndex = 0;
		for (int i = 0; i < segmentCount; i++)
		{
			int current = i * vertsPerRing;
			int next = (i + 1) % ringCount * vertsPerRing;
			AddQuad(triangles, ref triangleIndex, current, current + 1, next + 1, next);
			AddQuad(triangles, ref triangleIndex, current + 2, next + 2, next + 3, current + 3);
			AddQuad(triangles, ref triangleIndex, current, next, next + 2, current + 2);
			AddQuad(triangles, ref triangleIndex, current + 3, next + 3, next + 1, current + 1);
		}
		Mesh mesh = new Mesh();
		mesh.name = "SplineRoadMesh";
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		return mesh;
	}

	private Mesh BuildRailMesh(ConveyorPathRuntime container)
	{
		if (railWidth <= 0f || railHeight <= 0f)
		{
			return new Mesh
			{
				name = "EmptyRailMesh"
			};
		}
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		BuildSingleRail(container, -1f, vertices, normals, uvs, triangles);
		BuildSingleRail(container, 1f, vertices, normals, uvs, triangles);
		Mesh mesh = new Mesh();
		mesh.name = "SplineRailMesh";
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetUVs(0, uvs);
		mesh.SetTriangles(triangles, 0);
		mesh.RecalculateBounds();
		return mesh;
	}

	private void BuildSingleRail(ConveyorPathRuntime container, float sideSign, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
	{
		bool isClosed = container.Closed;
		int ringCount = GetRingCount();
		int segmentCount = (isClosed ? ringCount : (ringCount - 1));
		List<Vector2> profilePoints = new List<Vector2>();
		List<Vector2> profileNormals = new List<Vector2>();
		BuildRoundedRailProfile(profilePoints, profileNormals);
		int vertsPerRing = profilePoints.Count;
		int startVertex = vertices.Count;
		float halfWidth = roadWidth * 0.5f;
		float accumulatedLength = 0f;
		Vector3 previousCenter = Vector3.zero;
		float profileLength = CalculateProfileLength(profilePoints);
		for (int j = 0; j < ringCount; j++)
		{
			float t = GetSampleTime(j, ringCount, isClosed);
			SampleFrame(container, t, out var center, out var forward, out var up, out var right);
			Vector3 railCenter = center + right * sideSign * (halfWidth - railWidth * 0.5f);
			Vector3 side = right * sideSign;
			if (j > 0)
			{
				accumulatedLength += Vector3.Distance(previousCenter, railCenter);
			}
			for (int p = 0; p < vertsPerRing; p++)
			{
				Vector2 profilePoint = profilePoints[p];
				Vector2 profileNormal = profileNormals[p];
				Vector3 worldPoint = railCenter + side * profilePoint.x + up * profilePoint.y;
				Vector3 profileWorldNormal = (side * profileNormal.x + up * profileNormal.y).normalized;
				forward = Vector3.Slerp(profileWorldNormal, up, railLightingFromTop);
				Vector3 worldNormal = forward.normalized;
				vertices.Add(base.transform.InverseTransformPoint(worldPoint));
				normals.Add(base.transform.InverseTransformDirection(worldNormal));
				uvs.Add(new Vector2(ProfileDistanceAt(profilePoints, p) / profileLength, accumulatedLength));
			}
			previousCenter = railCenter;
		}
		for (int i = 0; i < segmentCount; i++)
		{
			int current = startVertex + i * vertsPerRing;
			int next = startVertex + (i + 1) % ringCount * vertsPerRing;
			for (int p2 = 0; p2 < vertsPerRing; p2++)
			{
				int profileNext = (p2 + 1) % vertsPerRing;
				AddRailQuad(triangles, sideSign, current + p2, next + p2, next + profileNext, current + profileNext);
			}
		}
	}

	private int GetRingCount()
	{
		return Mathf.Max(2, sampleCount);
	}

	private static float GetSampleTime(int index, int ringCount, bool isClosed)
	{
		if (ringCount <= 1)
		{
			return 0f;
		}
		return isClosed ? ((float)index / (float)ringCount) : ((float)index / (float)(ringCount - 1));
	}

	private void BuildRoundedRailProfile(List<Vector2> profilePoints, List<Vector2> profileNormals)
	{
		profilePoints.Clear();
		profileNormals.Clear();
		float halfRailWidth = railWidth * 0.5f;
		float radius = Mathf.Min(railCornerRadius, halfRailWidth, railHeight * 0.5f);
		if (radius <= 0.0001f)
		{
			AddProfilePoint(profilePoints, profileNormals, new Vector2(0f - halfRailWidth, 0f), new Vector2(-1f, 0f));
			AddProfilePoint(profilePoints, profileNormals, new Vector2(0f - halfRailWidth, railHeight), new Vector2(-1f, 0f));
			AddProfilePoint(profilePoints, profileNormals, new Vector2(halfRailWidth, railHeight), new Vector2(1f, 0f));
			AddProfilePoint(profilePoints, profileNormals, new Vector2(halfRailWidth, 0f), new Vector2(1f, 0f));
			return;
		}
		int segments = Mathf.Max(1, railCornerSegments);
		Vector2 bottomLeftCenter = new Vector2(0f - halfRailWidth + radius, radius);
		Vector2 topLeftCenter = new Vector2(0f - halfRailWidth + radius, railHeight - radius);
		Vector2 topRightCenter = new Vector2(halfRailWidth - radius, railHeight - radius);
		Vector2 bottomRightCenter = new Vector2(halfRailWidth - radius, radius);
		AddArc(profilePoints, profileNormals, bottomLeftCenter, radius, 270f, 180f, segments, true);
		AddArc(profilePoints, profileNormals, topLeftCenter, radius, 180f, 90f, segments, false);
		AddArc(profilePoints, profileNormals, topRightCenter, radius, 90f, 0f, segments, false);
		AddArc(profilePoints, profileNormals, bottomRightCenter, radius, 0f, -90f, segments, false);
	}

	private static void AddArc(List<Vector2> profilePoints, List<Vector2> profileNormals, Vector2 center, float radius, float startAngleDeg, float endAngleDeg, int segments, bool includeStart)
	{
		int startStep = ((!includeStart) ? 1 : 0);
		for (int step = startStep; step <= segments; step++)
		{
			float t = (float)step / (float)segments;
			float angle = Mathf.Lerp(startAngleDeg, endAngleDeg, t) * (3.14159265f / 180f);
			Vector2 normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			AddProfilePoint(profilePoints, profileNormals, center + normal * radius, normal);
		}
	}

	private static void AddProfilePoint(List<Vector2> profilePoints, List<Vector2> profileNormals, Vector2 point, Vector2 normal)
	{
		if (profilePoints.Count <= 0 || !(Vector2.Distance(profilePoints[profilePoints.Count - 1], point) < 0.0001f))
		{
			profilePoints.Add(point);
			profileNormals.Add(normal.normalized);
		}
	}

	private static float CalculateProfileLength(List<Vector2> profilePoints)
	{
		float length = 0f;
		for (int i = 0; i < profilePoints.Count; i++)
		{
			int next = (i + 1) % profilePoints.Count;
			length += Vector2.Distance(profilePoints[i], profilePoints[next]);
		}
		return Mathf.Max(length, 0.0001f);
	}

	private static float ProfileDistanceAt(List<Vector2> profilePoints, int pointIndex)
	{
		float distance = 0f;
		for (int i = 0; i < pointIndex; i++)
		{
			distance += Vector2.Distance(profilePoints[i], profilePoints[i + 1]);
		}
		return distance;
	}

	private void SampleFrame(ConveyorPathRuntime container, float t, out Vector3 center, out Vector3 forward, out Vector3 up, out Vector3 right)
	{
		container.Evaluate(t, out var pos, out var tangent, out var upVector);
		center = container.TransformPoint(pos);
		forward = container.TransformDirection(tangent).normalized;
		up = container.TransformDirection(upVector).normalized;
		if (forward.sqrMagnitude < 0.0001f)
		{
			forward = Vector3.forward;
		}
		if (up.sqrMagnitude < 0.0001f)
		{
			up = Vector3.up;
		}
		right = Vector3.Cross(up, forward).normalized;
		if (right.sqrMagnitude < 0.0001f)
		{
			right = Vector3.right;
		}
	}

	private void EnsureRoadRoot(out GameObject roadObject)
	{
		Transform childTransform = base.transform.Find("GeneratedRoad");
		roadObject = ((childTransform == null) ? new GameObject("GeneratedRoad") : childTransform.gameObject);
		roadObject.transform.SetParent(base.transform, false);
		roadObject.transform.localPosition = RoadVisualOffset;
	}

	private void UpdateRoadVisual(ConveyorPathRuntime splineContainer, GameObject roadObject)
	{
		Mesh roadMesh = BuildRoadVisualMesh(splineContainer);
		EnsureRoadComponents(roadObject, out var meshFilter, out var meshRenderer);
		ReplaceMesh(meshFilter, roadMesh);
		ApplyRoadMaterial(meshRenderer);
		ApplyRoadShaderData(meshRenderer, splineContainer);
	}

	private void UpdateLunaSolidRoadVisual(ConveyorPathRuntime splineContainer)
	{
		EnsureGeneratedMeshVisual("GeneratedRoadBase", BuildRoadMesh(splineContainer), ref _lunaRoadBaseMaterial, new Color(0.42f, 0.42f, 0.58f, 1f), "LunaRoadBaseMaterial", new Vector3(0f, -0.08f, 0f), roadMaterial);
		EnsureGeneratedMeshVisual("GeneratedRailVisual", BuildRailMesh(splineContainer), ref _lunaRailMaterial, new Color(0.77f, 0.78f, 0.96f, 1f), "LunaRailMaterial", Vector3.zero);
	}

	private void EnsureGeneratedMeshVisual(string objectName, Mesh mesh, ref Material material, Color color, string materialName, Vector3 localOffset, Material sourceMaterial = null)
	{
		Transform childTransform = base.transform.Find(objectName);
		GameObject visualObject = ((childTransform == null) ? new GameObject(objectName) : childTransform.gameObject);
		visualObject.transform.SetParent(base.transform, false);
		visualObject.transform.localPosition = localOffset;
		visualObject.transform.localRotation = Quaternion.identity;
		visualObject.transform.localScale = Vector3.one;
		MeshFilter meshFilter = visualObject.GetComponent<MeshFilter>();
		if (meshFilter == null)
		{
			meshFilter = visualObject.AddComponent<MeshFilter>();
		}
		MeshRenderer meshRenderer = visualObject.GetComponent<MeshRenderer>();
		if (meshRenderer == null)
		{
			meshRenderer = visualObject.AddComponent<MeshRenderer>();
		}
		ReplaceMesh(meshFilter, mesh);
		if (material == null)
		{
			material = ((sourceMaterial != null) ? LunaMaterialUtility.CreateRuntimeMaterial(sourceMaterial, color) : LunaMaterialUtility.CreateRuntimeMaterial(color, materialName));
		}
		meshRenderer.sharedMaterial = material;
	}

	private void ApplyRoadShaderData(MeshRenderer meshRenderer, ConveyorPathRuntime splineContainer)
	{
		if (meshRenderer == null || splineContainer == null)
		{
			return;
		}
		SetRoadFloat(RoadLengthId, _roadVisualLength);
		SetRoadFloat(ClosedLoopId, splineContainer.Closed ? 1f : 0f);
		SetRoadFloat(RevealProgressId, _revealProgress);
		_isClosed = splineContainer.Closed;
		if (_roadChild == null)
		{
			_roadChild = base.transform.Find("GeneratedRoad");
		}
		if (_roadMeshRenderer == null && _roadChild != null)
		{
			_roadMeshRenderer = _roadChild.GetComponent<MeshRenderer>();
		}
		if (_roadMeshRenderer != null && _roadMeshRenderer.sharedMaterial != null && _roadMeshRenderer.sharedMaterial.HasProperty(FollowerSpacingId))
		{
			float spacing = _roadMeshRenderer.sharedMaterial.GetFloat(FollowerSpacingId);
			spacing = Mathf.Max(spacing, 0.01f);
			if (_isClosed)
			{
				float repeatCount = Mathf.Max(1f, Mathf.Round(Mathf.Max(_roadVisualLength, spacing) / spacing));
				_spacingForWrap = Mathf.Max(_roadVisualLength / repeatCount, 0.01f);
			}
			else
			{
				_spacingForWrap = spacing;
			}
		}
	}

	public void SetRevealProgress(float progress)
	{
		_revealProgress = Mathf.Clamp01(progress);
		_revealProgressDirty = true;
		UpdateShaderProperties();
	}

	private void UpdateShaderProperties()
	{
		if (_roadChild == null)
		{
			_roadChild = base.transform.Find("GeneratedRoad");
		}
		if (_roadMeshRenderer == null && _roadChild != null)
		{
			_roadMeshRenderer = _roadChild.GetComponent<MeshRenderer>();
		}
		if ((bool)_roadMeshRenderer)
		{
			SetRoadFloat(RevealProgressId, _revealProgress);
			_revealProgressDirty = false;
		}
	}

	private Mesh BuildRoadVisualMesh(ConveyorPathRuntime container)
	{
		bool isClosed = container.Closed;
		int sampleRingCount = GetRoadVisualRingCount();
		int ringCount = (isClosed ? (sampleRingCount + 1) : sampleRingCount);
		int segmentCount = ringCount - 1;
		Vector3[] vertices = new Vector3[ringCount * 2];
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		int[] triangles = new int[segmentCount * 6];
		float halfWidth = roadWidth * 0.5f;
		float accumulatedLength = 0f;
		Vector3 previousCenter = Vector3.zero;
		for (int i = 0; i < ringCount; i++)
		{
			float sampleTime = GetRoadVisualSampleTime(i, sampleRingCount, isClosed);
			FillRoadVisualRing(container, sampleTime, i, i > 0, halfWidth, ref accumulatedLength, ref previousCenter, vertices, normals, uvs);
		}
		_roadVisualLength = accumulatedLength;
		FillRoadVisualTriangles(ringCount, segmentCount, triangles);
		return CreateRoadVisualMesh(vertices, normals, uvs, triangles);
	}

	private int GetRoadVisualRingCount()
	{
		return Mathf.Max(2, GetRingCount() * Mathf.Max(1, roadVisualSampleMultiplier));
	}

	private static float GetRoadVisualSampleTime(int index, int sampleRingCount, bool isClosed)
	{
		if (!isClosed)
		{
			return GetSampleTime(index, sampleRingCount, false);
		}
		if (index >= sampleRingCount)
		{
			return 1f;
		}
		return (float)index / (float)sampleRingCount;
	}

	private void FillRoadVisualRing(ConveyorPathRuntime container, float sampleTime, int ringIndex, bool hasPrevious, float halfWidth, ref float accumulatedLength, ref Vector3 previousCenter, Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
	{
		SampleFrame(container, sampleTime, out var center, out var _, out var up, out var right);
		if (hasPrevious)
		{
			accumulatedLength += Vector3.Distance(previousCenter, center);
		}
		center += up * roadVisualSurfaceOffset;
		Vector3 left = center - right * halfWidth;
		Vector3 roadRight = center + right * halfWidth;
		int baseIndex = ringIndex * 2;
		vertices[baseIndex] = base.transform.InverseTransformPoint(left);
		vertices[baseIndex + 1] = base.transform.InverseTransformPoint(roadRight);
		normals[baseIndex] = base.transform.InverseTransformDirection(up);
		normals[baseIndex + 1] = base.transform.InverseTransformDirection(up);
		uvs[baseIndex] = new Vector2(0f, accumulatedLength);
		uvs[baseIndex + 1] = new Vector2(1f, accumulatedLength);
		previousCenter = center;
	}

	private static void FillRoadVisualTriangles(int ringCount, int segmentCount, int[] triangles)
	{
		int triangleIndex = 0;
		for (int i = 0; i < segmentCount; i++)
		{
			int current = i * 2;
			int next = (i + 1) * 2;
			AddQuad(triangles, ref triangleIndex, current, current + 1, next + 1, next);
		}
	}

	private static Mesh CreateRoadVisualMesh(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
	{
		Mesh mesh = new Mesh
		{
			name = "SplineRoadVisualMesh"
		};
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		return mesh;
	}

	private static float CalculateProjectedMeshArea(Mesh mesh)
	{
		float area = 0f;
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3)
		{
			area += CalculateProjectedTriangleArea(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
		}
		return area;
	}

	private static float CalculateProjectedTriangleArea(Vector3 a, Vector3 b, Vector3 c)
	{
		Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
		Vector2 ac = new Vector2(c.x - a.x, c.z - a.z);
		return Mathf.Abs(ab.x * ac.y - ab.y * ac.x) * 0.5f;
	}

	private void EnsureRoadComponents(GameObject roadObject, out MeshFilter meshFilter, out MeshRenderer meshRenderer)
	{
		meshFilter = roadObject.GetComponent<MeshFilter>();
		meshRenderer = roadObject.GetComponent<MeshRenderer>();
		if (meshFilter == null)
		{
			meshFilter = roadObject.AddComponent<MeshFilter>();
		}
		if (meshRenderer == null)
		{
			meshRenderer = roadObject.AddComponent<MeshRenderer>();
		}
	}

	private void ReplaceMesh(MeshFilter meshFilter, Mesh nextMesh)
	{
		if (meshFilter.sharedMesh != null)
		{
			DestroyObject(meshFilter.sharedMesh);
		}
		meshFilter.sharedMesh = nextMesh;
	}

	private void ApplyRoadMaterial(MeshRenderer meshRenderer)
	{
		if (roadMaterial != null)
		{
			if (Application.isPlaying)
			{
				if (_roadRuntimeMaterial != null && _roadRuntimeMaterial != roadMaterial)
				{
					DestroyObject(_roadRuntimeMaterial);
				}
				_roadRuntimeMaterial = new Material(roadMaterial)
				{
					name = roadMaterial.name + "_Runtime"
				};
				meshRenderer.sharedMaterial = _roadRuntimeMaterial;
			}
			else
			{
				meshRenderer.sharedMaterial = roadMaterial;
				_roadRuntimeMaterial = roadMaterial;
			}
		}
		else if (meshRenderer.sharedMaterial != null)
		{
			_roadRuntimeMaterial = meshRenderer.sharedMaterial;
		}
		else
		{
			Shader shader = Shader.Find("Custom/SplineFollowerRoad");
			if (!(shader == null))
			{
				meshRenderer.sharedMaterial = new Material(shader)
				{
					name = "GeneratedRoadMaterial_Runtime"
				};
				_roadRuntimeMaterial = meshRenderer.sharedMaterial;
			}
		}
	}

	private void UpdateRoadPlaneCollider(ConveyorPathRuntime splineContainer, Transform roadRoot)
	{
		if (!generateRoadCollider)
		{
			DestroyRoadPlane(roadRoot);
			return;
		}
		Mesh roadMesh = BuildRoadMesh(splineContainer);
		SetupRoadPlaneCollider(roadRoot, roadMesh);
		DestroyObject(roadMesh);
	}

	private void EnsureRailColliderObject(out GameObject railObject, out MeshCollider railCollider)
	{
		Transform childTransform = base.transform.Find("GeneratedRails");
		railObject = ((childTransform == null) ? new GameObject("GeneratedRails") : childTransform.gameObject);
		railObject.transform.SetParent(base.transform, false);
		railCollider = railObject.GetComponent<MeshCollider>();
		if (railCollider == null)
		{
			railCollider = railObject.AddComponent<MeshCollider>();
		}
	}

	private void RemoveRenderComponents(GameObject target)
	{
		if (!(target == null))
		{
			MeshFilter meshFilter = target.GetComponent<MeshFilter>();
			MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
			if (meshFilter != null && meshFilter.sharedMesh != null)
			{
				Mesh previousMesh = meshFilter.sharedMesh;
				meshFilter.sharedMesh = null;
				DestroyObject(previousMesh);
			}
			if (meshFilter != null)
			{
				DestroyObject(meshFilter);
			}
			if (meshRenderer != null)
			{
				DestroyObject(meshRenderer);
			}
		}
	}

	private void ClearRailColliderMesh(MeshCollider meshCollider)
	{
		if (!(meshCollider == null) && !(meshCollider.sharedMesh == null))
		{
			Mesh previousMesh = meshCollider.sharedMesh;
			meshCollider.sharedMesh = null;
			DestroyObject(previousMesh);
		}
	}

	private void RemoveLunaGeneratedPhysics(Transform roadRoot)
	{
		DestroyRoadPlane(roadRoot);
		Transform railTransform = base.transform.Find("GeneratedRails");
		if (!(railTransform == null))
		{
			MeshCollider railCollider = railTransform.GetComponent<MeshCollider>();
			if (railCollider != null)
			{
				ClearRailColliderMesh(railCollider);
				DestroyObject(railCollider);
			}
			RemoveRenderComponents(railTransform.gameObject);
		}
	}

	private void SetupRoadPlaneCollider(Transform roadRoot, Mesh roadMesh)
	{
		MeshCollider roadCollider = roadRoot.GetComponent<MeshCollider>();
		if (roadCollider != null)
		{
			DestroyObject(roadCollider);
		}
		GameObject plane = GetOrCreateRoadPlane(roadRoot);
		Bounds bounds = roadMesh.bounds;
		Vector3 center = roadRoot.TransformPoint(bounds.center);
		plane.transform.SetPositionAndRotation(center, Quaternion.identity);
		plane.transform.localScale = GetRoadPlaneScale(bounds.size);
		SetupRoadCube(roadRoot, plane.transform.localPosition, bounds.size);
	}

	private GameObject GetOrCreateRoadPlane(Transform roadRoot)
	{
		Transform plane = roadRoot.Find("GeneratedRoadPlane");
		if (plane != null)
		{
			return plane.gameObject;
		}
		GameObject planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
		planeObject.name = "GeneratedRoadPlane";
		planeObject.transform.SetParent(roadRoot, true);
		MeshRenderer renderer = planeObject.GetComponent<MeshRenderer>();
		if (renderer != null)
		{
			renderer.enabled = false;
		}
		return planeObject;
	}

	private Vector3 GetRoadPlaneScale(Vector3 roadSize)
	{
		Vector3 scaledSize = roadSize * roadColliderScaleMultiplier;
		return new Vector3(scaledSize.x / 10f, 1f, scaledSize.z / 10f);
	}

	private void SetupRoadCube(Transform roadRoot, Vector3 planeLocalPosition, Vector3 roadSize)
	{
		GameObject cube = GetOrCreateRoadCube(roadRoot);
		cube.transform.localPosition = new Vector3(planeLocalPosition.x, 1.6f, planeLocalPosition.z);
		cube.transform.localRotation = Quaternion.identity;
		cube.transform.localScale = GetRoadCubeScale(roadSize);
	}

	private GameObject GetOrCreateRoadCube(Transform roadRoot)
	{
		Transform cube = roadRoot.Find("GeneratedRoadCube");
		if (cube != null)
		{
			return cube.gameObject;
		}
		GameObject cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cubeObject.name = "GeneratedRoadCube";
		cubeObject.transform.SetParent(roadRoot, false);
		MeshRenderer renderer = cubeObject.GetComponent<MeshRenderer>();
		if (renderer != null)
		{
			renderer.enabled = false;
		}
		return cubeObject;
	}

	private Vector3 GetRoadCubeScale(Vector3 roadSize)
	{
		Vector3 scaledSize = roadSize * roadColliderScaleMultiplier;
		return new Vector3(scaledSize.x, 1f, scaledSize.z);
	}

	private void DestroyRoadPlane(Transform roadRoot)
	{
		Transform plane = roadRoot.Find("GeneratedRoadPlane");
		if (plane != null)
		{
			DestroyObject(plane.gameObject);
		}
		Transform cube = roadRoot.Find("GeneratedRoadCube");
		if (cube != null)
		{
			DestroyObject(cube.gameObject);
		}
	}

	private new void DestroyObject(UnityEngine.Object target)
	{
		if (!(target == null))
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(target);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(target);
			}
		}
	}

	private static void AddQuad(IList<int> triangles, int a, int b, int c, int d)
	{
		triangles.Add(a);
		triangles.Add(b);
		triangles.Add(c);
		triangles.Add(a);
		triangles.Add(c);
		triangles.Add(d);
	}

	private static void AddRailQuad(IList<int> triangles, float sideSign, int a, int b, int c, int d)
	{
		if (sideSign < 0f)
		{
			AddQuad(triangles, d, c, b, a);
		}
		else
		{
			AddQuad(triangles, a, b, c, d);
		}
	}

	private static void AddQuad(int[] triangles, ref int triangleIndex, int a, int b, int c, int d)
	{
		triangles[triangleIndex++] = a;
		triangles[triangleIndex++] = b;
		triangles[triangleIndex++] = c;
		triangles[triangleIndex++] = a;
		triangles[triangleIndex++] = c;
		triangles[triangleIndex++] = d;
	}

	private void OnDisable()
	{
		_isShaderSpeedInitialized = false;
	}

	private void Update()
	{
		if (_roadChild == null)
		{
			_roadChild = base.transform.Find("GeneratedRoad");
		}
		if (_roadMeshRenderer == null && _roadChild != null)
		{
			_roadMeshRenderer = _roadChild.GetComponent<MeshRenderer>();
		}
		if ((bool)_roadMeshRenderer)
		{
			float targetShaderSpeed = (MonoSingleton<ConveyorDeliverySystem>.Instance.HasActiveCubesOnConveyor ? cubeMovementConfig.FastShaderSpeed : cubeMovementConfig.SlowShaderSpeed);
			float timeScale = ((MonoSingleton<CustomTimeScaleGroup>.Instance != null) ? MonoSingleton<CustomTimeScaleGroup>.Instance.CurrentTimeScale : 1f);
			if (!_isShaderSpeedInitialized)
			{
				_currentShaderSpeed = targetShaderSpeed;
				_isShaderSpeedInitialized = true;
			}
			else
			{
				_currentShaderSpeed = Mathf.MoveTowards(_currentShaderSpeed, targetShaderSpeed, shaderAcceleration * Time.deltaTime * timeScale);
			}
			_currentScrollOffset += Time.deltaTime * _currentShaderSpeed * timeScale;
			_currentScrollOffset = Mathf.Repeat(_currentScrollOffset, _spacingForWrap);
			SetRoadFloat(FollowerOffsetId, _currentScrollOffset);
			if (_revealProgressDirty)
			{
				SetRoadFloat(RevealProgressId, _revealProgress);
				_revealProgressDirty = false;
			}
		}
	}

	private void SetRoadFloat(int propertyId, float value)
	{
		if (_roadRuntimeMaterial == null && _roadMeshRenderer != null)
		{
			_roadRuntimeMaterial = _roadMeshRenderer.sharedMaterial;
		}
		if (!(_roadRuntimeMaterial == null))
		{
			_roadRuntimeMaterial.SetFloat(propertyId, value);
		}
	}
}
