using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SITF
{
    /// Главное меню: фон, логотип, плавающие кнопки, светлячки.
    public class MainMenu : MonoBehaviour
    {
        GameObject settings;

        void Start()
        {
            Time.timeScale = 1f;
            var canvas = UIFactory.Canvas("MenuCanvas", 10, transform);

            var bg = UIFactory.Image(canvas.transform, Art.Sprite("ui_menu_bg"), Vector2.zero, "Background");
            UIFactory.Stretch(bg.rectTransform);

            var logo = UIFactory.Image(canvas.transform, Art.Sprite("ui_logo"), new Vector2(900, 340), "Logo");
            UIFactory.Anchor(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -230));
            logo.preserveAspect = true;
            logo.gameObject.AddComponent<UIFactory.Floater>().amplitude = 6f;

            float y = -40;
            int i = 0;
            if (GameState.HasSave) AddButton(canvas.transform, "Continue", GameFlow.Continue, ref y, i++);
            AddButton(canvas.transform, "Start", GameFlow.NewGame, ref y, i++);
            AddButton(canvas.transform, "Settings", () => settings.SetActive(true), ref y, i++);
            AddButton(canvas.transform, "Quit", Quit, ref y, i++);

            settings = GameUI.BuildSettings(canvas.transform, () => settings.SetActive(false));
            settings.SetActive(false);

            // светлячки в меню
            var cam = Camera.main;
            if (cam)
            {
                var fx = new GameObject("MenuFireflies");
                fx.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0);
                var ps = fx.AddComponent<ParticleSystem>();
                FireflyParticles.Configure(ps, 7f, 6f);
            }
        }

        void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction action, ref float y, int index)
        {
            var b = UIFactory.Button(parent, label, action);
            var rt = (RectTransform)b.transform;
            UIFactory.Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0, y));
            var f = b.gameObject.AddComponent<UIFactory.Floater>();
            f.phase = index * 1.3f;
            f.amplitude = 10f;
            y -= 125;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
