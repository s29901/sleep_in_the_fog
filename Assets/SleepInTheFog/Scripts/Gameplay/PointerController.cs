using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SITF
{
    /// Мышь: наведение (смена курсора) и клик (идти / взаимодействовать). Правый клик — снять выбранный предмет.
    public class PointerController : MonoBehaviour
    {
        public static bool Blocked;     // пазл, пауза, катсцена
        Interactable hovered;
        readonly List<Collider2D> hits = new List<Collider2D>();

        void Update()
        {
            var player = PlayerMover.Current;
            var cam = Camera.main;
            if (player == null || cam == null || Blocked || GameFlow.Busy || Time.timeScale == 0f)
            {
                hovered = null;
                CursorManager.I.Set(CursorKind.Default);
                return;
            }

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
            hovered = overUI ? null : Pick(world);
            CursorManager.I.Set(hovered ? hovered.HoverCursor : CursorKind.Default);

            if (Input.GetMouseButtonDown(1)) GameState.I.Select(ItemId.None);
            if (!Input.GetMouseButtonDown(0) || overUI || player.Locked) return;

            if (hovered)
            {
                var target = hovered;
                var held = GameState.I.Selected;
                Debug.Log($"[SITF] Клик по {target.name} (в руке: {held})");
                player.WalkToObject(target, () =>
                {
                    if (target) target.Interact(held);
                });
            }
            else
            {
                player.WalkTo(world);
            }
        }

        Interactable Pick(Vector2 world)
        {
            hits.Clear();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.NoFilter();
            Physics2D.OverlapPoint(world, filter, hits);
            Interactable best = null;
            foreach (var h in hits)
            {
                var it = h.GetComponentInParent<Interactable>();
                if (it == null || !it.isActiveAndEnabled || !it.CanHover) continue;
                if (best == null || it.SortingOrder > best.SortingOrder) best = it;
            }
            return best;
        }
    }
}
