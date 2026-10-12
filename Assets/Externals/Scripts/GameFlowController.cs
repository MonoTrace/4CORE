using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace FourCore
{
    public enum GameFlowState { Title, Starting, Playing, GameOver }
    public enum IntroStage { None, Camera, Grid, Entities, Complete }

    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private CinemachineCamera sceneCamera;
        [Header("Game over")]
        [SerializeField] private CanvasGroup gameOverGroup;
        [SerializeField] private UnityEngine.UI.Button retryButton;
        [SerializeField] private UnityEngine.UI.Button menuButton;
        private static bool retryOnLoad;
        private bool loadingScene;
        private bool ownsGameOverPause;
        private float gameOverTimeScale;
        [SerializeField, Min(0.1f)] private float transitionDuration = 1.5f;
        [SerializeField, Min(0.1f)] private float gridFadeDuration = 0.6f;
        [SerializeField, Min(0.1f)] private float entitiesFadeDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float titleZoom = 1.35f;
        [SerializeField, Min(0.1f)] private float gameplayZoom = 12.5f;
        [SerializeField, Range(0.55f, 0.9f)] private float playerScreenX = 0.73f;
        public GameFlowState State { get; private set; } = GameFlowState.Title;
        public IntroStage Stage { get; private set; }
        private IntroWorldFade worldFade;
        private Camera output;
        private Transform player;
        private BoardView board;
        private GameObject core;
        private PlayerController controls;
        private EnemyManager enemies;
        private bool initialized;
        private bool damageCameraActive;
        private Vector3 damageCameraOrigin;
        private float damageCameraZoom;

        public void BeginCoreDamageFocus()
        {
            if (!initialized || State != GameFlowState.Playing || damageCameraActive) return;
            damageCameraOrigin = sceneCamera != null ? sceneCamera.transform.position : output.transform.position;
            damageCameraZoom = sceneCamera != null ? sceneCamera.Lens.OrthographicSize : output.orthographicSize;
            damageCameraActive = true;
        }

        public void SetCoreDamageFocus(float amount)
        {
            if (!damageCameraActive) return;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount));
            Vector3 focus = player.position;
            focus.z = damageCameraOrigin.z;
            SetCamera(Vector3.Lerp(damageCameraOrigin, focus, eased),
                Mathf.Lerp(damageCameraZoom, Mathf.Min(damageCameraZoom, 4f), eased));
        }

        public void EndCoreDamageFocus()
        {
            if (!damageCameraActive) return;
            SetCamera(damageCameraOrigin, damageCameraZoom);
            damageCameraActive = false;
        }

        public void Initialize(Camera output, Transform player, BoardView board, GameObject core,
            PlayerController controls, EnemyManager enemies)
        {
            this.output = output; this.player = player; this.board = board;
            this.core = core; this.controls = controls; this.enemies = enemies;
            if (gameOverGroup == null || retryButton == null || menuButton == null)
                throw new System.InvalidOperationException("Set up the Game Over screen using 4CORE/Set Up Game Over Screen.");
            gameOverGroup.gameObject.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            menuButton.onClick.AddListener(ReturnToMenu);
            enemies.AllCoresDestroyed += HandleGameOver;
            RestoreCanvasRootScale(titleGroup);
            RestoreCanvasRootScale(hudGroup);
            titleGroup.gameObject.SetActive(true);
            hudGroup.gameObject.SetActive(true);
            controls.SetGameplayInput(false);
            board.SetVisibility(0f);
            core.SetActive(false);
            core.GetComponent<CoreLinkView>()?.SetVisibility(0f);
            hudGroup.alpha = 0f;
            hudGroup.interactable = hudGroup.blocksRaycasts = false;
            titleGroup.alpha = 1f;
            titleGroup.interactable = titleGroup.blocksRaycasts = true;
            startButton.onClick.AddListener(BeginGame);
            quitButton.onClick.AddListener(Quit);
            State = GameFlowState.Title;
            initialized = true;
            SetCamera(TitlePosition(), titleZoom);
            if (retryOnLoad)
            {
                retryOnLoad = false;
                StartCoroutine(StartRetry());
            }
        }

        private IEnumerator StartRetry()
        {
            yield return null;
            BeginGame();
        }

        private void HandleGameOver()
        {
            if (State != GameFlowState.Playing) return;
            State = GameFlowState.GameOver;
            controls.SetGameplayInput(false);
            StartCoroutine(ShowGameOver());
        }

        private IEnumerator ShowGameOver()
        {
            // Let the final core's zoom/blink/zoom-out finish before the menu appears.
            while (controls.PresentationInputLocked) yield return null;
            EndCoreDamageFocus();
            gameOverTimeScale = Time.timeScale;
            ownsGameOverPause = true;
            Time.timeScale = 0f;
            gameOverGroup.gameObject.SetActive(true);
            gameOverGroup.transform.SetAsLastSibling();
            gameOverGroup.alpha = 1f;
            gameOverGroup.interactable = gameOverGroup.blocksRaycasts = true;
            hudGroup.interactable = hudGroup.blocksRaycasts = false;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }

        public void Retry() => ReloadGame(true);
        public void ReturnToMenu() => ReloadGame(false);

        private void ReloadGame(bool startImmediately)
        {
            if (State != GameFlowState.GameOver || loadingScene) return;
            loadingScene = true;
            retryOnLoad = startImmediately;
            gameOverGroup.interactable = gameOverGroup.blocksRaycasts = false;
            ReleaseGameOverPause();
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene.path);
        }

        private void ReleaseGameOverPause()
        {
            if (!ownsGameOverPause) return;
            Time.timeScale = gameOverTimeScale;
            ownsGameOverPause = false;
        }

        private static void RestoreCanvasRootScale(CanvasGroup group)
        {
            if (group == null) return;
            RectTransform root = group.transform as RectTransform;
            if (root == null) return;
            Vector3 scale = root.localScale;
            if (Mathf.Abs(scale.x) < 0.0001f || Mathf.Abs(scale.y) < 0.0001f || Mathf.Abs(scale.z) < 0.0001f)
            {
                root.localScale = Vector3.one;
                Debug.LogWarning($"Restored zero-scale UI canvas root '{root.name}' so its buttons and text can render.", root);
            }
        }

        private Vector3 TitlePosition()
        {
            Vector3 position = player.position;
            position.x -= (playerScreenX - 0.5f) * 2f * titleZoom * output.aspect;
            position.z = -10f;
            return position;
        }

        private void LateUpdate()
        {
            if (initialized && State == GameFlowState.Title) SetCamera(TitlePosition(), titleZoom);
        }

        public void BeginGame()
        {
            if (!initialized || State != GameFlowState.Title) return;
            State = GameFlowState.Starting;
            Stage = IntroStage.Camera;
            // Disabling the whole menu must not flash every button's default grey tint.
            // Keep QUIT in its authored normal state; only START shows a pressed state.
            SetExitTint(quitButton, quitButton.colors.normalColor);
            SetExitTint(startButton, startButton.colors.pressedColor);
            titleGroup.interactable = titleGroup.blocksRaycasts = false;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(Transition());
        }

        private static void SetExitTint(UnityEngine.UI.Button button, Color color)
        {
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.disabledColor = color;
            button.colors = colors;
        }

        private IEnumerator Transition()
        {
            Vector3 from = TitlePosition();
            Vector3 to = new(0f, 0f, -10f);
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                SetCamera(Vector3.Lerp(from, to, eased), Mathf.Lerp(titleZoom, gameplayZoom, eased));
                titleGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.3f));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetCamera(to, gameplayZoom);
            titleGroup.alpha = 0f;
            titleGroup.gameObject.SetActive(false);

            Stage = IntroStage.Grid;
            elapsed = 0f;
            while (elapsed < gridFadeDuration)
            {
                board.SetVisibility(Mathf.SmoothStep(0f, 1f, elapsed / gridFadeDuration));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            board.SetVisibility(1f);

            Stage = IntroStage.Entities;
            // Spawn and make transparent in the same frame, before anything can render.
            enemies.StartSpawning();
            worldFade = new IntroWorldFade(core, enemies.gameObject);
            core.SetActive(true);
            elapsed = 0f;
            while (elapsed < entitiesFadeDuration)
            {
                float opacity = Mathf.SmoothStep(0f, 1f, elapsed / entitiesFadeDuration);
                worldFade.SetOpacity(opacity);
                core.GetComponent<CoreLinkView>()?.SetVisibility(opacity);
                hudGroup.alpha = opacity;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            worldFade.Dispose();
            worldFade = null;
            core.GetComponent<CoreLinkView>()?.SetVisibility(1f);
            hudGroup.alpha = 1f;
            Stage = IntroStage.Complete;
            State = GameFlowState.Playing;
            controls.SetGameplayInput(true);
        }

        private void SetCamera(Vector3 position, float zoom)
        {
            if (sceneCamera != null)
            {
                sceneCamera.transform.SetPositionAndRotation(position, Quaternion.identity);
                sceneCamera.Lens.OrthographicSize = zoom;
            }
            output.transform.SetPositionAndRotation(position, Quaternion.identity);
            output.orthographic = true;
            output.orthographicSize = zoom;
        }

        private void Quit()
        {
            if (State != GameFlowState.Title) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            ReleaseGameOverPause();
            if (enemies != null) enemies.AllCoresDestroyed -= HandleGameOver;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (menuButton != null) menuButton.onClick.RemoveListener(ReturnToMenu);
            worldFade?.Dispose();
            if (startButton != null) startButton.onClick.RemoveListener(BeginGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        }
    }
}
