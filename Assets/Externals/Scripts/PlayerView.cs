using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Color playerColor = new(0.05f, 0.65f, 1f, 1f);

        private Mesh playerMesh;
        private Material playerMaterial;

        public void Initialize(PlayerState state)
        {
            playerMesh = CreateTriangleMesh();
            playerMaterial = BoardView.CreateUnlitMaterial(playerColor, "Player Material");

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = playerMesh;
            meshRenderer.sharedMaterial = playerMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 10;

            Apply(state);
        }

        public void Apply(PlayerState state)
        {
            transform.localPosition = GameConfig.LogicalToWorld(state.Position, -0.1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, FacingToAngle(state.Facing));
        }

        private static Mesh CreateTriangleMesh()
        {
            const float halfHeight = 0.29f;
            float halfWidth = halfHeight * 2f / Mathf.Sqrt(3f);

            Mesh mesh = new()
            {
                name = "Player Triangle Mesh",
                vertices = new[]
                {
                    new Vector3(0f, halfHeight, 0f),
                    new Vector3(-halfWidth, -halfHeight, 0f),
                    new Vector3(halfWidth, -halfHeight, 0f)
                },
                triangles = new[] { 0, 2, 1 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static float FacingToAngle(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.Right => -90f,
                FacingDirection.Down => 180f,
                FacingDirection.Left => 90f,
                _ => 0f
            };
        }

        private void OnDestroy()
        {
            if (playerMesh != null) Destroy(playerMesh);
            if (playerMaterial != null) Destroy(playerMaterial);
        }
    }
}
