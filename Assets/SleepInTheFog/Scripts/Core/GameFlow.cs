using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
    /// Переходы между сценами с затемнением и автосохранением.
    public static class GameFlow
    {
        public static bool Busy { get; private set; }
        /// Из какой сцены пришли — героиня появится у выхода, ведущего обратно туда.
        public static string CameFrom { get; private set; }

        public static void Load(string scene, bool autosave = true)
        {
            if (Busy) return;
            CameFrom = SceneManager.GetActiveScene().name;
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
            CameFrom = null;
            Load(string.IsNullOrEmpty(GameState.I.data.scene) ? Scenes.Forest : GameState.I.data.scene, false);
        }

        public static void ToMenu() => Load(Scenes.Menu, false);
    }
}
