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

        private ResourceStack _resources;
        private BuildPanel _buildPanel;
        private Button _nextTurnButton;
        private Text _yearLabel;
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

            _resources = canvasGo.AddComponent<ResourceStack>();
            _resources.Build(canvasGo.transform);

            _buildPanel = canvasGo.AddComponent<BuildPanel>();
            _buildPanel.Build(canvasGo.transform, buildController, cityGrowth);

            CreateTurnControls(canvasGo.transform);
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

        /// <summary>Year readout and the primary action, anchored bottom-right.</summary>
        private void CreateTurnControls(Transform canvas)
        {
            RectTransform yearPill = UIFactory.Capsule("YearPill", canvas, UIFactory.Ink);
            UIFactory.Place(yearPill, UIFactory.BottomRight, -18f, 116f, 244f, 44f);

            _yearLabel = UIFactory.Label("Year", yearPill, "YEAR 0", 17, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(_yearLabel.rectTransform);

            _nextTurnButton = UIFactory.ChunkyButton("NextTurn", canvas, UIFactory.Leaf, UIFactory.LeafDark,
                UIFactory.BottomRight, -18f, 30f, 244f, 74f);

            Text label = UIFactory.Label("Label", _nextTurnButton.transform, "NEXT YEAR", 21,
                new Color(0.05f, 0.16f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(label.rectTransform);
        }

        /// <summary>Bottom-left key so the hex colours are readable without clicking each one.</summary>
        private void CreateTerrainKey(Transform canvas)
        {
            const float width = 268f;
            const float rowHeight = 24f;
            const float headerHeight = 34f;

            var entries = new (TerrainRelief relief, string label)[]
            {
                (TerrainRelief.Mutable, "Open, build anything"),
                (TerrainRelief.Waterfall, "Waterfall, hydro only"),
                (TerrainRelief.Coast, "Coast, tidal only"),
                (TerrainRelief.Mountain, "Mountain, favours wind")
            };

            // The hint lives inside this card: as loose text it sat grey-on-grass
            // and failed to read at all.
            const float hintHeight = 34f;
            float height = headerHeight + entries.Length * rowHeight + hintHeight + 10f;

            RectTransform panel = UIFactory.Card("TerrainKey", canvas, UIFactory.Ink);
            UIFactory.Place(panel, UIFactory.BottomLeft, 18f, 22f, width, height);

            Text title = UIFactory.Label("Title", panel, "TERRAIN", 10, UIFactory.FaintColor);
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, 16f, -12f, width - 32f, 14f);

            for (int i = 0; i < entries.Length; i++)
            {
                float y = -(headerHeight + i * rowHeight);

                RectTransform dot = UIFactory.Circle($"Dot{i}", panel,
                    HexCell.ColorForRelief(entries[i].relief), 12f);
                UIFactory.Place(dot, UIFactory.TopLeft, 16f, y - 3f, 12f, 12f);

                Text label = UIFactory.Label($"Key{i}", panel, entries[i].label, 12, UIFactory.MutedColor);
                UIFactory.Place(label.rectTransform, UIFactory.TopLeft, 36f, y - 4f, width - 48f, 16f);
            }

            Text hint = UIFactory.Label("Hint", panel,
                "Click a hexagon to build.\nSpace also advances a year.", 11, UIFactory.FaintColor);
            hint.verticalOverflow = VerticalWrapMode.Overflow;
            UIFactory.Place(hint.rectTransform, UIFactory.BottomLeft, 16f, 10f, width - 32f, 30f);
        }

        private void CreateGameOverBanner(Transform canvas)
        {
            _gameOverBanner = UIFactory.Card("GameOver", canvas, UIFactory.InkSolid);
            _gameOverBanner.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverBanner.pivot = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchoredPosition = Vector2.zero;
            _gameOverBanner.sizeDelta = new Vector2(520f, 216f);

            _gameOverText = UIFactory.Label("Text", _gameOverBanner, "", 23, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _gameOverText.rectTransform.anchorMin = new Vector2(0f, 0.36f);
            _gameOverText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _gameOverText.rectTransform.offsetMin = new Vector2(26f, 0f);
            _gameOverText.rectTransform.offsetMax = new Vector2(-26f, -22f);

            Button restart = UIFactory.ChunkyButton("Restart", _gameOverBanner, UIFactory.Leaf,
                UIFactory.LeafDark, new Vector2(0.5f, 0f), 0f, 30f, 228f, 52f);
            RectTransform restartRect = restart.GetComponent<RectTransform>();
            restartRect.anchoredPosition = new Vector2(-114f, 30f);

            Text restartLabel = UIFactory.Label("Label", restart.transform, "PLAY AGAIN", 16,
                new Color(0.05f, 0.16f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(restartLabel.rectTransform);

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
            _nextTurnButton.onClick.AddListener(turnManager.AdvanceTurn);
        }

        private void Refresh()
        {
            ResourceVector perTurn = turnManager.CalculateTurnDelta();
            _resources.SetResources(resourceManager.Current, perTurn);
            _yearLabel.text = $"YEAR {turnManager.CurrentTurn}";
            _nextTurnButton.interactable = !resourceManager.GameOver;
        }

        private void ShowGameOver(bool victory)
        {
            _gameOverBanner.gameObject.SetActive(true);
            _gameOverText.text = victory
                ? $"VICTORY\n\nYou reached year {TurnManager.VictoryTurn}."
                : "COLLAPSE\n\nSustainability or happiness hit zero.";
            _gameOverText.color = victory ? UIFactory.Leaf : UIFactory.WarnColor;
        }
    }
}
