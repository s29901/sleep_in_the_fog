using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
    /// Названия сцен. Если переименуешь сцену в Unity — поменяй здесь.
    public static class Scenes
    {
        public const string Menu = "SITF_Menu";
        public const string Forest = "SampleScene";
        public const string Path = "Scene2";
        public const string Final = "SITF_Final";
    }

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

    /// Создаёт постоянные системы игры при запуске любой сцены.
    public static class Boot
    {
        public static GameObject Root { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Root != null) return;
            Root = new GameObject("SITF_Systems");
            Object.DontDestroyOnLoad(Root);
            Root.AddComponent<GameState>();
            Root.AddComponent<AudioManager>();
            Root.AddComponent<CursorManager>();
            Root.AddComponent<Fader>();
            Root.AddComponent<GameUI>();
            Root.AddComponent<PointerController>();

            SceneManager.sceneLoaded += (s, m) => EnsureEventSystem();
            EnsureEventSystem();
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    /// Переходы между сценами с затемнением и автосохранением.
    public static class GameFlow
    {
        public static bool Busy { get; private set; }

        public static void Load(string scene, bool autosave = true)
        {
            if (Busy) return;
            Fader.I.StartCoroutine(LoadRoutine(scene, autosave));
        }

        static IEnumerator LoadRoutine(string scene, bool autosave)
        {
            Busy = true;
            GameState.I.Select(ItemId.None);
            yield return Fader.I.Fade(1f, 0.8f);
            if (autosave && scene != Scenes.Menu && scene != Scenes.Final) GameState.I.Save(scene);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return Fader.I.Fade(0f, 0.8f);
            Busy = false;
        }

        public static void NewGame()
        {
            GameState.ClearSave();
            GameState.I.ResetGame();
            Load(Scenes.Forest);
        }

        public static void Continue()
        {
            if (!GameState.I.Load()) { NewGame(); return; }
            Load(string.IsNullOrEmpty(GameState.I.data.scene) ? Scenes.Forest : GameState.I.data.scene, false);
        }

        public static void ToMenu() => Load(Scenes.Menu, false);
    }

    /// Чёрный экран для переходов.
    public class Fader : MonoBehaviour
    {
        public static Fader I { get; private set; }
        CanvasGroup group;

        void Awake()
        {
            I = this;
            var canvas = UIFactory.Canvas("SITF_Fader", 1000, transform);
            var img = UIFactory.Image(canvas.transform, null, Vector2.zero);
            UIFactory.Stretch(img.rectTransform);
            img.color = Color.black;
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public IEnumerator Fade(float to, float time)
        {
            group.blocksRaycasts = true;
            float from = group.alpha;
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / time));
                yield return null;
            }
            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
        }
    }

    /// Громкость музыки и звуков. Звуки кладутся в Resources/SITF/Audio (например pickup.wav) — если файла нет, просто тишина.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        AudioSource music, sfx;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("sitf_music", 0.7f);
            set { PlayerPrefs.SetFloat("sitf_music", value); if (I) I.music.volume = value; }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("sitf_sfx", 0.8f);
            set { PlayerPrefs.SetFloat("sitf_sfx", value); if (I) I.sfx.volume = value; }
        }

        void Awake()
        {
            I = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = MusicVolume;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.volume = SfxVolume;
            var clip = Resources.Load<AudioClip>("SITF/Audio/music");
            if (clip) { music.clip = clip; music.Play(); }
        }

        public static void Sfx(string name)
        {
            if (!I) return;
            var clip = Resources.Load<AudioClip>("SITF/Audio/" + name);
            if (clip) I.sfx.PlayOneShot(clip);
        }
    }
}
