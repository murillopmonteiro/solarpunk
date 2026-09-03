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
    /// Assembles the HUD at runtime and keeps it in sync with the simulation.
    /// Built from code deliberately: it regenerates cleanly and there's no scene
    /// wiring to break while the layout is still churning.
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

            CreateTerrainKey(canvasGo.transform);
            CreateGameOverBanner(canvasGo.transform);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        /// <summary>Bottom-left key so the hex colours are readable without clicking each one.</summary>
        private void CreateTerrainKey(Transform canvas)
        {
            const float width = 300f;
            const float rowHeight = 22f;
            const float headerHeight = 30f;

            var entries = new (TerrainRelief relief, string label)[]
            {
                (TerrainRelief.Mutable, "Open — build anything"),
                (TerrainRelief.Waterfall, "Waterfall — Hidrelétrica only"),
                (TerrainRelief.Coast, "Coast — Maremotriz only"),
                (TerrainRelief.Mountain, "Mountain — favours wind")
            };

            float height = headerHeight + entries.Length * rowHeight + 10f;
            RectTransform panel = UIFactory.Panel("TerrainKey", canvas, UIFactory.PanelColor);
            UIFactory.Place(panel, UIFactory.BottomLeft, 16f, 44f, width, height);

            Text title = UIFactory.Label("Title", panel, "TERRAIN", 11, UIFactory.FaintColor);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(12f, -10f);
            title.rectTransform.sizeDelta = new Vector2(width - 24f, 14f);

            for (int i = 0; i < entries.Length; i++)
            {
                float y = -(headerHeight + i * rowHeight);
                UIFactory.Swatch(panel, 12f, y - 4f, 10f, 10f, HexCell.ColorForRelief(entries[i].relief));

                Text label = UIFactory.Label($"Key{i}", panel, entries[i].label, 12, UIFactory.MutedColor);
                UIFactory.Place(label.rectTransform, UIFactory.TopLeft, 30f, y - 6f, width - 42f, 16f);
            }

            Text hint = UIFactory.Label("Hint", canvas,
                "Click a hexagon to inspect and build  ·  Space or NEXT YEAR advances a year", 12,
                UIFactory.MutedColor);
            UIFactory.Place(hint.rectTransform, UIFactory.BottomLeft, 16f, 18f, 900f, 18f);
        }

        private void CreateGameOverBanner(Transform canvas)
        {
            _gameOverBanner = UIFactory.Panel("GameOver", canvas, new Color(0.06f, 0.08f, 0.09f, 0.97f));
            _gameOverBanner.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverBanner.pivot = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchoredPosition = Vector2.zero;
            _gameOverBanner.sizeDelta = new Vector2(480f, 200f);

            _gameOverText = UIFactory.Label("Text", _gameOverBanner, "", 22, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _gameOverText.rectTransform.anchorMin = new Vector2(0f, 0.34f);
            _gameOverText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _gameOverText.rectTransform.offsetMin = new Vector2(24f, 0f);
            _gameOverText.rectTransform.offsetMax = new Vector2(-24f, -20f);

            Button restart = UIFactory.Button("Restart", _gameOverBanner, UIFactory.AccentColor);
            RectTransform restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0f);
            restartRect.anchorMax = new Vector2(0.5f, 0f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.anchoredPosition = new Vector2(0f, 28f);
            restartRect.sizeDelta = new Vector2(210f, 44f);

            Text restartLabel = UIFactory.Label("Label", restart.transform, "PLAY AGAIN", 15,
                new Color(0.04f, 0.11f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            restartLabel.rectTransform.anchorMin = Vector2.zero;
            restartLabel.rectTransform.anchorMax = Vector2.one;
            restartLabel.rectTransform.offsetMin = Vector2.zero;
            restartLabel.rectTransform.offsetMax = Vector2.zero;

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
