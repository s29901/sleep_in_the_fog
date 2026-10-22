using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Светящиеся точки, которые летят от объекта к героине (светлячки «влетают» в банку).
    public static class Effects
    {
        public static void FlyTo(Vector3 from, Transform to, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("FlyingGlow");
                go.transform.position = from + (Vector3)Random.insideUnitCircle * 0.6f;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Sprite("glow");
                sr.color = color;
                sr.sortingOrder = 60;
                go.transform.localScale = Vector3.one * 0.35f;
                var mat = Resources.Load<Material>("SITF/GlowMaterial");
                if (mat) sr.sharedMaterial = mat;
                go.AddComponent<Flyer>().Init(to, 0.6f + i * 0.15f);
            }
        }

        class Flyer : MonoBehaviour
        {
            Transform target; float time, t; Vector3 start, bend;
            public void Init(Transform to, float duration)
            {
                target = to; time = duration; start = transform.position;
                bend = (Vector3)Random.insideUnitCircle * 1.5f + Vector3.up;
            }
            void Update()
            {
                if (!target) { Destroy(gameObject); return; }
                t += Time.deltaTime / time;
                Vector3 end = target.position + Vector3.up * 1.2f;
                Vector3 mid = (start + end) * 0.5f + bend;
                float u = Mathf.SmoothStep(0, 1, t);
                transform.position = Vector3.Lerp(Vector3.Lerp(start, mid, u), Vector3.Lerp(mid, end, u), u);
                if (t >= 1f) Destroy(gameObject);
            }
        }

        public static IEnumerator Pop(Transform tr, float time = 0.25f)
        {
            Vector3 s = tr.localScale;
            for (float t = 0; t < time; t += Time.deltaTime)
            {
                if (!tr) yield break;
                tr.localScale = s * (1f + Mathf.Sin(t / time * Mathf.PI) * 0.25f);
                yield return null;
            }
            if (tr) tr.localScale = s;
        }
    }
}
