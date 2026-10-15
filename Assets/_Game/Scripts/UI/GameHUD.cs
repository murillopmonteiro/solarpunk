using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Research;
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
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private RandomEventManager randomEvents;

        private ResourceStack _resources;
        private BuildPanel _buildPanel;
        private ResearchPanel _researchPanel;
        private AdminPanel _adminPanel;
        private Button _nextTurnButton;
        private Text _researchLabel;
        private Text _yearLabel;
        private RectTransform _gameOverBanner;
        private Text _gameOverText;
        private GameObject _canvas;
        private bool? _endedInVictory;

        private const int SunRays = 12;
        private static readonly Color SunUnlit = new Color(0.89f, 0.84f, 0.71f);
        private readonly Image[] _sunRays = new Image[SunRays];

        private void Start()
        {
            BuildCanvas();
            HookEvents();
            Refresh();
            _buildPanel.Show(null);
            Loc.Changed += Rebuild;
        }

        private void OnDestroy() => Loc.Changed -= Rebuild;

        /// <summary>
        /// Every label takes its text from <see cref="Loc"/> when it is built, so a
        /// language switch tears the canvas down and builds it again rather than
        /// tracking each label. The game state lives in the managers, untouched.
        /// </summary>
        private void Rebuild()
        {
            Destroy(_canvas);
            BuildCanvas();
            Refresh();
            _buildPanel.Show(selectionController.Selected);
            if (_endedInVictory.HasValue) ShowGameOver(_endedInVictory.Value);
        }

        private void Update()
        {
            if (_adminPanel != null && Input.GetKeyDown(AdminPanel.ToggleKey)) _adminPanel.Toggle();

            if (Input.GetKeyDown(KeyCode.Escape) && _researchPanel.IsOpen) _researchPanel.Show(false);

            // Space must not tick a year away while the player is reading the tree,
            // or while typing a number into the admin panel.
            if (Input.GetKeyDown(KeyCode.Space) && !_researchPanel.IsOpen && !TypingInField())
                turnManager.AdvanceTurn();
        }

        private static bool TypingInField()
        {
            GameObject focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return focused != null && focused.TryGetComponent(out InputField field) && field.isFocused;
        }

        private void BuildCanvas()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("HUD Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo;

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

            CreateYearMedallion(canvasGo.transform);
            CreateTurnControls(canvasGo.transform);
            CreateTerrainKey(canvasGo.transform);
            CreateGameOverBanner(canvasGo.transform);

            // Cheats exist only in the Editor and development builds; a release build has no panel and F1 does nothing.
            if (Debug.isDebugBuild)
            {
                _adminPanel = canvasGo.AddComponent<AdminPanel>();
                _adminPanel.Build(canvasGo.transform, resourceManager, turnManager, researchManager, randomEvents);
            }

            // Built last so the overlay sorts above every other piece of the HUD.
            _researchPanel = canvasGo.AddComponent<ResearchPanel>();
            _researchPanel.Build(canvasGo.transform, researchManager);

            // Above even the research overlay: a waiting card is the one thing the player must answer.
            canvasGo.AddComponent<EventCard>().Build(canvasGo.transform, randomEvents);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Year medallion top-left: a sun whose rays light up as the run's
        /// <see cref="TurnManager.VictoryTurn"/> years pass, next to the count.
        /// </summary>
        private void CreateYearMedallion(Transform canvas)
        {
            RectTransform panel = UIFactory.Panel("Year", canvas);
            UIFactory.Place(panel, UIFactory.TopLeft, 34f, -ResourceStack.Top, 236f, ResourceStack.Height);

            var centre = new Vector2(62f, -60f);
            for (int i = 0; i < SunRays; i++)
            {
                // Each ray hangs off a pivot at the sun's centre, so turning the pivot places it.
                RectTransform pivot = UIFactory.NewRect($"RayPivot{i}", panel);
                UIFactory.Place(pivot, UIFactory.TopLeft, centre.x, centre.y, 0f, 0f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, -i * 360f / SunRays);

                Image ray = UIFactory.Ray("Ray", pivot, SunUnlit, 11f, 14f);
                RectTransform rect = ray.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 24f);
                _sunRays[i] = ray;
            }

            RectTransform disc = UIFactory.HexBadge("Sun", panel, UIFactory.Sun, 44f, 0f);
            UIFactory.Place(disc, new Vector2(0f, 1f), centre.x - 22f, centre.y + 19f, 44f, 38f);

            Text caption = UIFactory.Label("Caption", panel, Loc.T("hud.yearCaption"), 14, UIFactory.FaintColor);
            UIFactory.Place(caption.rectTransform, UIFactory.TopLeft, 118f, -18f, 110f, 18f);

            _yearLabel = UIFactory.Label("Value", panel, "0", 44, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_yearLabel.rectTransform, UIFactory.TopLeft, 117f, -30f, 110f, 58f);

            Text of = UIFactory.Label("Of", panel, Loc.T("hud.yearOf", TurnManager.VictoryTurn), 16,
                UIFactory.MutedColor);
            UIFactory.Place(of.rectTransform, UIFactory.TopLeft, 118f, -84f, 110f, 22f);
        }

        /// <summary>Research and the primary action, stacked bottom-right.</summary>
        private void CreateTurnControls(Transform canvas)
        {
            _nextTurnButton = UIFactory.ChunkyButton("NextTurn", canvas, UIFactory.LeafLight, UIFactory.LeafDark,
                UIFactory.BottomRight, -34f, 48f, 340f, 104f, 9f);

            Text label = UIFactory.Label("Label", _nextTurnButton.transform, Loc.T("hud.nextYear"), 36,
                UIFactory.LeafText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(label.rectTransform);
            _nextTurnButton.onClick.AddListener(turnManager.AdvanceTurn);

            // Sun-gold: research is the one thing on screen that wears it.
            Button research = UIFactory.ChunkyButton("Research", canvas, UIFactory.Sun, UIFactory.SunDark,
                UIFactory.BottomRight, -34f, 180f, 340f, 76f);
            research.onClick.AddListener(() => _researchPanel.Toggle());

            _researchLabel = UIFactory.Label("Label", research.transform, Loc.T("hud.research"), 26,
                UIFactory.SunText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(_researchLabel.rectTransform);

            // Under the year, out of the way: touched once a session at most, and the
            // right-hand column is the build panel's.
            Button language = UIFactory.ChunkyButton("Language", canvas, UIFactory.Paper,
                UIFactory.Slab, UIFactory.TopLeft, 34f, -168f, 170f, 46f, 5f);
            language.onClick.AddListener(() =>
                Loc.Set(Loc.Current == Language.English ? Language.Portuguese : Language.English));

            Text languageLabel = UIFactory.Label("Label", language.transform, Loc.T("hud.switchLanguage"), 15,
                UIFactory.MutedColor, TextAnchor.MiddleCenter);
            UIFactory.Fill(languageLabel.rectTransform);
        }

        /// <summary>Bottom-left key so the hex colours are readable without clicking each one.</summary>
        private void CreateTerrainKey(Transform canvas)
        {
            const float width = 360f;
            const float rowHeight = 32f;
            const float headerHeight = 50f;

            var entries = new (TerrainRelief relief, string label)[]
            {
                (TerrainRelief.Mutable, Loc.T("hud.keyOpen")),
                (TerrainRelief.Waterfall, Loc.T("hud.keyWaterfall")),
                (TerrainRelief.Coast, Loc.T("hud.keyCoast")),
                (TerrainRelief.Mountain, Loc.T("hud.keyMountain"))
            };

            // The hint lives inside this card: as loose text it sat on the board
            // and failed to read at all.
            const float hintHeight = 48f;
            float height = headerHeight + entries.Length * rowHeight + hintHeight + 14f;

            RectTransform panel = UIFactory.Panel("TerrainKey", canvas);
            UIFactory.Place(panel, UIFactory.BottomLeft, 34f, 48f, width, height);

            Text title = UIFactory.Label("Title", panel, Loc.T("hud.terrain"), 14, UIFactory.FaintColor);
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, 24f, -20f, width - 48f, 18f);

            for (int i = 0; i < entries.Length; i++)
            {
                float y = -(headerHeight + i * rowHeight);

                RectTransform chip = UIFactory.HexBadge($"Chip{i}", panel,
                    HexCell.ColorForRelief(entries[i].relief), 26f, 0f);
                UIFactory.Place(chip, UIFactory.TopLeft, 24f, y - 2f, 26f, 23f);

                Text label = UIFactory.Label($"Key{i}", panel, entries[i].label, 18, UIFactory.TextColor);
                UIFactory.Place(label.rectTransform, UIFactory.TopLeft, 62f, y - 1f, width - 80f, 26f);
            }

            Text hint = UIFactory.Label("Hint", panel, Loc.T("hud.hint"), 15, UIFactory.FaintColor);
            hint.verticalOverflow = VerticalWrapMode.Overflow;
            UIFactory.Place(hint.rectTransform, UIFactory.BottomLeft, 24f, 18f, width - 48f, 44f);
        }

        private void CreateGameOverBanner(Transform canvas)
        {
            _gameOverBanner = UIFactory.Panel("GameOver", canvas);
            _gameOverBanner.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverBanner.pivot = new Vector2(0.5f, 0.5f);
            _gameOverBanner.anchoredPosition = Vector2.zero;
            _gameOverBanner.sizeDelta = new Vector2(640f, 290f);

            _gameOverText = UIFactory.Label("Text", _gameOverBanner, "", 32, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            _gameOverText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _gameOverText.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            _gameOverText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _gameOverText.rectTransform.offsetMin = new Vector2(34f, 0f);
            _gameOverText.rectTransform.offsetMax = new Vector2(-34f, -26f);

            Button restart = UIFactory.ChunkyButton("Restart", _gameOverBanner, UIFactory.LeafLight,
                UIFactory.LeafDark, new Vector2(0.5f, 0f), 0f, 38f, 300f, 70f);

            Text restartLabel = UIFactory.Label("Label", restart.transform, Loc.T("hud.playAgain"), 26,
                UIFactory.LeafText, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            if (researchManager != null)
            {
                researchManager.OnResearchChanged += Refresh;
                // An unlock can add build options or the territory offer to the open panel.
                researchManager.OnResearchChanged += () => _buildPanel.Show(selectionController.Selected);
            }
        }

        private void Refresh()
        {
            ResourceVector perTurn = turnManager.CalculateTurnDelta();
            _resources.SetResources(resourceManager.Current, perTurn, turnManager.EnergyCap());
            _yearLabel.text = turnManager.CurrentTurn.ToString();

            // One ray lit from year 0, all twelve at the victory year.
            int lit = Mathf.Clamp(Mathf.CeilToInt(SunRays * (float)turnManager.CurrentTurn / TurnManager.VictoryTurn),
                1, SunRays);
            for (int i = 0; i < SunRays; i++) _sunRays[i].color = i < lit ? UIFactory.Sun : SunUnlit;
            _nextTurnButton.interactable = !resourceManager.GameOver;

            // The count rides on the button so research is legible without opening it.
            if (researchManager != null)
            {
                _researchLabel.text = Loc.T("hud.researchCount", Mathf.FloorToInt(researchManager.Points));
                _researchPanel.Refresh();
            }
        }

        private void ShowGameOver(bool victory)
        {
            _endedInVictory = victory;
            _gameOverBanner.gameObject.SetActive(true);
            _gameOverText.text = victory
                ? Loc.T("hud.victory", TurnManager.VictoryTurn)
                : Loc.T("hud.collapse");
            _gameOverText.color = victory ? UIFactory.LeafDark : UIFactory.WarnColor;
        }
    }
}
