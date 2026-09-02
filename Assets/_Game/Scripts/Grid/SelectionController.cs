using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Turns a left-click in the world into a selected <see cref="HexCell"/>.
    /// Clicks that land on the HUD are ignored; clicks on empty space clear
    /// the selection.
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask hexLayers = ~0;

        public HexCell Selected { get; private set; }
        public event Action<HexCell> OnSelectionChanged;

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            HexCell hit = Physics.Raycast(ray, out RaycastHit info, 500f, hexLayers)
                ? info.collider.GetComponentInParent<HexCell>()
                : null;

            Select(hit);
        }

        public void Select(HexCell cell)
        {
            if (Selected == cell) return;

            if (Selected != null) Selected.SetSelected(false);
            Selected = cell;
            if (Selected != null) Selected.SetSelected(true);

            OnSelectionChanged?.Invoke(Selected);
        }

        /// <summary>Re-raises the selection event, e.g. after the board changes.</summary>
        public void RefreshSelection() => OnSelectionChanged?.Invoke(Selected);
    }
}
