using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using SplineArchitect.Objects;
using NaughtyAttributes;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshCollider))]
public class SplineRoad : MonoBehaviour {
	public Spline spline;
	MeshCollider meshCollider;

	// these will make up either edge of the road
	readonly List<Vector3> leftVerts = new();
	readonly List<Vector3> rightVerts = new();

	[Tooltip("Amount of segments per world space unit")]
	public int resolution = 1;
	public float width = 5f;

	public float textureScale = 1f;

	readonly Vector3[] rightUpForward = new Vector3[3];
	Vector3 pos;

	void OnEnable() {
		meshCollider = GetComponent<MeshCollider>();
	}

	void Start() {
		BuildMesh();
	}

	void Update() {
		if (!Application.isPlaying) BuildMesh();
	}

	void SampleSplineWidth(float t, out Vector3 left, out Vector3 right) {
		spline.GetNormalsNonAlloc(rightUpForward, t);
		pos = spline.GetPosition(t, Space.World);
		left = pos + (rightUpForward[0] * width / 2f);
		right = pos - (rightUpForward[0] * width / 2f);
	}

	void GetVerts() {
		leftVerts.Clear();
		rightVerts.Clear();
		float actualResolution = resolution * spline.length;
		for (int i = 0; i < actualResolution; i++) {
			float t = i / actualResolution;
			SampleSplineWidth(t, out Vector3 left, out Vector3 right);
			leftVerts.Add(left);
			rightVerts.Add(right);
		}
	}

	[Button("Build Mesh")]
	void BuildMesh() {
		if (spline == null) return;
		GetVerts();
		// mesh filter thing has the mesh scaling weirdly
		Mesh m = new();
		List<Vector3> verts = new();
		List<int> tris = new();
		List<Vector2> uvs = new();
		int offset;

		int length = leftVerts.Count;
		float uvOffset = 0;
		for (int i = 1; i < length; i++) {
			Vector3 p1 = leftVerts[i - 1];
			Vector3 p2 = rightVerts[i - 1];
			Vector3 p3 = leftVerts[i];
			Vector3 p4 = rightVerts[i];

			offset = 4 * (i - 1);
			// it has to be like this because of triangle winding order
			int t1 = offset + 3;
			int t2 = offset + 2;
			int t3 = offset + 0;

			int t4 = offset + 0;
			int t5 = offset + 1;
			int t6 = offset + 3;

			verts.AddRange(new Vector3[] { p1, p2, p3, p4 });
			tris.AddRange(new int[] { t1, t2, t3, t4, t5, t6 });

			float distance = (1f / length) * spline.length;
			float uvDistance = uvOffset + (distance / textureScale);
			uvs.AddRange(new Vector2[] {
				new(uvOffset, 0),
				new(uvOffset, 1),
				new(uvDistance, 0),
				new(uvDistance, 1)
			});
			uvOffset += distance;
		}

		m.SetVertices(verts);
		m.SetTriangles(tris, 0);
		m.Optimize();
		m.RecalculateTangents();
		m.RecalculateBounds();
		meshCollider.sharedMesh = m;
		gameObject.layer = LayerMask.NameToLayer("Ground");
	}
}
