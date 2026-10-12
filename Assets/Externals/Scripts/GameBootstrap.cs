using UnityEngine;

namespace FourCore
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new(0.008f, 0.014f, 0.032f, 1f);
        [Header("Gameplay prefabs")]
        [SerializeField] private PlayerView playerPrefab;
        [SerializeField] private CoreView corePrefab;
        [SerializeField] private GameObject shieldPrefab;
        [SerializeField] private EnemyView[] enemyPrefabs;
        [SerializeField] private ScorePopup scorePopupPrefab;
        [Header("Scene HUD")]
        [SerializeField] private ScoreHud scoreHud;
        [SerializeField] private GameFlowController gameFlow;

        private void Awake()
        {
            if (playerPrefab == null || corePrefab == null || shieldPrefab == null ||
                scoreHud == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogError("Assign gameplay prefabs and scene HUD references on GameBootstrap.", this);
                enabled = false;
                return;
            }

            gameObject.name = "GameRoot";
            ConfigureCamera();

            CoreState coreState = new();
            ScoreState scoreState = new();
            BoardState boardState = new(coreState);
            PlayerState playerState = new(GameConfig.PlayerStart);
            PlayerEnergyState playerEnergyState = new(coreState.AliveCount);
            ShieldState shieldState = new(GameConfig.MaxShieldCount);

            Transform boardRoot = CreateContainer("Board", transform);
            Transform entitiesRoot = CreateContainer("Entities", transform);
            Transform shieldsRoot = CreateContainer("Shields", transform);
            Transform enemiesRoot = CreateContainer("Enemies", entitiesRoot);

            BoardView boardView = boardRoot.gameObject.AddComponent<BoardView>();
            TurnManager turnManager = GetOrAddComponent<TurnManager>(gameObject);
            boardView.Initialize();

            CoreView coreView = Instantiate(corePrefab, boardRoot, false);
            coreView.name = "Core";
            coreView.Initialize(coreState);

            ShieldView shieldView = shieldsRoot.gameObject.AddComponent<ShieldView>();
            shieldView.Initialize(shieldPrefab);

            PlayerView playerView = Instantiate(playerPrefab, entitiesRoot, false);
            playerView.name = "Player";
            PlayerController playerController = playerView.GetComponent<PlayerController>();
            playerView.Initialize(playerState);
            playerController.Initialize(
                boardState,
                playerState,
                playerView,
                playerEnergyState,
                shieldState,
                shieldView,
                turnManager);

            EnemyManager enemyManager = enemiesRoot.gameObject.AddComponent<EnemyManager>();
            CoreLinkView links = coreView.GetComponent<CoreLinkView>();
            if (links != null) links.Initialize(coreState, coreView, playerView.transform, playerController, gameFlow);
            enemyManager.Initialize(
                coreState,
                coreView,
                playerState,
                playerEnergyState,
                shieldState,
                shieldView,
                turnManager,
                scoreState,
                enemyPrefabs,
                scorePopupPrefab,
                startImmediately: gameFlow == null);

            scoreHud.Initialize(scoreState);
            playerController.SetUndoHistory(new TurnUndoHistory(
                playerState, playerView, playerEnergyState, coreState, coreView,
                shieldState, shieldView, scoreState, turnManager, enemyManager));
            if (gameFlow != null)
                gameFlow.Initialize(Camera.main, playerView.transform, boardView, coreView.gameObject, playerController, enemyManager);
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
