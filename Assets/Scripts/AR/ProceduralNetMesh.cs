using UnityEngine;

namespace ARBadmintonNet.AR
{
    /// <summary>
    /// Generates a procedural net mesh that looks like a real badminton net.
    /// Creates a grid of thin quads (horizontal + vertical lines) with a thicker top border.
    /// Attach this to the BadmintonNet prefab.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralNetMesh : MonoBehaviour
    {
        [Header("Net Dimensions")]
        [SerializeField] private float netWidth = 5.18f;   // Standard badminton net width
        [SerializeField] private float netHeight = 1.55f;  // Standard height
        
        [Header("Grid Settings")]
        [SerializeField] private float gridSpacing = 0.08f;  // 8cm between strings
        [SerializeField] private float stringThickness = 0.04f; // 40mm - matches court line width
        [SerializeField] private float topBorderHeight = 0.05f;  // 5cm top tape
        [SerializeField] private float sideBorderWidth = 0.04f;  // 40mm - matches court line width
        
        [Header("Appearance")]
        [SerializeField] private Color netColor = new Color(0.95f, 0.95f, 0.9f, 0.9f); // Off-white
        [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 1f); // White tape
        
        private Mesh netMesh;
        
        private void Awake()
        {
            GenerateNetMesh();
        }
        
        private void GenerateNetMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            
            netMesh = new Mesh();
            netMesh.name = "ProceduralBadmintonNet";
            
            // Calculate grid counts
            int horizontalLines = Mathf.FloorToInt(netHeight / gridSpacing);
            int verticalLines = Mathf.FloorToInt(netWidth / gridSpacing);
            
            // Each line is a quad (4 vertices, 6 indices)
            // +1 for top border, +2 for side borders
            int totalQuads = horizontalLines + verticalLines + 1 + 2;
            
            Vector3[] vertices = new Vector3[totalQuads * 4];
            int[] triangles = new int[totalQuads * 6];
            Vector2[] uvs = new Vector2[totalQuads * 4];
            Color[] colors = new Color[totalQuads * 4];
            
            int vertIndex = 0;
            int triIndex = 0;
            
            // Net is centered at origin, extending -width/2 to +width/2 on X, 
            // -height/2 to +height/2 on Y, flat on Z
            float halfWidth = netWidth / 2f;
            float halfHeight = netHeight / 2f;
            float depth = 0.04f; // 40mm depth - matches court line width for visual cohesion
            
            // === HORIZONTAL STRINGS ===
            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = -halfHeight + (i * gridSpacing);
                if (y > halfHeight - topBorderHeight) break; // Don't overlap with top tape
                
                AddQuad(vertices, triangles, uvs, colors,
                    ref vertIndex, ref triIndex,
                    new Vector3(-halfWidth, y - stringThickness / 2f, -depth / 2f),
                    new Vector3(halfWidth, y + stringThickness / 2f, depth / 2f),
                    netColor);
            }
            
            // === VERTICAL STRINGS ===
            for (int i = 0; i <= verticalLines; i++)
            {
                float x = -halfWidth + (i * gridSpacing);
                if (x < -halfWidth + sideBorderWidth || x > halfWidth - sideBorderWidth) continue;
                
                AddQuad(vertices, triangles, uvs, colors,
                    ref vertIndex, ref triIndex,
                    new Vector3(x - stringThickness / 2f, -halfHeight, -depth / 2f),
                    new Vector3(x + stringThickness / 2f, halfHeight - topBorderHeight, depth / 2f),
                    netColor);
            }
            
            // === TOP BORDER (tape) ===
            AddQuad(vertices, triangles, uvs, colors,
                ref vertIndex, ref triIndex,
                new Vector3(-halfWidth, halfHeight - topBorderHeight, -depth),
                new Vector3(halfWidth, halfHeight, depth),
                borderColor);
            
            // === LEFT SIDE BORDER ===
            AddQuad(vertices, triangles, uvs, colors,
                ref vertIndex, ref triIndex,
                new Vector3(-halfWidth, -halfHeight, -depth),
                new Vector3(-halfWidth + sideBorderWidth, halfHeight, depth),
                borderColor);
            
            // === RIGHT SIDE BORDER ===
            AddQuad(vertices, triangles, uvs, colors,
                ref vertIndex, ref triIndex,
                new Vector3(halfWidth - sideBorderWidth, -halfHeight, -depth),
                new Vector3(halfWidth, halfHeight, depth),
                borderColor);
            
            // Trim arrays to actual used size
            System.Array.Resize(ref vertices, vertIndex);
            System.Array.Resize(ref triangles, triIndex);
            System.Array.Resize(ref uvs, vertIndex);
            System.Array.Resize(ref colors, vertIndex);
            
            netMesh.vertices = vertices;
            netMesh.triangles = triangles;
            netMesh.uv = uvs;
            netMesh.colors = colors;
            netMesh.RecalculateNormals();
            netMesh.RecalculateBounds();
            
            meshFilter.mesh = netMesh;
            
            // Create material
            SetupMaterial(meshRenderer);
            
            Debug.Log($"[ProceduralNetMesh] Generated net: {netWidth}m x {netHeight}m, " +
                      $"{horizontalLines} h-lines, {verticalLines} v-lines, {vertIndex / 4} quads");
        }
        
        /// <summary>
        /// Adds a box quad (visible from both sides) defined by min/max corners.
        /// </summary>
        private void AddQuad(Vector3[] vertices, int[] triangles, Vector2[] uvs, Color[] colors,
            ref int vertIndex, ref int triIndex,
            Vector3 min, Vector3 max, Color color)
        {
            int v = vertIndex;
            
            // Front face (z = max.z)
            vertices[v + 0] = new Vector3(min.x, min.y, max.z);
            vertices[v + 1] = new Vector3(max.x, min.y, max.z);
            vertices[v + 2] = new Vector3(max.x, max.y, max.z);
            vertices[v + 3] = new Vector3(min.x, max.y, max.z);
            
            uvs[v + 0] = new Vector2(0, 0);
            uvs[v + 1] = new Vector2(1, 0);
            uvs[v + 2] = new Vector2(1, 1);
            uvs[v + 3] = new Vector2(0, 1);
            
            colors[v + 0] = color;
            colors[v + 1] = color;
            colors[v + 2] = color;
            colors[v + 3] = color;
            
            // Front face triangles
            triangles[triIndex + 0] = v + 0;
            triangles[triIndex + 1] = v + 2;
            triangles[triIndex + 2] = v + 1;
            triangles[triIndex + 3] = v + 0;
            triangles[triIndex + 4] = v + 3;
            triangles[triIndex + 5] = v + 2;
            
            vertIndex += 4;
            triIndex += 6;
        }
        
        private void SetupMaterial(MeshRenderer meshRenderer)
        {
            // Use URP Lit shader if available, otherwise fallback
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            
            Material netMaterial = new Material(shader);
            netMaterial.name = "NetMaterial";
            netMaterial.color = netColor;
            
            // Enable vertex colors
            netMaterial.EnableKeyword("_VERTEX_COLORS");
            
            // Configure for opaque rendering with double-sided
            netMaterial.SetFloat("_Cull", 0); // Off = double-sided
            
            // Make it slightly emissive so it's visible in AR
            if (netMaterial.HasProperty("_EmissionColor"))
            {
                netMaterial.EnableKeyword("_EMISSION");
                netMaterial.SetColor("_EmissionColor", netColor * 0.3f);
            }
            
            meshRenderer.material = netMaterial;
        }
    }
}
