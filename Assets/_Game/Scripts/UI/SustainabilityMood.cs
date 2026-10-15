using Solarpunk.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Makes the environmental damage visible: below 500 sustainability the sea, the sky
    /// and the light drift from a clear blue lagoon toward a dark polluted murk, and a
    /// smog haze closes in from the screen edges, all the way there at 0. Lives on the
    /// camera, not the HUD canvas, so a HUD rebuild keeps it.
    /// </summary>
    public class SustainabilityMood : MonoBehaviour
    {
        /// <summary>Sustainability above this leaves the scene untouched.</summary>
        public const float GloomStartsBelow = 500f;

        /// <summary>How fast the look catches up with the number, in gloom per second.</summary>
        private const float FadeSpeed = 0.6f;

        private static readonly int GloomId = Shader.PropertyToID("_Gloom");
        private static readonly int GloomColorId = Shader.PropertyToID("_GloomColor");

        // Bruised violet: polluted, but still the game's palette rather than a horror filter.
        private static readonly Color Murk = new(0.30f, 0.22f, 0.38f);
        private static readonly Color SmogSky = new(0.26f, 0.20f, 0.33f);
        private static readonly Color SmogLight = new(0.86f, 0.74f, 0.92f);
        private static readonly Color SmogAmbient = new(0.24f, 0.20f, 0.30f);
        private static readonly Color Smog = new(0.24f, 0.12f, 0.32f);

        private const float LightAtFullGloom = 0.75f;

        private ResourceManager _resources;
        private Camera _camera;
        private Light _keyLight;

        private Color _cleanSky, _cleanLight, _cleanAmbient;
        private float _cleanIntensity;
        private float _gloom = -1f; // forces the first frame to apply
        private Image _haze;

        /// <summary>
        /// Installs itself on the camera every time a scene loads (restart included), so
        /// the effect needs no scene rebuild and no wiring to go missing.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads() =>
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
            {
                Camera main = Camera.main;
                if (main != null && main.GetComponent<SustainabilityMood>() == null
                                 && FindFirstObjectByType<ResourceManager>() != null)
                    main.gameObject.AddComponent<SustainabilityMood>();
            };

        private void Awake()
        {
            _resources = FindFirstObjectByType<ResourceManager>();
            _camera = GetComponent<Camera>();
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) _keyLight = light;

            if (_keyLight == null)
            {
                Debug.LogError("[SustainabilityMood] No directional light in the scene; effect off.");
                enabled = false;
                return;
            }

            _cleanSky = _camera.backgroundColor;
            _cleanLight = _keyLight.color;
            _cleanIntensity = _keyLight.intensity;
            _cleanAmbient = RenderSettings.ambientLight;
            Shader.SetGlobalColor(GloomColorId, Murk);
            _haze = BuildHaze();
        }

        /// <summary>
        /// 0 at 500 sustainability and above, 1 at zero. Eased a little toward
        /// the start, so the damage shows early: 250 is already 60% of the way.
        /// </summary>
        public static float GloomFor(float sustainability) =>
            Mathf.Pow(Mathf.Clamp01((GloomStartsBelow - sustainability) / GloomStartsBelow), 0.75f);

        private void Update()
        {
            float target = GloomFor(_resources.Current.sustainability);
            float next = _gloom < 0f ? target : Mathf.MoveTowards(_gloom, target, FadeSpeed * Time.deltaTime);
            if (Mathf.Approximately(next, _gloom)) return;
            _gloom = next;

            Shader.SetGlobalFloat(GloomId, _gloom);
            _camera.backgroundColor = Color.Lerp(_cleanSky, SmogSky, _gloom);
            _keyLight.color = Color.Lerp(_cleanLight, SmogLight, _gloom);
            _keyLight.intensity = Mathf.Lerp(_cleanIntensity, _cleanIntensity * LightAtFullGloom, _gloom);
            RenderSettings.ambientLight = Color.Lerp(_cleanAmbient, SmogAmbient, _gloom);
            _haze.color = new Color(1f, 1f, 1f, _gloom);
            _haze.enabled = _gloom > 0f;
        }

        /// <summary>
        /// A full-screen smog vignette on its own canvas, drawn under the HUD (sort -1)
        /// so panels stay readable, and with no raycaster so clicks pass through.
        /// </summary>
        private Image BuildHaze()
        {
            var canvasGo = new GameObject("SmogHaze");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance from the centre, 1 at the edge midpoints, ~1.41 in the corners.
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float edge = UIFactory.Smooth01(0.25f, 1.3f, Mathf.Sqrt(dx * dx + dy * dy));
                // A thin haze everywhere, thick smog at the borders.
                pixels[y * size + x] = new Color(Smog.r, Smog.g, Smog.b, 0.06f + 0.44f * edge);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Image image = UIFactory.NewRect("Haze", canvasGo.transform).gameObject.AddComponent<Image>();
            UIFactory.Fill(image.rectTransform);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        // The global outlives play mode in the Editor; leave the sea clean behind it.
        private void OnDestroy() => Shader.SetGlobalFloat(GloomId, 0f);
    }
}
