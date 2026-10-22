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

    /// Финал: комната, ночник загорается, титры. Клик — в меню.
    public class FinalController : MonoBehaviour
    {
        public UnityEngine.Rendering.Universal.Light2D nightlightLight;
        public SpriteRenderer nightlightOff, nightlightOn;
        public string credits = "Sleep in the Fog\n\nMade by Alina Shcherbin";
        bool canLeave;

        IEnumerator Start()
        {
            Time.timeScale = 1f;
            var canvas = UIFactory.Canvas("FinalCanvas", 10, transform);
            var text = UIFactory.Label(canvas.transform, credits, 44, new Vector2(1200, 400));
            UIFactory.Anchor(text.rectTransform, new Vector2(0.5f, 0.2f), Vector2.zero);
            text.color = new Color(1, 1, 1, 0);

            if (nightlightOn) nightlightOn.color = new Color(1, 1, 1, 0);
            if (nightlightLight) nightlightLight.intensity = 0;
            yield return new WaitForSeconds(1.5f);

            AudioManager.Sfx("nightlight");
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                float k = t / 2f;
                float flick = k < 0.4f ? (Random.value > 0.5f ? 1f : 0.3f) : 1f;
                if (nightlightOn) nightlightOn.color = new Color(1, 1, 1, k * flick);
                if (nightlightLight) nightlightLight.intensity = 1.3f * k * flick;
                yield return null;
            }
            yield return new WaitForSeconds(1f);
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { text.color = new Color(1, 1, 1, t / 1.5f); yield return null; }
            canLeave = true;
        }

        void Update()
        {
            if (canLeave && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            {
                canLeave = false;
                GameState.I.ResetGame();
                GameFlow.ToMenu();
            }
        }
    }
}
