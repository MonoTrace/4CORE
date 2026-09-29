using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    public sealed class CoreView : MonoBehaviour
    {
        [SerializeField] private Color coreColor = new(1f, 0.075f, 0.095f, 1f);
        [SerializeField, Range(0f, 0.25f)] private float cellInset = 0.08f;

        private Mesh coreMesh;
        private Material coreMaterial;

        public void Initialize(CoreState state)
        {
            coreMesh = CreateCoreMesh(state);
            coreMaterial = BoardView.CreateUnlitMaterial(coreColor, "Core Material");

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = coreMesh;
            meshRenderer.sharedMaterial = coreMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 5;
        }

        private Mesh CreateCoreMesh(CoreState state)
        {
            Vector3[] vertices = new Vector3[state.CellCount * 4];
            int[] triangles = new int[state.CellCount * 6];
            float halfSize = GameConfig.CellSize * 0.5f - cellInset;

            for (int index = 0; index < state.CellCount; index++)
            {
                Vector3 center = GameConfig.LogicalToWorld(state.GetCell(index), -0.05f);
                int vertex = index * 4;
                int triangle = index * 6;

                vertices[vertex] = center + new Vector3(-halfSize, -halfSize, 0f);
                vertices[vertex + 1] = center + new Vector3(-halfSize, halfSize, 0f);
                vertices[vertex + 2] = center + new Vector3(halfSize, halfSize, 0f);
                vertices[vertex + 3] = center + new Vector3(halfSize, -halfSize, 0f);

                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 2;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 2;
                triangles[triangle + 5] = vertex + 3;
            }

            Mesh mesh = new()
            {
                name = "2x2 Core Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (coreMesh != null) Destroy(coreMesh);
            if (coreMaterial != null) Destroy(coreMaterial);
        }
    }
}
