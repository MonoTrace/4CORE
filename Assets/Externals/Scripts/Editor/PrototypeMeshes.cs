using UnityEngine;

namespace FourCore.EditorTools
{
    // Bakes the existing prototype geometry into editable prefab assets once.
    public static class PrototypeMeshes
    {
        public static Mesh CreateEnemyMesh(EnemyType type)
        {
            return type switch
            {
                EnemyType.Diagonal => CreateSquareMesh(),
                EnemyType.RightAngle => CreateRightAngleMesh(),
                EnemyType.Zigzag => CreateHexagonMesh(),
                _ => CreateDiamondMesh()
            };
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

        private static Mesh CreateSquareMesh()
        {
            const float halfSize = 0.3f;
            Mesh mesh = new()
            {
                name = "Diagonal Enemy Square",
                vertices = new[]
                {
                    new Vector3(-halfSize, -halfSize, 0f),
                    new Vector3(-halfSize, halfSize, 0f),
                    new Vector3(halfSize, halfSize, 0f),
                    new Vector3(halfSize, -halfSize, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateRightAngleMesh()
        {
            Mesh mesh = new()
            {
                name = "Right Angle Enemy L",
                vertices = new[]
                {
                    new Vector3(-0.32f, -0.32f, 0f),
                    new Vector3(-0.32f, 0.32f, 0f),
                    new Vector3(0.04f, 0.32f, 0f),
                    new Vector3(0.04f, -0.32f, 0f),
                    new Vector3(0.04f, -0.32f, 0f),
                    new Vector3(0.04f, -0.04f, 0f),
                    new Vector3(0.32f, -0.04f, 0f),
                    new Vector3(0.32f, -0.32f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateHexagonMesh()
        {
            const float radius = 0.33f;
            float halfHeight = radius * 0.8660254f;
            Mesh mesh = new()
            {
                name = "Zigzag Enemy Hexagon",
                vertices = new[]
                {
                    Vector3.zero,
                    new Vector3(radius, 0f, 0f),
                    new Vector3(radius * 0.5f, halfHeight, 0f),
                    new Vector3(-radius * 0.5f, halfHeight, 0f),
                    new Vector3(-radius, 0f, 0f),
                    new Vector3(-radius * 0.5f, -halfHeight, 0f),
                    new Vector3(radius * 0.5f, -halfHeight, 0f)
                },
                triangles = new[]
                {
                    0, 2, 1,
                    0, 3, 2,
                    0, 4, 3,
                    0, 5, 4,
                    0, 6, 5,
                    0, 1, 6
                }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        public static Mesh CreateTriangleMesh()
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

    }
}

