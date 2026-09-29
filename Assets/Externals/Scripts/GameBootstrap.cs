using UnityEngine;

namespace FourCore
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new(0.008f, 0.014f, 0.032f, 1f);

        private void Awake()
        {
            ConfigureCamera();

            CoreState coreState = new();
            BoardState boardState = new(coreState);
            PlayerState playerState = new(GameConfig.PlayerStart);

            BoardView boardView = GetOrAddComponent<BoardView>(gameObject);
            TurnManager turnManager = GetOrAddComponent<TurnManager>(gameObject);
            boardView.Initialize();

            GameObject coreObject = new("Core");
            coreObject.transform.SetParent(transform, false);
            CoreView coreView = coreObject.AddComponent<CoreView>();
            coreView.Initialize(coreState);

            GameObject playerObject = new("Player");
            playerObject.transform.SetParent(transform, false);

            PlayerView playerView = playerObject.AddComponent<PlayerView>();
            PlayerController playerController = playerObject.AddComponent<PlayerController>();
            playerView.Initialize(playerState);
            playerController.Initialize(boardState, playerState, playerView, turnManager);
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

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
