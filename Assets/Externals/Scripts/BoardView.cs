using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private Color gridColor = new(0.11f, 0.28f, 0.48f, 0.9f);

        private Mesh gridMesh;
        private Material gridMaterial;

        public void Initialize()
        {
            if (gridMesh != null)
            {
                return;
            }

            GameObject gridObject = new("Grid Lines");
            gridObject.transform.SetParent(transform, false);

            MeshFilter meshFilter = gridObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gridObject.AddComponent<MeshRenderer>();

            gridMesh = CreateGridMesh();
            gridMaterial = CreateUnlitMaterial(gridColor, "Grid Material");

            meshFilter.sharedMesh = gridMesh;
            meshRenderer.sharedMaterial = gridMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 0;
        }

        private static Mesh CreateGridMesh()
        {
            int lineCount = (GameConfig.PlayableGridSize + 1) * 2;
            Vector3[] vertices = new Vector3[lineCount * 2];
            int[] indices = new int[vertices.Length];
            float halfBoard = GameConfig.PlayableGridSize * GameConfig.CellSize * 0.5f;
            int vertexIndex = 0;

            for (int index = 0; index <= GameConfig.PlayableGridSize; index++)
            {
                float offset = -halfBoard + index * GameConfig.CellSize;

                vertices[vertexIndex] = new Vector3(offset, -halfBoard, 0f);
                indices[vertexIndex] = vertexIndex++;
                vertices[vertexIndex] = new Vector3(offset, halfBoard, 0f);
                indices[vertexIndex] = vertexIndex++;

                vertices[vertexIndex] = new Vector3(-halfBoard, offset, 0f);
                indices[vertexIndex] = vertexIndex++;
                vertices[vertexIndex] = new Vector3(halfBoard, offset, 0f);
                indices[vertexIndex] = vertexIndex++;
            }

            Mesh mesh = new() { name = "16x16 Grid Mesh" };
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Material CreateUnlitMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new(shader)
            {
                name = materialName,
                color = color
            };
            return material;
        }

        private void OnDestroy()
        {
            if (gridMesh != null) Destroy(gridMesh);
            if (gridMaterial != null) Destroy(gridMaterial);
        }
    }
}
