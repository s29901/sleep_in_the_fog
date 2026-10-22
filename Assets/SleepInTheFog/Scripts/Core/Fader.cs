using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
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
}
