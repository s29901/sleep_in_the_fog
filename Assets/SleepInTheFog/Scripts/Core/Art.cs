using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
    /// Картинки-заглушки и твои рисунки лежат в Assets/SleepInTheFog/Resources/SITF/Art.
    /// Чтобы заменить заглушку — положи свой PNG с тем же именем.
    public static class Art
    {
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        public static Sprite Sprite(string name)
        {
            if (sprites.TryGetValue(name, out var s) && s != null) return s;
            s = Resources.Load<Sprite>("SITF/Art/" + name);
            if (s == null) Debug.LogWarning("[SITF] Нет картинки Resources/SITF/Art/" + name);
            sprites[name] = s;
            return s;
        }

        public static Texture2D Texture(string name)
        {
            if (textures.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("SITF/Art/" + name);
            textures[name] = t;
            return t;
        }

        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
