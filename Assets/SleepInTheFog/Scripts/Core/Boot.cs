using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
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
}
