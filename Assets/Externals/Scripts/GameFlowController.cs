using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace FourCore
{
    public enum GameFlowState { Title, Starting, Playing, GameOver, Reward }
    public enum IntroStage { None, Camera, Grid, Entities, Complete }

    public sealed class GameFlowController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup hudGroup;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button stageModeButton;
        [SerializeField] private UnityEngine.UI.Button infiniteModeButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private CinemachineCamera sceneCamera;
        [Header("Game over")]
        [SerializeField] private CanvasGroup gameOverGroup;
        [SerializeField] private UnityEngine.UI.Button retryButton;
        [SerializeField] private UnityEngine.UI.Button menuButton;
        [Header("Between chapters")]
        [SerializeField] private CanvasGroup rewardGroup;
        [SerializeField] private UnityEngine.UI.Button freezeReward;
        [SerializeField] private UnityEngine.UI.Button shieldReward;
        [SerializeField] private UnityEngine.UI.Button repairReward;
        [SerializeField] private TMPro.TMP_Text resultScore;
        private ScoreState score;
        private static bool retryOnLoad;
        private static GameMode retryMode;
        public GameMode SelectedMode { get; private set; } = GameMode.Stage;
        private bool loadingScene;
        private bool ownsGameOverPause;
        private bool stageVictory;
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
        private bool rewardTransition;
        private RewardIris rewardIris;
        private Vector2 rewardTitlePosition, rewardPromptPosition;
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
            PlayerController controls, EnemyManager enemies,ScoreState score=null)
        {
            this.output = output; this.player = player; this.board = board;
            this.core = core; this.controls = controls; this.enemies = enemies;
            this.score=score;
            if (gameOverGroup == null || retryButton == null || menuButton == null)
                throw new System.InvalidOperationException("Set up the Game Over screen using 4CORE/Set Up Game Over Screen.");
            gameOverGroup.gameObject.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            menuButton.onClick.AddListener(ReturnToMenu);
            enemies.AllCoresDestroyed += HandleGameOver;
            enemies.AllStagesCleared += HandleStageClear;
            enemies.RewardRequested+=ShowReward;
            if(rewardGroup!=null)
            {
                var irisObject = new GameObject("Reward Iris", typeof(RectTransform), typeof(CanvasRenderer));
                var irisRect = (RectTransform)irisObject.transform;
                irisRect.SetParent(rewardGroup.transform, false);
                irisRect.anchorMin = Vector2.zero; irisRect.anchorMax = Vector2.one;
                irisRect.offsetMin = irisRect.offsetMax = Vector2.zero;
                irisRect.SetAsFirstSibling();
                rewardIris = irisObject.AddComponent<RewardIris>();
                rewardIris.raycastTarget = false;
                var backdrop = rewardGroup.transform.Find("Backdrop");
                if (backdrop != null) backdrop.gameObject.SetActive(false);
                rewardTitlePosition = ((RectTransform)rewardGroup.transform.Find("Title")).anchoredPosition;
                rewardPromptPosition = ((RectTransform)rewardGroup.transform.Find("Prompt")).anchoredPosition;
                ConfigureAugmentChoices();
                rewardGroup.gameObject.SetActive(false);
                freezeReward.onClick.AddListener(ChooseFreeze);
                shieldReward.onClick.AddListener(ChooseShield);
                repairReward.onClick.AddListener(ChooseRepair);
            }
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
            if(stageModeButton!=null)stageModeButton.onClick.AddListener(BeginStageMode);
            else if(startButton!=null)startButton.onClick.AddListener(BeginGame);
            if(infiniteModeButton!=null)infiniteModeButton.onClick.AddListener(BeginInfiniteMode);
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
            BeginGame(retryMode);
        }
        private void ShowReward()
        {
            if(State!=GameFlowState.Playing)return;
            if(rewardGroup==null)throw new System.InvalidOperationException("Assign the between-stage augment selection screen.");
            State=GameFlowState.Reward;controls.SetGameplayInput(false);
            ConfigureAugmentChoices();
            rewardTransition = true;
            freezeReward.interactable=shieldReward.interactable=repairReward.interactable=true;
            rewardGroup.alpha=1f;rewardGroup.gameObject.SetActive(true);
            rewardGroup.transform.SetAsLastSibling();
            rewardGroup.interactable=false;rewardGroup.blocksRaycasts=true;
            StartCoroutine(RevealRewards());
        }

        private UnityEngine.UI.Button[] RewardCards => new[] { freezeReward, shieldReward, repairReward };

        private Vector2 RewardPoint(Vector3 worldPosition)
        {
            Canvas canvas = rewardGroup.GetComponentInParent<Canvas>().rootCanvas;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rewardGroup.transform,
                output.WorldToScreenPoint(worldPosition), uiCamera, out Vector2 point);
            return point;
        }

        private float IrisCoverRadius(Vector2 center)
        {
            Rect rect = ((RectTransform)rewardGroup.transform).rect;
            float x = Mathf.Max(Mathf.Abs(rect.xMin - center.x), Mathf.Abs(rect.xMax - center.x));
            float y = Mathf.Max(Mathf.Abs(rect.yMin - center.y), Mathf.Abs(rect.yMax - center.y));
            return new Vector2(x, y).magnitude + 8f;
        }

        private void SetRewardChrome(float opacity)
        {
            foreach (Transform child in rewardGroup.transform)
            {
                if (child == rewardIris.transform || child.name == "Backdrop" ||
                    child == freezeReward.transform || child == shieldReward.transform || child == repairReward.transform) continue;
                var group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                group.alpha = opacity; group.interactable = group.blocksRaycasts = false;
            }
        }

        private IEnumerator RevealRewards()
        {
            var cards = RewardCards;
            Canvas.ForceUpdateCanvases();
            SetRewardChrome(0f);
            float[] belowScreen = new float[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var view = cards[i].GetComponent<AugmentCardView>();
                belowScreen[i] = ((RectTransform)rewardGroup.transform).rect.yMin -
                    ((RectTransform)cards[i].transform).rect.height * .5f - view.RestingPosition.y - 50f;
                view.Present(1f, 1f, belowScreen[i], 0f);
            }
            Vector2 center = RewardPoint(enemies.LastDeathWorldPosition);
            rewardIris.SetDisc(center, 0f);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            while (controls.PresentationInputLocked) yield return null;
            for (float age = 0f; age < .65f; age += Time.unscaledDeltaTime)
            {
                // Recompute projection after any camera hit reaction and while the window resizes.
                center = RewardPoint(enemies.LastDeathWorldPosition);
                float t = Mathf.SmoothStep(0f, 1f, age / .65f);
                rewardIris.SetDisc(center, IrisCoverRadius(center) * t);
                hudGroup.alpha = 1f - t;
                yield return null;
            }
            rewardIris.SetDisc(center, IrisCoverRadius(center));
            hudGroup.alpha = 0f;
            yield return new WaitForSecondsRealtime(.35f);
            for (float age = 0f; age < .9f; age += Time.unscaledDeltaTime)
            {
                rewardIris.SetDisc(center, IrisCoverRadius(center));
                SetRewardChrome(Mathf.SmoothStep(0f, 1f, age / .35f));
                for (int i = 0; i < cards.Length; i++)
                {
                    float t = Mathf.SmoothStep(0f, 1f, (age - i * .09f) / .65f);
                    cards[i].GetComponent<AugmentCardView>().Present(1f, 1f, belowScreen[i] * (1f - t), 0f);
                }
                yield return null;
            }
            SetRewardChrome(1f);
            foreach (var card in cards) card.GetComponent<AugmentCardView>().EndPresentation();
            rewardTransition = false; rewardGroup.interactable = true;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(shieldReward.gameObject);
        }
        private void ConfigureAugmentChoices()
        {
            var footer = rewardGroup.transform.Find("FooterHint");
            if (footer != null) footer.gameObject.SetActive(false);
            if (enemies.Run.RewardChoices.Count != 3) return;
            ((RectTransform)rewardGroup.transform.Find("Title")).anchoredPosition = rewardTitlePosition;
            ((RectTransform)rewardGroup.transform.Find("Prompt")).anchoredPosition = rewardPromptPosition;
            foreach (var label in rewardGroup.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (label.name == "AugmentEyebrow") label.gameObject.SetActive(true);
            UnityEngine.UI.Button[] cards = { freezeReward, shieldReward, repairReward };
            string[] descriptions = { "FreezeDescription", "ShieldDescription", "RepairDescription" };
            for (int i = 0; i < cards.Length; i++)
            {
                AugmentType type = enemies.Run.RewardChoices[i];
                cards[i].transform.Find("Label").GetComponent<TMPro.TMP_Text>().text = AugmentCatalog.Name(type);
                foreach (var text in cards[i].GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (text.name == descriptions[i]) text.text = AugmentCatalog.Description(type, enemies.Run);
                    if (text.name == "Category") { text.text = AugmentCatalog.Category(type); text.color = AugmentCatalog.Color(type); }
                    if (text.name == "SelectionHint") text.gameObject.SetActive(false);
                }
                cards[i].GetComponentInChildren<AugmentCardArtwork>(true)?.Configure(type, AugmentCatalog.Color(type));
            }
            foreach (var text in rewardGroup.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (text.name == "Title") text.text = $"STAGE {enemies.Run.Chapter} CLEAR";
                if (text.name == "Prompt") text.text = "증강 하나를 선택하세요";
            }
        }
        private void ChooseFreeze()=>ChooseSlot(0);
        private void ChooseShield()=>ChooseSlot(1);
        private void ChooseRepair()=>ChooseSlot(2);
        private void ChooseSlot(int slot)
        {
            if (!rewardTransition && enemies.Run.RewardChoices.Count == 3) ChooseReward(enemies.Run.RewardChoices[slot], slot);
        }
        private void ChooseReward(AugmentType type, int slot)
        {
            if(State!=GameFlowState.Reward || rewardTransition || !enemies.ChooseReward(type))return;
            rewardTransition = true;
            rewardGroup.interactable = false;
            freezeReward.interactable = shieldReward.interactable = repairReward.interactable = false;
            if(UnityEngine.EventSystems.EventSystem.current!=null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(AcceptReward(slot, type));
        }

        private IEnumerator AcceptReward(int selected, AugmentType type)
        {
            var cards = RewardCards;
            var chosen = cards[selected].GetComponent<AugmentCardView>();
            Vector2 origin = ((RectTransform)cards[selected].transform).anchoredPosition;
            float initialScale = cards[selected].transform.localScale.x;
            Vector2 center = rewardIris.Center;
            // First turn the full-size choice into a small, still recognizable card.
            for (float age = 0f; age < .25f; age += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, age / .25f);
                chosen.PresentAt(1f, Mathf.Lerp(initialScale, .24f, t), origin, 1f);
                for (int i = 0; i < cards.Length; i++)
                    if (i != selected) cards[i].GetComponent<AugmentCardView>().Present(1f - t, 1f - .08f * t, -90f * t, 0f);
                SetRewardChrome(1f - t);
                rewardIris.SetDisc(center, IrisCoverRadius(center), 1f - .35f * t);
                hudGroup.alpha = t;
                yield return null;
            }
            // Reveal the world as the small card travels to the player's projected position.
            for (float age = 0f; age < .75f; age += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, age / .75f);
                Vector2 target = RewardPoint(player.position);
                Vector2 bend = Vector2.Lerp(origin, target, .5f) + Vector2.up * 100f;
                Vector2 position = (1f-t)*(1f-t)*origin + 2f*(1f-t)*t*bend + t*t*target;
                chosen.PresentAt(1f - Mathf.SmoothStep(0f, 1f, (t - .82f) / .18f),
                    Mathf.Lerp(.24f, .035f, t), position, 1f);
                rewardIris.SetDisc(center, IrisCoverRadius(center), .65f * (1f - Mathf.SmoothStep(0f, 1f, age / .3f)));
                yield return null;
            }
            chosen.PresentAt(0f, .035f, RewardPoint(player.position), 1f);
            SpawnPulse.Play(player, player.position, AugmentCatalog.Color(type), 1.25f);
            yield return new WaitForSecondsRealtime(.2f);
            rewardIris.SetDisc(center, 0f, 0f);
            rewardGroup.gameObject.SetActive(false);
            foreach (var card in cards) card.GetComponent<AugmentCardView>().EndPresentation();
            hudGroup.alpha = 1f;
            rewardTransition = false; State = GameFlowState.Playing; controls.SetGameplayInput(true);
        }
        private void HandleGameOver()
        {
            if (State != GameFlowState.Playing) return;
            stageVictory=false;
            State = GameFlowState.GameOver;
            controls.SetGameplayInput(false);
            StartCoroutine(ShowGameOver());
        }

        private void HandleStageClear()
        {
            if(State!=GameFlowState.Playing)return;
            stageVictory=true;State=GameFlowState.GameOver;controls.SetGameplayInput(false);
            StartCoroutine(ShowGameOver());
        }

        private IEnumerator ShowGameOver()
        {
            // Let the final core's zoom/blink/zoom-out finish before the menu appears.
            while (controls.PresentationInputLocked) yield return null;
            EndCoreDamageFocus();
            foreach(var label in gameOverGroup.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if(label.name=="Title" || label.text.Trim().ToUpperInvariant()=="GAME OVER")
                {
                    label.text=stageVictory?"ALL STAGES CLEAR":"GAME OVER";
                    if(stageVictory)label.color=new Color(.25f,1f,.65f);
                }
            if(resultScore!=null)resultScore.gameObject.SetActive(SelectedMode==GameMode.Infinite);
            if(SelectedMode==GameMode.Infinite&&resultScore!=null&&score!=null)
            {
                bool record=HighScoreStore.Submit(score.Total);
                resultScore.text=$"SCORE {score.Total}     HIGH {HighScoreStore.Best}";
                resultScore.color=record?new Color(1f,.86f,.3f):Color.white;
            }
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
            if ((State != GameFlowState.GameOver && State != GameFlowState.Playing) || loadingScene) return;
            controls.SetGameplayInput(false);
            loadingScene = true;
            retryOnLoad = startImmediately;
            retryMode = startImmediately ? SelectedMode : GameMode.Stage;
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

        public void BeginGame() => BeginStageMode();
        public void BeginStageMode() => BeginGame(GameMode.Stage);
        public void BeginInfiniteMode() => BeginGame(GameMode.Infinite);

        private void BeginGame(GameMode mode)
        {
            if (!initialized || State != GameFlowState.Title) return;
            SelectedMode = mode;
            board.SetMode(mode);
            score?.SetEnabled(mode == GameMode.Infinite);
            enemies.Run.ConfigureMode(mode);
            State = GameFlowState.Starting;
            Stage = IntroStage.Camera;
            // Disabling the whole menu must not flash every button's default grey tint.
            // Keep QUIT in its authored normal state; only START shows a pressed state.
            SetExitTint(quitButton, quitButton.colors.normalColor);
            UnityEngine.UI.Button selected = mode == GameMode.Infinite ? infiniteModeButton : (stageModeButton != null ? stageModeButton : startButton);
            if(stageModeButton!=null)SetExitTint(stageModeButton, stageModeButton.colors.normalColor);
            if(infiniteModeButton!=null)SetExitTint(infiniteModeButton, infiniteModeButton.colors.normalColor);
            if(selected!=null)SetExitTint(selected, selected.colors.pressedColor);
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
            if (enemies != null) enemies.AllStagesCleared -= HandleStageClear;
            if(enemies!=null)enemies.RewardRequested-=ShowReward;
            if(freezeReward!=null)freezeReward.onClick.RemoveListener(ChooseFreeze);
            if(shieldReward!=null)shieldReward.onClick.RemoveListener(ChooseShield);
            if(repairReward!=null)repairReward.onClick.RemoveListener(ChooseRepair);
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (menuButton != null) menuButton.onClick.RemoveListener(ReturnToMenu);
            worldFade?.Dispose();
            if (startButton != null) startButton.onClick.RemoveListener(BeginGame);
            if (stageModeButton != null) stageModeButton.onClick.RemoveListener(BeginStageMode);
            if (infiniteModeButton != null) infiniteModeButton.onClick.RemoveListener(BeginInfiniteMode);
            if (quitButton != null) quitButton.onClick.RemoveListener(Quit);
        }
    }
}
