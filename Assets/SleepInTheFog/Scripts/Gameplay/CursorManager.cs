using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SITF
{
    public enum CursorKind { Default, Interact, Grab }

    /// Курсоры cursor_default / cursor_interact / cursor_grab, точка клика — в центре.
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager I { get; private set; }
        CursorKind current = (CursorKind)(-1);

        void Awake() { I = this; Set(CursorKind.Default); }

        public void Set(CursorKind kind)
        {
            if (kind == current) return;
            current = kind;
            string name = kind == CursorKind.Interact ? "cursor_interact" : kind == CursorKind.Grab ? "cursor_grab" : "cursor_default";
            var tex = Art.Texture(name);
            if (tex == null && kind == CursorKind.Grab) tex = Art.Texture("cursor_interact");
            if (tex == null) { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); return; }
            Cursor.SetCursor(tex, new Vector2(tex.width / 2f, tex.height / 2f), CursorMode.Auto);
        }
    }
}
