using UnityEditor;
using UnityEngine;

namespace SITF.EditorTools
{
    /// Настройки импорта для картинок из Assets/SleepInTheFog/Resources/SITF/Art.
    /// Применяются один раз к каждому файлу (потом можно менять вручную — инструмент не перезапишет).
    /// Если заменишь PNG на свой с тем же именем — настройки (точка опоры, размер) сохранятся.
    public class SITF_ArtImporter : AssetPostprocessor
    {
        public const string ArtFolder = "Assets/SleepInTheFog/Resources/SITF/Art";
        const string Mark = "sitf";
        public const float PixelsPerUnit = 200f;   // рисунки в ×2: 200 px = 1 юнит (фоны: 100 px = 1 юнит)

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(ArtFolder)) return;
            var ti = (TextureImporter)assetImporter;
            // только для новых файлов; уже импортированные настраивает ImportAll
            if (!ti.importSettingsMissing) return;
            Apply(ti, assetPath);
        }

        static void Apply(TextureImporter ti, string path)
        {
            ti.userData = Mark;
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;
            ti.wrapMode = TextureWrapMode.Clamp;

            if (name.StartsWith("cursor_"))
            {
                ti.textureType = TextureImporterType.Cursor;
                ti.isReadable = true;
                ti.npotScale = TextureImporterNPOTScale.None;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                return;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)PivotFor(name);
            bool shard = name.StartsWith("nightlight_shard_");
            if (shard)
            {
                s.spriteMeshType = SpriteMeshType.FullRect;     // для точного клика по кусочку пазла
                s.readable = true;                              // иначе SetTextureSettings сбросит Read/Write
                ti.textureCompression = TextureImporterCompression.Uncompressed;
            }
            ti.SetTextureSettings(s);
            if (shard) ti.isReadable = true;
        }

        static SpriteAlignment PivotFor(string name)
        {
            if (name.StartsWith("jar_") || name == "lid" || name.StartsWith("lantern_") || name == "shard_world")
                return SpriteAlignment.BottomCenter;     // стоят на земле
            if (name.StartsWith("swing_")) return SpriteAlignment.TopCenter;   // висят на ветке
            return SpriteAlignment.Center;
        }

        /// Чинит осколки пазла, импортированные без Read/Write (ошибка первой версии инструмента).
        [InitializeOnLoadMethod]
        static void FixShardsOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var g in AssetDatabase.FindAssets("nightlight_shard_ t:Texture2D", new[] { ArtFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null || ti.isReadable) continue;
                    Apply(ti, path);
                    ti.SaveAndReimport();
                    Debug.Log("[SITF] Включён Read/Write для " + path);
                }
            };
        }

        /// Переимпортировать картинки, которые ещё не получили настройки (или все — force).
        public static void ImportAll(bool force)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var g in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(g);
                    var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (ti == null) continue;
                    if (!force && ti.userData == Mark) continue;
                    Apply(ti, path);
                    ti.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }
    }
}
