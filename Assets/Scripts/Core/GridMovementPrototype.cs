using UnityEngine;
using UnityEngine.InputSystem;

namespace FourCore
{
    public sealed class GridMovementPrototype : MonoBehaviour
    {
        private const int GridSize = 16;
        private const float CellSize = 1f;

        [SerializeField] private Color backgroundColor = new(0.012f, 0.018f, 0.04f, 1f);
        [SerializeField] private Color gridColor = new(0.11f, 0.19f, 0.31f, 0.9f);
        [SerializeField] private Color playerColor = new(0.08f, 0.65f, 1f, 1f);

        private Vector2Int playerCell = new(7, 10);
        private Transform playerTransform;
        private Material lineMaterial;
        private Material playerMaterial;

        private void Awake()
        {
            ConfigureCamera();
            CreateMaterials();
            CreateGrid();
            CreatePlayer();
            UpdatePlayerPosition();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2Int movement = Vector2Int.zero;

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                movement = Vector2Int.up;
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                movement = Vector2Int.down;
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                movement = Vector2Int.left;
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                movement = Vector2Int.right;
            }

            if (movement == Vector2Int.zero)
            {
                return;
            }

            playerCell.x = Mathf.Clamp(playerCell.x + movement.x, 0, GridSize - 1);
            playerCell.y = Mathf.Clamp(playerCell.y + movement.y, 0, GridSize - 1);
            UpdatePlayerPosition();
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 9.2f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
        }

        private void CreateMaterials()
        {
            Shader shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader) { name = "Grid Line Material" };
            playerMaterial = new Material(shader) { name = "Player Material", color = playerColor };
        }

        private void CreateGrid()
        {
            GameObject gridRoot = new("Grid");
            gridRoot.transform.SetParent(transform, false);

            float halfSize = GridSize * CellSize * 0.5f;
            for (int index = 0; index <= GridSize; index++)
            {
                float offset = -halfSize + index * CellSize;
                CreateLine(gridRoot.transform, new Vector3(offset, -halfSize, 0f), new Vector3(offset, halfSize, 0f));
                CreateLine(gridRoot.transform, new Vector3(-halfSize, offset, 0f), new Vector3(halfSize, offset, 0f));
            }
        }

        private void CreateLine(Transform parent, Vector3 start, Vector3 end)
        {
            GameObject lineObject = new("Grid Line");
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = lineMaterial;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.startColor = gridColor;
            line.endColor = gridColor;
            line.sortingOrder = 0;
        }

        private void CreatePlayer()
        {
            GameObject player = new("Player");
            player.transform.SetParent(transform, false);
            playerTransform = player.transform;

            Mesh mesh = new()
            {
                name = "Player Triangle",
                vertices = new[]
                {
                    new Vector3(0f, 0.38f, 0f),
                    new Vector3(-0.33f, -0.19f, 0f),
                    new Vector3(0.33f, -0.19f, 0f)
                },
                triangles = new[] { 0, 2, 1 }
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            player.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = player.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = playerMaterial;
            renderer.sortingOrder = 10;
        }

        private void UpdatePlayerPosition()
        {
            float halfSize = GridSize * CellSize * 0.5f;
            playerTransform.localPosition = new Vector3(
                -halfSize + (playerCell.x + 0.5f) * CellSize,
                -halfSize + (playerCell.y + 0.5f) * CellSize,
                0f);
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }

            if (playerMaterial != null)
            {
                Destroy(playerMaterial);
            }
        }
    }
}
