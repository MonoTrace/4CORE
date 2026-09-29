using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private Color straightColor = new(1f, 0.22f, 0.3f, 1f);

        private Mesh enemyMesh;
        private Material enemyMaterial;

        public void Initialize(EnemyState state)
        {
            enemyMesh = CreateDiamondMesh();
            enemyMaterial = BoardView.CreateUnlitMaterial(straightColor, "Straight Enemy Material");

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = enemyMesh;
            meshRenderer.sharedMaterial = enemyMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 8;
            Apply(state);
        }

        public void Apply(EnemyState state)
        {
            transform.localPosition = GameConfig.LogicalToWorld(state.Position, -0.08f);
        }

        private static Mesh CreateDiamondMesh()
        {
            const float radius = 0.32f;
            Mesh mesh = new()
            {
                name = "Straight Enemy Diamond",
                vertices = new[]
                {
                    new Vector3(0f, radius, 0f),
                    new Vector3(radius, 0f, 0f),
                    new Vector3(0f, -radius, 0f),
                    new Vector3(-radius, 0f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (enemyMesh != null) Destroy(enemyMesh);
            if (enemyMaterial != null) Destroy(enemyMaterial);
        }
    }
}
