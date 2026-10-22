using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SITF.EditorTools
{
    /// Меню Tools → Sleep in the Fog. Встраивает механику в твои сцены и создаёт меню и финал.
    public static class SITF_SetupTool
    {
        const string Root = "Assets/SleepInTheFog";
        const string GameplayRootName = "SITF_Gameplay";

        // ---------------------------------------------------------------- меню

        [MenuItem("Tools/Sleep in the Fog/1. Setup current scene as Forest (scene 1)")]
        static void SetupForest() => SetupScene(true);

        [MenuItem("Tools/Sleep in the Fog/2. Setup current scene as Path (scene 2)")]
        static void SetupPath() => SetupScene(false);

        [MenuItem("Tools/Sleep in the Fog/3. Create Menu + Final scenes and Build Settings")]
        static void CreateMenuAndFinal()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets();
            string current = SceneManager.GetActiveScene().path;
            string menu = CreateMenuScene();
            string final = CreateFinalScene();
            SetBuildSettings(menu, final);
            if (!string.IsNullOrEmpty(current)) EditorSceneManager.OpenScene(current);
            EditorUtility.DisplayDialog("Sleep in the Fog",
                "Готово: созданы сцены SITF_Menu и SITF_Final, порядок сцен в Build Settings:\n" +
                "SITF_Menu → SampleScene → Scene2 → SITF_Final.\n\nЗапускай игру из SITF_Menu.", "OK");
        }

        [MenuItem("Tools/Sleep in the Fog/Fix current scene (light + colliders)")]
        static void FixCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            bool added = EnsureGlobalLight();
            int n = MakeGameplayTriggers();
            bool exit = EnsureBackExit();
            SoftenReveal();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Sleep in the Fog",
                (added ? "Добавлен Global Light 2D (его не было — поэтому сцена была чёрной).\n" : "Global Light 2D уже есть.\n") +
                $"Коллайдеров объектов сделано триггерами: {n}.\n" +
                (exit ? "Добавлен выход обратно в лес (у левого края).\n" : "") +
                "Качели проявляются от света легче.\nСцена сохранена.", "OK");
        }

        /// В 2D URP без глобального света всё, кроме освещённых мест, — чёрное.
        static bool EnsureGlobalLight()
        {
            if (Object.FindObjectsOfType<Light2D>().Any(l => l.lightType == Light2D.LightType.Global)) return false;
            var l = NewGlobalLight(0.45f, Color.white);
            Undo.RegisterCreatedObjectUndo(l.gameObject, "SITF global light");
            return true;
        }

        static int MakeGameplayTriggers()
        {
            int n = 0;
            foreach (var it in Object.FindObjectsOfType<Interactable>())
                foreach (var c in it.GetComponentsInChildren<Collider2D>(true))
                    if (!c.isTrigger) { Undo.RecordObject(c, "SITF"); c.isTrigger = true; n++; }
            return n;
        }

        [MenuItem("Tools/Sleep in the Fog/Reimport placeholder art")]
        static void ReimportArt()
        {
            SITF_ArtImporter.ImportAll(true);
            EditorUtility.DisplayDialog("Sleep in the Fog", "Картинки переимпортированы с настройками игры.", "OK");
        }

        [MenuItem("Tools/Sleep in the Fog/Delete save (PlayerPrefs)")]
        static void DeleteSave()
        {
            GameState.ClearSave();
            Debug.Log("[SITF] Сохранение удалено.");
        }

        // ---------------------------------------------------------------- общее

        static void PrepareAssets()
        {
            SITF_ArtImporter.ImportAll(false);
            EnsureGlowMaterial();
        }

        static void EnsureGlowMaterial()
        {
            string path = Root + "/Resources/SITF/GlowMaterial.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "GlowMaterial" };
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SITF_ArtImporter.ArtFolder + "/glow.png");
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SITF_ArtImporter.ArtFolder + "/" + name + ".png");

        static GameObject FindPlayer()
        {
            var tagged = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
            if (tagged) return tagged;
            var mover = Object.FindObjectOfType<PlayerMover>();
            if (mover) return mover.gameObject;
            return Object.FindObjectsOfType<Transform>().Select(t => t.gameObject)
                .FirstOrDefault(g => g.name.ToLower() == "player");
        }

        static IEnumerable<MonoBehaviour> ByTypeName(GameObject go, string typeName) =>
            go.GetComponents<MonoBehaviour>().Where(m => m != null && m.GetType().Name == typeName);

        static IEnumerable<MonoBehaviour> AllByTypeName(string typeName) =>
            Object.FindObjectsOfType<MonoBehaviour>().Where(m => m != null && m.GetType().Name == typeName);

        struct Ctx
        {
            public GameObject player;
            public PlayerMover mover;
            public SpriteRenderer playerSr;
            public float feetY, startX, endX;
            public Vector3? oldExit;
            public Transform root;
        }

        // ---------------------------------------------------------------- настройка сцены

        static void SetupScene(bool forest)
        {
            var scene = SceneManager.GetActiveScene();
            string what = forest ? "Forest (сцена 1)" : "Path (сцена 2)";
            if (!EditorUtility.DisplayDialog("Sleep in the Fog",
                $"Настроить сцену «{scene.name}» как {what}?\n\n" +
                "• старые скрипты движения героини выключатся, добавится ходьба по клику\n" +
                "• глобальный свет станет темнее (чтобы был виден свет банки и фонарей)\n" +
                "• туман из Particle System начнёт расступаться вокруг света\n" +
                "• добавятся объекты-заглушки в папку SITF_Gameplay — потом их можно двигать\n\n" +
                "Сцена будет сохранена.", "Настроить", "Отмена")) return;

            PrepareAssets();

            var ctx = new Ctx();
            ctx.player = FindPlayer();
            if (ctx.player == null)
            {
                EditorUtility.DisplayDialog("Sleep in the Fog", "Не нашла героиню: нужен объект с тегом Player или с именем «player».", "OK");
                return;
            }

            var existing = GameObject.Find(GameplayRootName);
            if (existing)
            {
                if (!EditorUtility.DisplayDialog("Sleep in the Fog", "В сцене уже есть SITF_Gameplay. Пересоздать объекты-заглушки (старые удалятся)?", "Пересоздать", "Отмена")) return;
                Undo.DestroyObjectImmediate(existing);
            }

            SetupPlayer(ref ctx);
            FixCameras();
            EnsureGlobalLight();
            DarkenGlobalLight();
            AddFogReveal();
            ctx.oldExit = DisableOldSceneTriggers();
            EnsureEventSystem();

            var rootGo = new GameObject(GameplayRootName);
            Undo.RegisterCreatedObjectUndo(rootGo, "SITF setup");
            ctx.root = rootGo.transform;

            var jarLight = new GameObject("JarLight");
            jarLight.transform.SetParent(ctx.root, false);
            var jl = jarLight.AddComponent<JarLight>();
            jl.offset = new Vector3(0.4f, ctx.playerSr ? ctx.playerSr.bounds.center.y - ctx.player.transform.position.y : 0f, 0);

            if (forest) BuildForest(ctx); else BuildPath(ctx);
            MakeGameplayTriggers();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = rootGo;
            EditorUtility.DisplayDialog("Sleep in the Fog",
                $"Сцена «{scene.name}» настроена и сохранена.\n\nОбъекты лежат в SITF_Gameplay — передвинь их, как тебе нравится. " +
                "Высоту, где ходит героиня, можно поправить в компоненте PlayerMover (Min/Max Bounds).", "OK");
        }

        static void SetupPlayer(ref Ctx ctx)
        {
            var p = ctx.player;
            Undo.RecordObject(p, "SITF player");
            if (p.tag != "Player") p.tag = "Player";

            Vector2? min = null, max = null;
            foreach (var mb in ByTypeName(p, "Player"))
            {
                var so = new SerializedObject(mb);
                var a = so.FindProperty("minBounds");
                var b = so.FindProperty("maxBounds");
                if (a != null && b != null) { min = a.vector2Value; max = b.vector2Value; }
                Undo.RecordObject(mb, "SITF");
                mb.enabled = false;
            }
            foreach (var mb in ByTypeName(p, "PlayerMoveWithAnimator")) { Undo.RecordObject(mb, "SITF"); mb.enabled = false; }

            ctx.mover = p.GetComponent<PlayerMover>();
            if (ctx.mover == null) ctx.mover = Undo.AddComponent<PlayerMover>(p);
            ctx.playerSr = p.GetComponentInChildren<SpriteRenderer>();

            if (min == null)
            {
                var bounds = GameObject.Find("CameraBounds")?.GetComponent<Collider2D>();
                float y = p.transform.position.y;
                min = new Vector2(bounds ? bounds.bounds.min.x + 1 : y - 10, y - 1f);
                max = new Vector2(bounds ? bounds.bounds.max.x - 1 : y + 30, y + 0.8f);
            }
            ctx.mover.minBounds = min.Value;
            ctx.mover.maxBounds = max.Value;

            ctx.feetY = ctx.playerSr ? ctx.playerSr.bounds.min.y + ctx.playerSr.bounds.size.y * 0.05f : p.transform.position.y;
            ctx.startX = p.transform.position.x;
            ctx.endX = ctx.mover.maxBounds.x;
        }

        static void FixCameras()
        {
            var cams = Object.FindObjectsOfType<Camera>().Where(c => c.CompareTag("MainCamera")).ToList();
            if (cams.Count <= 1) return;
            var keep = cams.FirstOrDefault(c => c.GetComponents<MonoBehaviour>().Any(m => m && m.GetType().Name == "CinemachineBrain")) ?? cams[0];
            foreach (var c in cams)
                if (c != keep) { Undo.RecordObject(c.gameObject, "SITF"); c.gameObject.SetActive(false); Debug.Log("[SITF] Выключена лишняя камера: " + c.name); }
        }

        static void DarkenGlobalLight()
        {
            foreach (var l in Object.FindObjectsOfType<Light2D>())
            {
                if (l.lightType != Light2D.LightType.Global) continue;
                Undo.RecordObject(l, "SITF");
                if (l.intensity > 0.45f) l.intensity = 0.45f;
            }
        }

        static void AddFogReveal()
        {
            foreach (var ps in Object.FindObjectsOfType<ParticleSystem>())
            {
                bool fog = ps.name.ToLower().Contains("fog") || (ps.transform.parent && ps.transform.parent.name.ToLower().Contains("fog"));
                if (fog && !ps.GetComponent<FogParticleReveal>()) Undo.AddComponent<FogParticleReveal>(ps.gameObject);
            }
        }

        static Vector3? DisableOldSceneTriggers()
        {
            Vector3? pos = null;
            foreach (var mb in AllByTypeName("SceneTrigger"))
            {
                pos = mb.transform.position;
                Undo.RecordObject(mb.gameObject, "SITF");
                mb.gameObject.SetActive(false);
            }
            return pos;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>()) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "SITF");
        }

        // ---------------------------------------------------------------- объекты

        static float At(Ctx c, float fraction, float endX) => Mathf.Lerp(c.startX + 2f, endX, fraction);

        static GameObject Sprite(Ctx c, string name, string sprite, Vector3 pos, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(c.root, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S(sprite);
            if (c.playerSr) { sr.sortingLayerID = c.playerSr.sortingLayerID; sr.sortingOrder = c.playerSr.sortingOrder + orderOffset; }
            return go;
        }

        static void FitBox(GameObject go, SpriteRenderer sr, float pad = 0.1f)
        {
            var col = go.AddComponent<BoxCollider2D>();
            if (sr && sr.sprite)
            {
                var b = sr.sprite.bounds;
                col.size = (Vector2)b.size + Vector2.one * pad;
                col.offset = b.center;
            }
        }

        static void PickupAt(Ctx c, string name, string sprite, ItemId item, float x)
        {
            var go = Sprite(c, name, sprite, new Vector3(x, c.feetY, 0), 1);
            FitBox(go, go.GetComponent<SpriteRenderer>(), 0.3f);
            var p = go.AddComponent<Pickup>();
            p.item = item;
            p.interactDistance = 0.6f;
        }

        static void SwarmAt(Ctx c, string name, float x, float height, int count)
        {
            var go = new GameObject(name);
            go.transform.SetParent(c.root, false);
            go.transform.position = new Vector3(x, c.feetY + height, 0);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 1.3f;
            var fx = new GameObject("Particles");
            fx.transform.SetParent(go.transform, false);
            fx.AddComponent<ParticleSystem>();
            var fp = fx.AddComponent<FireflyParticles>();
            fp.area = 1.1f;
            var s = go.AddComponent<FireflySwarm>();
            s.count = count;
            s.particles = fp;
            s.interactDistance = 0.8f;
        }

        static void FogWallAt(Ctx c, float x)
        {
            var go = new GameObject("ExitFog");
            go.transform.SetParent(c.root, false);
            go.transform.position = new Vector3(x, c.feetY + 2f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startColor = Palette.WithAlpha(Palette.Dusty, 0.35f);
            main.maxParticles = 80;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 8f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(2.5f, 5f, 0.1f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Resources/SITF/GlowMaterial.mat");
            if (c.playerSr) { r.sortingLayerID = c.playerSr.sortingLayerID; r.sortingOrder = c.playerSr.sortingOrder + 5; }
            go.AddComponent<FogParticleReveal>().minAlpha = 0.15f;
        }

        static void ExitAt(Ctx c, float x, string target, bool leftSide = false, bool needsLight = true)
        {
            var go = new GameObject("Exit → " + target);
            go.transform.SetParent(c.root, false);
            go.transform.position = new Vector3(x, c.feetY + 2f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 4.5f);
            var e = go.AddComponent<SceneExit>();
            e.targetScene = target;
            e.leftSide = leftSide;
            e.needsLight = needsLight;
            if (needsLight) FogWallAt(c, x + (leftSide ? -0.5f : 0.5f));
        }

        /// Выход у левого края во вторую сцену — обратно в лес.
        static bool EnsureBackExit()
        {
            if (SceneManager.GetActiveScene().name != Scenes.Path) return false;
            if (Object.FindObjectsOfType<SceneExit>().Any(e => e.targetScene == Scenes.Forest)) return false;
            var player = FindPlayer();
            var mover = player ? player.GetComponent<PlayerMover>() : null;
            var root = GameObject.Find(GameplayRootName);
            if (!mover || !root) return false;
            var c = new Ctx { player = player, mover = mover, playerSr = player.GetComponentInChildren<SpriteRenderer>(), root = root.transform };
            c.feetY = c.playerSr ? c.playerSr.bounds.min.y + c.playerSr.bounds.size.y * 0.05f : player.transform.position.y;
            ExitAt(c, mover.minBounds.x + 0.2f, Scenes.Forest, true, false);
            Undo.RegisterCreatedObjectUndo(root.transform.Find("Exit → " + Scenes.Forest).gameObject, "SITF exit");
            return true;
        }

        static void SoftenReveal()
        {
            foreach (var r in Object.FindObjectsOfType<RevealByLight>())
            {
                Undo.RecordObject(r, "SITF");
                r.threshold = 0.35f;
                r.hiddenAlpha = 0.15f;
            }
        }

        static void SwingAt(Ctx c, float x)
        {
            float h = 5.2f;
            var go = new GameObject("Swing");
            go.transform.SetParent(c.root, false);
            go.transform.position = new Vector3(x, c.feetY, 0);
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(go.transform, false);
            pivot.transform.localPosition = new Vector3(0, h, 0);
            foreach (var part in new[] { "swing_ropes", "swing_seat" })
            {
                var p = new GameObject(part);
                p.transform.SetParent(pivot.transform, false);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = S(part);
                if (c.playerSr) { sr.sortingLayerID = c.playerSr.sortingLayerID; sr.sortingOrder = c.playerSr.sortingOrder - 1; }
            }
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.6f, h);
            col.offset = new Vector2(0, h / 2f);
            go.AddComponent<RevealByLight>();
            var s = go.AddComponent<Swing>();
            s.pivot = pivot.transform;
            s.interactDistance = 1.6f;
        }

        static void LanternAt(Ctx c, string name, float x)
        {
            var go = Sprite(c, name, "lantern_off", new Vector3(x, c.feetY, 0), -1);
            FitBox(go, go.GetComponent<SpriteRenderer>(), 0.2f);
            var l = go.AddComponent<Lantern>();
            l.interactDistance = 1.0f;
        }

        static void HouseAt(Ctx c, float x)
        {
            var go = new GameObject("House (final)");
            go.transform.SetParent(c.root, false);
            go.transform.position = new Vector3(x, c.feetY + 2f, 0);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 4f);
            var win = Sprite(c, "WindowOn", "house_window_on", go.transform.position + Vector3.up * 0.5f, -1);
            win.transform.SetParent(go.transform, true);
            var sr = win.GetComponent<SpriteRenderer>();
            sr.color = new Color(1, 1, 1, 0);
            var f = go.AddComponent<FinalPoint>();
            f.windowOn = sr;
            f.interactDistance = 1.5f;
        }

        static void BuildForest(Ctx c)
        {
            float exitX = c.oldExit.HasValue ? Mathf.Min(c.oldExit.Value.x, c.endX - 0.2f) : c.endX - 0.2f;
            float end = exitX - 2f;
            PickupAt(c, "Jar", "jar_empty", ItemId.Jar, At(c, 0.08f, end));
            SwarmAt(c, "Fireflies A", At(c, 0.25f, end), 2.5f, 3);
            PickupAt(c, "Lid", "lid", ItemId.Lid, At(c, 0.42f, end));
            SwarmAt(c, "Fireflies B", At(c, 0.6f, end), 2.8f, 3);
            SwingAt(c, At(c, 0.85f, end));
            ExitAt(c, exitX, Scenes.Path);
        }

        static void BuildPath(Ctx c)
        {
            float end = c.endX - 0.5f;
            SwarmAt(c, "Fireflies C", At(c, 0.1f, end), 2.6f, 2);
            LanternAt(c, "Lantern 1", At(c, 0.3f, end));
            LanternAt(c, "Lantern 2", At(c, 0.5f, end));
            LanternAt(c, "Lantern 3", At(c, 0.7f, end));
            HouseAt(c, At(c, 0.93f, end));
            ExitAt(c, c.mover.minBounds.x + 0.2f, Scenes.Forest, true, false);
        }

        // ---------------------------------------------------------------- меню и финал

        static Camera NewCamera(Color bg)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.transform.position = new Vector3(0, 0, -10);
            return cam;
        }

        static Light2D NewGlobalLight(float intensity, Color color)
        {
            var go = new GameObject("Global Light 2D");
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.intensity = intensity;
            l.color = color;
            LightUtil.ApplyToAllLayers(l);
            return l;
        }

        static string CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera(Palette.Ink);
            NewGlobalLight(1f, Color.white);
            new GameObject("MainMenu").AddComponent<MainMenu>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            string path = Root + "/Scenes/SITF_Menu.unity";
            System.IO.Directory.CreateDirectory(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static string CreateFinalScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NewCamera(Color.Lerp(Palette.Ink, Color.black, 0.4f));
            NewGlobalLight(0.3f, Palette.Lavender);

            var room = new GameObject("Room").AddComponent<SpriteRenderer>();
            room.sprite = S("final_room");

            var nl = new GameObject("Nightlight");
            nl.transform.position = new Vector3(3.2f, -1.4f, 0);
            nl.transform.localScale = Vector3.one * 0.45f;
            var off = new GameObject("Off").AddComponent<SpriteRenderer>();
            off.transform.SetParent(nl.transform, false);
            off.sprite = S("nightlight_off");
            off.sortingOrder = 1;
            var on = new GameObject("On").AddComponent<SpriteRenderer>();
            on.transform.SetParent(nl.transform, false);
            on.sprite = S("nightlight_on");
            on.sortingOrder = 2;
            var lgo = new GameObject("Light");
            lgo.transform.SetParent(nl.transform, false);
            var light = LightUtil.AddPointLight(lgo, Palette.Apricot, 6f, 0f);

            var fc = new GameObject("FinalController").AddComponent<FinalController>();
            fc.nightlightOff = off;
            fc.nightlightOn = on;
            fc.nightlightLight = light;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            string path = Root + "/Scenes/SITF_Final.unity";
            System.IO.Directory.CreateDirectory(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static string FindScenePath(string name)
        {
            foreach (var g in AssetDatabase.FindAssets("t:Scene " + name))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return p;
            }
            return null;
        }

        static void SetBuildSettings(string menu, string final)
        {
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(menu, true) };
            foreach (var n in new[] { Scenes.Forest, Scenes.Path })
            {
                string p = FindScenePath(n);
                if (p != null) list.Add(new EditorBuildSettingsScene(p, true));
                else Debug.LogWarning("[SITF] Не нашла сцену " + n);
            }
            list.Add(new EditorBuildSettingsScene(final, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
