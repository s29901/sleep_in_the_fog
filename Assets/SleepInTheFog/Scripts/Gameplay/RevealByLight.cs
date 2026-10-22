using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Объект виден и кликабелен только в свете (например, качели в тумане).
    public class RevealByLight : MonoBehaviour
    {
        [Range(0, 1)] public float hiddenAlpha = 0.15f;
        public float threshold = 0.35f;
        [Tooltip("Если уже проявился — остаётся видимым")]
        public bool stayRevealed = true;
        SpriteRenderer[] renderers;
        Collider2D[] colliders;
        float alpha;
        bool revealed;

        void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            colliders = GetComponentsInChildren<Collider2D>();
            alpha = hiddenAlpha;
            Apply();
        }

        public void ForceReveal() { revealed = true; }

        /// Точка проверки света — центр рисунка (а не основание у земли).
        Vector2 SamplePoint()
        {
            bool any = false; Bounds b = default;
            foreach (var r in renderers)
            {
                if (!r || !r.sprite) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? (Vector2)b.center : (Vector2)transform.position;
        }

        void Update()
        {
            float clear = LightField.Clearance(SamplePoint());
            if (clear > threshold) revealed = true;
            bool visible = clear > threshold || (stayRevealed && revealed);
            alpha = Mathf.MoveTowards(alpha, visible ? 1f : hiddenAlpha, Time.deltaTime * 1.5f);
            Apply();
        }

        void Apply()
        {
            foreach (var r in renderers) { if (!r) continue; var c = r.color; c.a = alpha; r.color = c; }
            foreach (var c in colliders) if (c) c.enabled = alpha > 0.6f;
        }
    }
}
