using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace FourCore
{
    public enum GameFlowState { Title, Starting, Playing }
    public enum IntroStage { None, Camera, Grid, Entities, Complete }

    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private CinemachineCamera sceneCamera;
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

        public void Initialize(Camera output, Transform player, BoardView board, GameObject core,
            PlayerController controls, EnemyManager enemies)
        {
            this.output = output; this.player = player; this.board = board;
            this.core = core; this.controls = controls; this.enemies = enemies;
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
            worldFade?.Dispose();
            if (startButton != null) startButton.onClick.RemoveListener(BeginGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        }
    }
}
