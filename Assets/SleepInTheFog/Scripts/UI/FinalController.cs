using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SITF
{
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
            text.color = Palette.WithAlpha(Palette.Milk, 0);

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
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { text.color = Palette.WithAlpha(Palette.Milk, t / 1.5f); yield return null; }
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
