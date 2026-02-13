using UnityEngine;
using System.Collections.Generic;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Generates procedural badminton court line markings using BWF standard dimensions.
    /// Court lines are rendered as thin white quads on the ground plane (local Y=0).
    /// The court is centered at the local origin, with the net at the center line (Z=0).
    /// </summary>
    public class ProceduralCourtMesh : MonoBehaviour
    {
        [Header("Court Dimensions (BWF Standard - meters)")]
        [SerializeField] private float courtLength = 13.40f;
        [SerializeField] private float doublesWidth = 6.10f;
        [SerializeField] private float singlesWidth = 5.18f;
        [SerializeField] private float shortServiceDistance = 1.98f; // from net
        [SerializeField] private float longServiceDistance = 0.76f; // from baseline (doubles)
        [SerializeField] private float lineWidth = 0.04f; // 40mm BWF standard
        
        [Header("Appearance")]
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private float lineElevation = 0.005f; // slight raise above ground
        [SerializeField] private bool showDoublesLines = true;
        [SerializeField] private bool showSinglesLines = true;
        [SerializeField] private bool showNetLine = true;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();

        private void Awake()
        {
            GenerateCourtMesh();
        }

        public void GenerateCourtMesh()
        {
            vertices.Clear();
            triangles.Clear();

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            float halfLength = courtLength / 2f;
            float halfDoubles = doublesWidth / 2f;
            float halfSingles = singlesWidth / 2f;
            float y = lineElevation;
            float hw = lineWidth / 2f; // half line width

            // ===== OUTER BOUNDARY (Doubles) =====
            if (showDoublesLines)
            {
                // Left doubles sideline (full length)
                AddLineQuad(-halfDoubles, y, -halfLength, -halfDoubles, y, halfLength, hw);
                // Right doubles sideline (full length)
                AddLineQuad(halfDoubles, y, -halfLength, halfDoubles, y, halfLength, hw);
                // Back baseline (near side, Z = -halfLength)
                AddLineQuad(-halfDoubles, y, -halfLength, halfDoubles, y, -halfLength, hw);
                // Back baseline (far side, Z = +halfLength)
                AddLineQuad(-halfDoubles, y, halfLength, halfDoubles, y, halfLength, hw);
            }

            // ===== SINGLES SIDELINES =====
            if (showSinglesLines)
            {
                // Left singles sideline
                AddLineQuad(-halfSingles, y, -halfLength, -halfSingles, y, halfLength, hw);
                // Right singles sideline
                AddLineQuad(halfSingles, y, -halfLength, halfSingles, y, halfLength, hw);
            }

            // ===== NET LINE (center) =====
            if (showNetLine)
            {
                AddLineQuad(-halfDoubles, y, 0, halfDoubles, y, 0, hw * 1.5f);
            }

            // ===== SHORT SERVICE LINES =====
            // Near side (Z = -shortServiceDistance)
            AddLineQuad(-halfDoubles, y, -shortServiceDistance, halfDoubles, y, -shortServiceDistance, hw);
            // Far side (Z = +shortServiceDistance)
            AddLineQuad(-halfDoubles, y, shortServiceDistance, halfDoubles, y, shortServiceDistance, hw);

            // ===== LONG SERVICE LINES (Doubles) =====
            if (showDoublesLines)
            {
                // Near side (Z = -(halfLength - longServiceDistance))
                float longServiceZ = halfLength - longServiceDistance;
                AddLineQuad(-halfDoubles, y, -longServiceZ, halfDoubles, y, -longServiceZ, hw);
                // Far side
                AddLineQuad(-halfDoubles, y, longServiceZ, halfDoubles, y, longServiceZ, hw);
            }

            // ===== CENTER LINES =====
            // Near side: from short service line to baseline
            AddLineQuad(0, y, -halfLength, 0, y, -shortServiceDistance, hw);
            // Far side: from short service line to baseline
            AddLineQuad(0, y, shortServiceDistance, 0, y, halfLength, hw);

            // Build mesh
            Mesh mesh = new Mesh();
            mesh.name = "CourtMarkings";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            SetupMaterial();

            Debug.Log($"[CourtMesh] Generated court: {courtLength}m x {doublesWidth}m, {vertices.Count / 4} line segments");
        }

        /// <summary>
        /// Adds a line quad between two points with a given half-width.
        /// Lines are drawn on the XZ plane at the specified Y height.
        /// </summary>
        private void AddLineQuad(float x1, float y1, float z1, float x2, float y2, float z2, float halfWidth)
        {
            Vector3 start = new Vector3(x1, y1, z1);
            Vector3 end = new Vector3(x2, y2, z2);
            Vector3 direction = (end - start).normalized;

            // Perpendicular direction on XZ plane
            Vector3 perp;
            if (Mathf.Abs(direction.x) > 0.001f || Mathf.Abs(direction.z) > 0.001f)
            {
                perp = new Vector3(-direction.z, 0, direction.x).normalized * halfWidth;
            }
            else
            {
                perp = new Vector3(halfWidth, 0, 0);
            }

            int baseIndex = vertices.Count;

            // Four corners of the line quad
            vertices.Add(start - perp);
            vertices.Add(start + perp);
            vertices.Add(end + perp);
            vertices.Add(end - perp);

            // Two triangles (double-sided)
            // Front face
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);

            // Back face
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
        }

        private void SetupMaterial()
        {
            // Use unlit shader so lines are always visible
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Mobile/Particles/Additive");

            Material mat = new Material(shader);
            mat.color = lineColor;
            mat.renderQueue = 3100; // Render on top of AR planes

            meshRenderer.material = mat;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        public void SetShowDoubles(bool show)
        {
            showDoublesLines = show;
            GenerateCourtMesh();
        }

        public void SetShowSingles(bool show)
        {
            showSinglesLines = show;
            GenerateCourtMesh();
        }
    }
}
