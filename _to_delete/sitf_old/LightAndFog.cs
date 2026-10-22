using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Источник света, который разгоняет туман и проявляет скрытые объекты.
    public class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();
        [Tooltip("Радиус, в котором туман полностью рассеян (юниты)")]
        public float radius = 2f;
        [Range(0, 1)] public float strength = 1f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }

    public static class LightField
    {
        /// 0 = темно и туман, 1 = полностью светло.
        public static float Clearance(Vector2 p)
        {
            float best = 0f;
            for (int i = 0; i < LightSource.All.Count; i++)
            {
                var l = LightSource.All[i];
                if (l.strength <= 0f || l.radius <= 0f) continue;
                float d = Vector2.Distance(p, l.transform.position) / l.radius;
                float v = (1f - Mathf.SmoothStep(0.55f, 1.15f, d)) * l.strength;
                if (v > best) best = v;
            }
            return best;
        }
    }

    public static class LightUtil
    {
        static FieldInfo layersField;

        /// Точечный 2D-свет, который освещает все сортировочные слои.
        public static Light2D AddPointLight(GameObject go, Color color, float radius, float intensity)
        {
            var l = go.GetComponent<Light2D>();
            if (l == null) l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.15f;
            l.falloffIntensity = 0.6f;
            ApplyToAllLayers(l);
            return l;
        }

        public static void ApplyToAllLayers(Light2D l)
        {
            if (layersField == null)
                layersField = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (layersField == null) return;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            layersField.SetValue(l, ids);
        }

        public static readonly Color FireflyColor = new Color(1f, 0.82f, 0.45f);
    }

    /// Свет банки со светлячками. Следует за героиней; радиус растёт с числом светлячков.
    public class JarLight : MonoBehaviour
    {
        public float baseRadius = 1.6f, perFirefly = 0.4f, intensity = 1.1f;
        [Tooltip("Где банка относительно героини (юниты)")]
        public Vector3 offset = new Vector3(0.3f, 1.2f, 0f);
        Light2D light2D;
        LightSource source;
        float shown;

        void Awake()
        {
            light2D = LightUtil.AddPointLight(gameObject, LightUtil.FireflyColor, baseRadius, 0f);
            source = gameObject.AddComponent<LightSource>();
            source.strength = 0f;
        }

        void Update()
        {
            var gs = GameState.I;
            float targetRadius = 0f;
            if (gs != null && gs.JarHasLight) targetRadius = baseRadius + perFirefly * gs.data.fireflies;
            shown = Mathf.MoveTowards(shown, targetRadius, Time.deltaTime * 2f);
            float flicker = 1f + Mathf.Sin(Time.time * 7.3f) * 0.04f + Mathf.Sin(Time.time * 3.1f) * 0.05f;

            light2D.pointLightOuterRadius = Mathf.Max(0.01f, shown * 1.8f * flicker);
            light2D.intensity = shown > 0.01f ? intensity * flicker : 0f;
            source.radius = shown;
            source.strength = shown > 0.01f ? 1f : 0f;
        }

        void LateUpdate()
        {
            var p = PlayerMover.Current;
            if (p) transform.position = p.transform.position + offset;
        }
    }

    /// Туман из Particle System, который расступается вокруг света.
    [RequireComponent(typeof(ParticleSystem))]
    public class FogParticleReveal : MonoBehaviour
    {
        [Range(0, 1)] public float minAlpha = 0.05f;
        public float fadeSpeed = 3f;
        ParticleSystem ps;
        ParticleSystem.Particle[] buf;
        float baseAlpha = 1f;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            var sc = ps.main.startColor;
            baseAlpha = sc.mode == ParticleSystemGradientMode.TwoColors ? (sc.colorMin.a + sc.colorMax.a) * 0.5f
                      : sc.mode == ParticleSystemGradientMode.Color ? sc.color.a : 1f;
        }

        void LateUpdate()
        {
            int max = ps.main.maxParticles;
            if (buf == null || buf.Length < max) buf = new ParticleSystem.Particle[max];
            int n = ps.GetParticles(buf);
            bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            float k = Mathf.Clamp01(Time.deltaTime * fadeSpeed);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = local ? transform.TransformPoint(buf[i].position) : buf[i].position;
                float clear = LightField.Clearance(p);
                Color32 c = buf[i].startColor;
                float target = baseAlpha * Mathf.Lerp(1f, minAlpha, clear);
                float a = Mathf.Lerp(c.a / 255f, target, k);
                c.a = (byte)Mathf.RoundToInt(a * 255f);
                buf[i].startColor = c;
            }
            ps.SetParticles(buf, n);
        }
    }

    /// Объект виден и кликабелен только в свете (например, качели в тумане).
    public class RevealByLight : MonoBehaviour
    {
        [Range(0, 1)] public float hiddenAlpha = 0.08f;
        public float threshold = 0.5f;
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

        void Update()
        {
            float clear = LightField.Clearance(transform.position);
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

    /// Рой светлячков: частицы со свечением и мерцанием.
    [RequireComponent(typeof(ParticleSystem))]
    public class FireflyParticles : MonoBehaviour
    {
        public float area = 1.5f;
        public float rate = 4f;
        ParticleSystem ps;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            Configure(ps, area, rate);
        }

        public void SetRate(float r)
        {
            var e = ps.emission;
            e.rateOverTime = r;
        }

        public void Burst(int count)
        {
            var ep = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            ps.Emit(ep, count);
        }

        public static void Configure(ParticleSystem ps, float area, float rate)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startColor = LightUtil.FireflyColor;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = area;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.3f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.3f, 0.45f),
                        new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0.5f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.material = Resources.Load<Material>("SITF/GlowMaterial");
            r.sortingOrder = 50;
            ps.Play();
        }
    }

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
