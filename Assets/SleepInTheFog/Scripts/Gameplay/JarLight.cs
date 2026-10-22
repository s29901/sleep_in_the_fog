using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
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
}
