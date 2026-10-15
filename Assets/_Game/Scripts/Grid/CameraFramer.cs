using System.Collections.Generic;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Keeps the whole island in view: the camera keeps its fixed angle and slides and
    /// pulls back so every hex sits inside the part of the screen the HUD leaves free.
    /// Reframes when the island grows (territory bought or conquered) or the window
    /// changes size, gliding there rather than jumping.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFramer : MonoBehaviour
    {
        // The free corridor, as viewport fractions: resource bar on top, legend and
        // year card on the left, build panel and the action buttons on the right.
        private const float MarginLeft = 0.17f, MarginRight = 0.2f, MarginTop = 0.16f, MarginBottom = 0.05f;

        /// <summary>Hex top face sits at 0; its slab side runs down to about here.</summary>
        private const float SlabBottom = -0.36f;

        /// <summary>How quickly the camera settles on a new framing, per second.</summary>
        private const float GlideRate = 4f;

        private Camera _camera;
        private HexGridManager _grid;
        private Vector3 _target;
        private int _framedCells = -1;
        private Vector2Int _framedScreen;

        /// <summary>Installs itself on the camera at every scene load, so it needs no scene rebuild.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoads() =>
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) =>
            {
                Camera main = Camera.main;
                if (main != null && main.GetComponent<CameraFramer>() == null
                                 && FindFirstObjectByType<HexGridManager>() != null)
                    main.gameObject.AddComponent<CameraFramer>();
            };

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _grid = FindFirstObjectByType<HexGridManager>();
        }

        private void LateUpdate()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (_grid.Cells.Count != _framedCells || screen != _framedScreen)
            {
                bool first = _framedCells < 0;
                _framedCells = _grid.Cells.Count;
                _framedScreen = screen;
                _target = Frame();
                if (first) transform.position = _target; // the opening shot does not glide in
            }

            transform.position = Vector3.Lerp(transform.position, _target, 1f - Mathf.Exp(-GlideRate * Time.deltaTime));
        }

        /// <summary>Where the camera has to stand, at its current angle, to fit every hex in the free corridor.</summary>
        private Vector3 Frame()
        {
            List<Vector3> points = Corners();
            if (points.Count == 0) return transform.position;

            Vector3 restore = transform.position;
            Vector3 forward = transform.forward, right = transform.right, up = transform.up;

            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 p in points) bounds.Encapsulate(p);

            // Distance and sideways shift depend on each other, so settle them in a few rounds:
            // pull back until everything fits, then slide so the island sits mid-corridor.
            Vector3 pivot = bounds.center;
            float distance = 10f;
            var safeMid = new Vector2((MarginLeft + 1f - MarginRight) / 2f, (MarginBottom + 1f - MarginTop) / 2f);
            for (int round = 0; round < 4; round++)
            {
                distance = FitDistance(points, pivot, forward);
                transform.position = pivot - forward * distance;

                Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                foreach (Vector3 p in points)
                {
                    Vector3 v = _camera.WorldToViewportPoint(p);
                    min = Vector2.Min(min, v);
                    max = Vector2.Max(max, v);
                }

                Vector2 off = (min + max) / 2f - safeMid;
                float height = 2f * distance * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                pivot += right * (off.x * height * _camera.aspect) + up * (off.y * height);
            }

            Vector3 framed = pivot - forward * FitDistance(points, pivot, forward);
            transform.position = restore;
            return framed;
        }

        /// <summary>Closest distance back from the pivot at which every point lands inside the corridor.</summary>
        private float FitDistance(List<Vector3> points, Vector3 pivot, Vector3 forward)
        {
            float near = 1f, far = 200f;
            for (int i = 0; i < 24; i++)
            {
                float mid = (near + far) / 2f;
                transform.position = pivot - forward * mid;
                if (AllInside(points)) far = mid; else near = mid;
            }
            return far;
        }

        private bool AllInside(List<Vector3> points)
        {
            foreach (Vector3 p in points)
            {
                Vector3 v = _camera.WorldToViewportPoint(p);
                if (v.z <= 0f || v.x < MarginLeft || v.x > 1f - MarginRight
                    || v.y < MarginBottom || v.y > 1f - MarginTop) return false;
            }
            return true;
        }

        /// <summary>Six corners of every hex, at the top face and at the bottom of its slab.</summary>
        private List<Vector3> Corners()
        {
            var points = new List<Vector3>();
            foreach (HexCell cell in _grid.Cells.Values)
            {
                Vector3 c = cell.transform.position;
                float radius = cell.transform.lossyScale.x;
                for (int k = 0; k < 6; k++)
                {
                    // Pointy-top hexes: corners at 30°, 90°, 150°...
                    float a = (60f * k + 30f) * Mathf.Deg2Rad;
                    var corner = new Vector3(c.x + radius * Mathf.Cos(a), 0f, c.z + radius * Mathf.Sin(a));
                    points.Add(corner + Vector3.up * c.y);
                    points.Add(corner + Vector3.up * (c.y + SlabBottom));
                }
            }
            return points;
        }
    }
}
