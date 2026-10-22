using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
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
}
