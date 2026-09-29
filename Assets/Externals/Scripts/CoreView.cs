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
            coreMaterial = BoardView.CreateUnlitMaterial(coreColor, "Core Material");

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = coreMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 5;

            Apply(state);
        }

        public void Apply(CoreState state)
        {
            if (coreMesh != null)
            {
                Destroy(coreMesh);
            }

            coreMesh = CreateCoreMesh(state);
            GetComponent<MeshFilter>().sharedMesh = coreMesh;
        }

        private Mesh CreateCoreMesh(CoreState state)
        {
            int aliveCount = 0;
            for (int index = 0; index < state.CellCount; index++)
            {
                if (state.IsAlive(index)) aliveCount++;
            }

            Vector3[] vertices = new Vector3[aliveCount * 4];
            int[] triangles = new int[aliveCount * 6];
            float halfSize = GameConfig.CellSize * 0.5f - cellInset;
            int aliveIndex = 0;

            for (int index = 0; index < state.CellCount; index++)
            {
                if (!state.IsAlive(index)) continue;

                Vector3 center = GameConfig.LogicalToWorld(state.GetCell(index), -0.05f);
                int vertex = aliveIndex * 4;
                int triangle = aliveIndex * 6;

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
                aliveIndex++;
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
