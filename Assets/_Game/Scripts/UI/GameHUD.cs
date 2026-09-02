using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Tiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Assembles the whole placeholder HUD at runtime and keeps it in sync with
    /// the simulation. Built from code deliberately: it costs nothing to
    /// regenerate and there's no scene wiring to break while the layout is
    /// still churning.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private SelectionController selectionController;
        [SerializeField] private BuildController buildController;
        [SerializeField] private CityGrowth cityGrowth;

        private ResourceBar _resourceBar;
        private BuildPanel _buildPanel;
        private RectTransform _gameOverBanner;
        private Text _gameOverText;

        private void Start()
        {
            BuildCanvas();
            HookEvents();
            Refresh();
            _buildPanel.Show(null);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) turnManager.AdvanceTurn();
        }

        private void BuildCanvas()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("HUD Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            _resourceBar = canvasGo.AddComponent<ResourceBar>();
            _resourceBar.Build(canvasGo.transform);
            _resourceBar.NextTurnButton.onClick.AddListener(turnManager.AdvanceTurn);

            _buildPanel = canvasGo.AddComponent<BuildPanel>();
            _buildPanel.Build(canvasGo.transform, buildController, cityGrowth);

            CreateHint(canvasGo.transform);
            CreateGameOverBanner(canvasGo.transform);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void CreateHint(Transform canvas)
        {
            Text hint = UIFactory.Label("Hint", canvas,
                "Click a hexagon to select it  ·  Space or NEXT YEAR advances a turn", 12, UIFactory.MutedColor,
                TextAnchor.LowerLeft);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0f, 0f);
            hint.rectTransform.pivot = new Vector2(0f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(18f, 14f);
            hint.rectTransform.sizeDelta = new Vector2(680f, 20f);
        }

        private void CreateGameOverBanner(Transform canvas)
        {
            _gameOverBanner = UIFactory.Panel("GameOver", canvas, new Color(0.06f, 0.08f, 0.09f, 0.96f));
            _gameOverBanner.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverBanner.pivot = new Vector2(0.5f, 0.5f);
            _gameOverBanner.sizeDelta = new Vector2(460f, 190f);

            _gameOverText = UIFactory.Label("Text", _gameOverBanner, "", 22, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _gameOverText.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            _gameOverText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _gameOverText.rectTransform.offsetMin = new Vector2(20f, 0f);
            _gameOverText.rectTransform.offsetMax = new Vector2(-20f, -18f);

            Button restart = UIFactory.Button("Restart", _gameOverBanner, UIFactory.AccentColor);
            RectTransform restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0f);
            restartRect.anchorMax = new Vector2(0.5f, 0f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.anchoredPosition = new Vector2(0f, 26f);
            restartRect.sizeDelta = new Vector2(200f, 42f);

            Text restartLabel = UIFactory.Label("Label", restart.transform, "PLAY AGAIN", 14,
                new Color(0.05f, 0.12f, 0.07f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(restartLabel.rectTransform, 0f, 0f, 0f, 0f);

            restart.onClick.AddListener(() =>
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

            _gameOverBanner.gameObject.SetActive(false);
        }

        private void HookEvents()
        {
            resourceManager.OnResourcesChanged += _ => Refresh();
            resourceManager.OnGameEnded += ShowGameOver;
            turnManager.OnTurnAdvanced += _ => Refresh();
            selectionController.OnSelectionChanged += cell => _buildPanel.Show(cell);
            buildController.OnBoardChanged += () =>
            {
                Refresh();
                selectionController.RefreshSelection();
            };
        }

        private void Refresh()
        {
            ResourceVector perTurn = turnManager.CalculateTurnDelta();
            _resourceBar.SetResources(resourceManager.Current, perTurn);
            _resourceBar.SetTurn(turnManager.CurrentTurn);
            _resourceBar.NextTurnButton.interactable = !resourceManager.GameOver;
        }

        private void ShowGameOver(bool victory)
        {
            _gameOverBanner.gameObject.SetActive(true);
            _gameOverText.text = victory
                ? $"VICTORY\n\nYou reached year {TurnManager.VictoryTurn}."
                : "COLLAPSE\n\nSustainability or happiness hit zero.";
            _gameOverText.color = victory ? UIFactory.AccentColor : UIFactory.WarnColor;
        }
    }
}
