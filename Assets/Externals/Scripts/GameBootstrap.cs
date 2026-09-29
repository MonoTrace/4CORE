using UnityEngine;

namespace FourCore
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new(0.008f, 0.014f, 0.032f, 1f);

        private void Awake()
        {
            gameObject.name = "GameRoot";
            ConfigureCamera();

            CoreState coreState = new();
            BoardState boardState = new(coreState);
            PlayerState playerState = new(GameConfig.PlayerStart);
            ShieldState shieldState = new(GameConfig.MaxShieldCount);

            Transform boardRoot = CreateContainer("Board", transform);
            Transform entitiesRoot = CreateContainer("Entities", transform);
            Transform shieldsRoot = CreateContainer("Shields", transform);
            Transform enemiesRoot = CreateContainer("Enemies", entitiesRoot);

            BoardView boardView = boardRoot.gameObject.AddComponent<BoardView>();
            TurnManager turnManager = GetOrAddComponent<TurnManager>(gameObject);
            boardView.Initialize();

            GameObject coreObject = new("Core");
            coreObject.transform.SetParent(boardRoot, false);
            CoreView coreView = coreObject.AddComponent<CoreView>();
            coreView.Initialize(coreState);

            ShieldView shieldView = shieldsRoot.gameObject.AddComponent<ShieldView>();
            shieldView.Initialize();

            GameObject playerObject = new("Player");
            playerObject.transform.SetParent(entitiesRoot, false);

            PlayerView playerView = playerObject.AddComponent<PlayerView>();
            PlayerController playerController = playerObject.AddComponent<PlayerController>();
            playerView.Initialize(playerState);
            playerController.Initialize(boardState, playerState, playerView, shieldState, shieldView, turnManager);

            EnemyManager enemyManager = enemiesRoot.gameObject.AddComponent<EnemyManager>();
            enemyManager.Initialize(coreState, coreView, playerState, shieldState, shieldView, turnManager);
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
            camera.orthographicSize = 12.5f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Transform CreateContainer(string containerName, Transform parent)
        {
            GameObject container = new(containerName);
            container.transform.SetParent(parent, false);
            return container.transform;
        }
    }
}
