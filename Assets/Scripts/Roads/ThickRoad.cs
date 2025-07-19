using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class ThickRoad : MonoBehaviour
{
    public SplineContainer splineContainer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    // Vertex lists for the four corners of the prism
    List<Vector3> topLeftVerts = new();
    List<Vector3> topRightVerts = new();
    List<Vector3> bottomLeftVerts = new();
    List<Vector3> bottomRightVerts = new();

    [Tooltip("Amount of segments per world space unit. Higher values create a smoother mesh.")]
    public int resolution = 8;
    [Tooltip("The total width of the prism.")]
    public float width = 5f;
    [Tooltip("The total thickness (height) of the prism.")]
    public float thickness = 1f; // New property for prism thickness

    [Tooltip("Controls the tiling of the texture along the spline's length.")]
    public float textureScale = 1f;

    float splineLength;

    void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        
        // Subscribe to the spline changed event to rebuild the mesh automatically
        BuildMesh();
    }

	void Update() {
		if (Application.isEditor) BuildMesh();
	}

    /// <summary>
    /// Generates the four corner vertices for a cross-section of the prism at a given point on the spline.
    /// </summary>
    void SampleSplineCrossSection(float t, out Vector3 tl, out Vector3 tr, out Vector3 bl, out Vector3 br)
    {
        splineContainer.Evaluate(t, out float3 pos, out float3 forward, out float3 up);
        
        // Calculate the vector pointing to the right, perpendicular to the spline's forward and up vectors
        float3 rightVec = math.normalize(math.cross(forward, up));

        float3 halfWidthOffset = rightVec * (width / 2f);
        float3 halfThickOffset = (float3)up * (thickness / 2f);

        // Calculate the four corner points
        tl = pos - halfWidthOffset + halfThickOffset; // Top-Left
        tr = pos + halfWidthOffset + halfThickOffset; // Top-Right
        bl = pos - halfWidthOffset - halfThickOffset; // Bottom-Left
        br = pos + halfWidthOffset - halfThickOffset; // Bottom-Right
    }

    /// <summary>
    /// Populates the vertex lists for each of the four corners along the entire spline.
    /// </summary>
    void GetVerts()
    {
        // Clear previous vertex data
        topLeftVerts.Clear();
        topRightVerts.Clear();
        bottomLeftVerts.Clear();
        bottomRightVerts.Clear();

        if (splineContainer == null) return;
        
        splineLength = splineContainer.CalculateLength();
        int numSegments = Mathf.Max(1, Mathf.RoundToInt(splineLength * resolution));
        float step = 1f / numSegments;

        for (int i = 0; i <= numSegments; i++)
        {
            float t = step * i;
            SampleSplineCrossSection(t, out var tl, out var tr, out var bl, out var br);
            topLeftVerts.Add(tl);
            topRightVerts.Add(tr);
            bottomLeftVerts.Add(bl);
            bottomRightVerts.Add(br);
        }
    }

    /// <summary>
    /// Constructs the final mesh from the generated vertices.
    /// </summary>
    void BuildMesh()
    {
        if (splineContainer == null)
        {
            if (meshFilter.sharedMesh != null) meshFilter.sharedMesh.Clear();
            return;
        }

        GetVerts();
        
        Mesh m = new Mesh { name = "Spline Prism Mesh" };
        List<Vector3> verts = new();
        List<int> tris = new();
        List<Vector2> uvs = new();

        // 1. Add all vertices from the corner lists into a single list.
        // The order is important for creating the triangles later.
        verts.AddRange(topLeftVerts);
        verts.AddRange(topRightVerts);
        verts.AddRange(bottomLeftVerts);
        verts.AddRange(bottomRightVerts);

        // 2. Generate UVs.
        // U coordinate maps along the length of the spline.
        // V coordinate is 0 for left edges and 1 for right edges.
        int pointCount = topLeftVerts.Count;
        for (int i = 0; i < pointCount; i++) { uvs.Add(new Vector2(0, (splineLength * i / (float)(pointCount - 1)) / textureScale)); } // Top-Left UVs
        for (int i = 0; i < pointCount; i++) { uvs.Add(new Vector2(1, (splineLength * i / (float)(pointCount - 1)) / textureScale)); } // Top-Right UVs
        for (int i = 0; i < pointCount; i++) { uvs.Add(new Vector2(0, (splineLength * i / (float)(pointCount - 1)) / textureScale)); } // Bottom-Left UVs
        for (int i = 0; i < pointCount; i++) { uvs.Add(new Vector2(1, (splineLength * i / (float)(pointCount - 1)) / textureScale)); } // Bottom-Right UVs

        // 3. Generate triangles for each face along the prism.
        int numSegments = pointCount - 1;
        for (int i = 0; i < numSegments; i++)
        {
            // Offsets to find the start of each vertex strip
            int tl_offset = 0;
            int tr_offset = pointCount;
            int bl_offset = pointCount * 2;
            int br_offset = pointCount * 3;

            // Indices for the previous set of vertices in the segment
            int p_tl = tl_offset + i;
            int p_tr = tr_offset + i;
            int p_bl = bl_offset + i;
            int p_br = br_offset + i;

            // Indices for the current set of vertices in the segment
            int c_tl = tl_offset + i + 1;
            int c_tr = tr_offset + i + 1;
            int c_bl = bl_offset + i + 1;
            int c_br = br_offset + i + 1;

            // Top face (2 triangles)
            tris.AddRange(new int[] { p_tl, c_tl, c_tr, p_tl, c_tr, p_tr });
            // Bottom face (2 triangles, opposite winding order)
            tris.AddRange(new int[] { p_br, c_br, c_bl, p_br, c_bl, p_bl });
            // Left face (2 triangles)
            tris.AddRange(new int[] { p_bl, c_bl, c_tl, p_bl, c_tl, p_tl });
            // Right face (2 triangles)
            tris.AddRange(new int[] { p_tr, c_tr, c_br, p_tr, c_br, p_br });
        }

        // 4. Add end caps if the spline is not closed.
        if (!splineContainer.Spline.Closed && pointCount > 0)
        {
            // Start Cap
            int tl_start = 0;
            int tr_start = pointCount;
            int bl_start = pointCount * 2;
            int br_start = pointCount * 3;
            tris.AddRange(new int[] { bl_start, tl_start, tr_start, bl_start, tr_start, br_start });
            
            // End Cap
            int tl_end = pointCount - 1;
            int tr_end = pointCount * 2 - 1;
            int bl_end = pointCount * 3 - 1;
            int br_end = pointCount * 4 - 1;
            tris.AddRange(new int[] { tl_end, bl_end, br_end, tl_end, br_end, tr_end });
        }
        
        // 5. Assign the generated data to the mesh and finalize.
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals(); // Use RecalculateNormals() for smooth shading
        m.Optimize();
        
        meshFilter.sharedMesh = m;
        meshCollider.sharedMesh = m;
    }
}
