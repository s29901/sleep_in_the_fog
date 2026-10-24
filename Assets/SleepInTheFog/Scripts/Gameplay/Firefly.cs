using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Один светлячок: машет крыльями (firefly_01 ⇄ firefly_02), летает вокруг «дома»,
    /// на брюшке пульсирует огонёк (glow абрикосового цвета).
    public class Firefly : MonoBehaviour
    {
        // где на картинке 64×64 брюшко (в юнитах при 200 px = 1 юнит): чуть ниже центра
        static readonly Vector3 ButtOffset = new Vector3(0f, -0.05f, 0f);
        const float FlapFps = 14f;

        public Vector3 home;
        public float area = 1f;

        SpriteRenderer body, halo, core;
        Light2D light2D;
        float seed, flapPhase, pulseSpeed, pulsePhase, speed;
        Vector3 scatter, prevPos;
        float alpha, targetAlpha = 1f;
        bool dying;
        Sprite frameA, frameB;

        public static Firefly Create(Transform parent, Vector3 home, float area, int sortingOrder, bool withLight)
        {
            var go = new GameObject("Firefly");
            go.transform.SetParent(parent, false);
            go.transform.position = home;
            go.transform.localScale = Vector3.one * Random.Range(0.42f, 0.58f);
            var f = go.AddComponent<Firefly>();
            f.home = home;
            f.area = area;
            f.Build(sortingOrder, withLight);
            return f;
        }

        void Build(int order, bool withLight)
        {
            frameA = Art.Sprite("firefly_01");
            frameB = Art.Sprite("firefly_02");
            var glowSprite = Art.Sprite("glow");
            var glowMat = Resources.Load<Material>("SITF/GlowMaterial");

            body = gameObject.AddComponent<SpriteRenderer>();
            body.sprite = frameA;
            body.sortingOrder = order;

            halo = MakeGlow("ButtGlow", glowSprite, glowMat, order + 1, 1.3f);
            core = MakeGlow("ButtCore", glowSprite, glowMat, order + 2, 0.32f);

            if (withLight)
            {
                var lgo = new GameObject("Light");
                lgo.transform.SetParent(transform, false);
                lgo.transform.localPosition = ButtOffset;
                light2D = LightUtil.AddPointLight(lgo, LightUtil.FireflyColor, 0.9f, 0f);
            }

            seed = Random.value * 100f;
            flapPhase = Random.value * 2f;
            pulseSpeed = Random.Range(1.6f, 3.2f);
            pulsePhase = Random.value * 6.28f;
            speed = Random.Range(0.18f, 0.32f);
            prevPos = transform.position;
            Apply(0f);
        }

        SpriteRenderer MakeGlow(string name, Sprite sprite, Material mat, int order, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = ButtOffset;
            go.transform.localScale = Vector3.one * size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat) sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        /// Показать / спрятать (рой становится реже, когда светлячков поймали).
        public void SetVisible(bool v) => targetAlpha = v ? 1f : 0f;

        /// Разлететься в сторону (и потом вернуться).
        public void Scatter(Vector2 dir) => scatter += (Vector3)(dir * Random.Range(1.5f, 3f));

        public void FadeAndDie() { dying = true; targetAlpha = 0f; }

        void Update()
        {
            float dt = Time.deltaTime;
            float t = Time.time * speed;

            // плавное блуждание по шуму Перлина вокруг «дома»
            Vector3 wander = new Vector3(
                (Mathf.PerlinNoise(seed, t) - 0.5f) * 2.6f * area,
                (Mathf.PerlinNoise(seed + 37.1f, t) - 0.5f) * 2.0f * area,
                0f);
            scatter = Vector3.Lerp(scatter, Vector3.zero, dt * 0.7f);
            Vector3 target = home + wander + scatter;
            transform.position = Vector3.Lerp(transform.position, target, dt * 2.5f);

            // разворот и наклон по направлению полёта
            float vx = (transform.position.x - prevPos.x) / Mathf.Max(dt, 0.0001f);
            prevPos = transform.position;
            if (Mathf.Abs(vx) > 0.05f) body.flipX = vx < 0f;
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-vx * 12f, -20f, 20f));

            // взмахи крыльев
            body.sprite = ((Time.time * FlapFps + flapPhase) % 2f) < 1f ? frameA : frameB;

            alpha = Mathf.MoveTowards(alpha, targetAlpha, dt * 1.2f);
            Apply(Time.time);
            if (dying && alpha <= 0.001f) Destroy(gameObject);
        }

        void Apply(float time)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(time * pulseSpeed + pulsePhase);   // огонёк мерцает
            body.color = new Color(1, 1, 1, alpha);
            halo.color = Palette.WithAlpha(Palette.Apricot, alpha * pulse * 0.75f);
            core.color = Palette.WithAlpha(Color.Lerp(Palette.Apricot, Color.white, 0.5f), alpha * (0.6f + 0.4f * pulse));
            if (light2D) light2D.intensity = alpha * pulse * 0.8f;
        }
    }
}
