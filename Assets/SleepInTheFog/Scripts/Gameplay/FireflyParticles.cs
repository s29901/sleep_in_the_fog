using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
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
}
