using System.Collections.Generic;
using UnityEngine;

namespace SITF
{
    /// Рой светлячков из нарисованных Firefly (firefly_01/02 + светящееся брюшко).
    /// Имя класса осталось прежним, чтобы не сломать уже настроенные сцены;
    /// Particle System на объекте больше не используется (выключается).
    [RequireComponent(typeof(ParticleSystem))]
    public class FireflyParticles : MonoBehaviour
    {
        [Tooltip("Радиус, в котором летают светлячки (юниты)")]
        public float area = 1.5f;
        [Tooltip("Сколько светлячков в рое")]
        public float rate = 5f;
        [Tooltip("Каждый какой светлячок светит на окружение (Light 2D). 0 — ни один")]
        public int lightEvery = 3;
        public int sortingOrder = 50;

        readonly List<Firefly> flies = new List<Firefly>();
        bool built;

        void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = GetComponent<ParticleSystemRenderer>();
            if (r) r.enabled = false;
        }

        void Start() => Build();

        void Build()
        {
            if (built) return;
            built = true;
            int n = Mathf.Clamp(Mathf.RoundToInt(rate), 0, 30);
            for (int i = 0; i < n; i++)
            {
                Vector3 home = transform.position + (Vector3)(Random.insideUnitCircle * area * 0.5f);
                bool lit = lightEvery > 0 && i % lightEvery == 0;
                flies.Add(Firefly.Create(transform, home, area * 0.6f, sortingOrder, lit));
            }
        }

        /// Сколько светлячков видно (для роя, из которого часть уже поймали).
        public void SetRate(float r)
        {
            Build();
            int visible = Mathf.Clamp(Mathf.RoundToInt(r), 0, flies.Count);
            for (int i = 0; i < flies.Count; i++) if (flies[i]) flies[i].SetVisible(i < visible);
        }

        /// Разлететься (например, когда пытаешься поймать без крышки).
        public void Burst(int count)
        {
            foreach (var f in flies) if (f) f.Scatter(Random.insideUnitCircle.normalized);
        }

        /// Плавно погаснуть и исчезнуть вместе с объектом.
        public void FadeOut(float destroyAfter = 2f)
        {
            foreach (var f in flies) if (f) f.FadeAndDie();
            Destroy(gameObject, destroyAfter);
        }

        /// Создать рой на объекте с Particle System (для воспоминания и меню).
        public static FireflyParticles Configure(ParticleSystem ps, float area, float count)
        {
            var fp = ps.GetComponent<FireflyParticles>();
            if (fp == null) fp = ps.gameObject.AddComponent<FireflyParticles>();
            fp.area = area;
            fp.rate = count;
            return fp;   // рой появится в Start, уже с этими настройками
        }
    }
}
