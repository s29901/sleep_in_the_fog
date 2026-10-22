using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Фонарь: клик с банкой со светом — зажечь (тратит одного светлячка). Может оставить осколок.
    public class Lantern : Interactable
    {
        public bool dropsShard = true;
        public Vector2 shardOffset = new Vector2(0.8f, 0f);
        public float lightRadius = 3.5f;
        SpriteRenderer sr;
        Light2D light2D;
        LightSource source;
        bool lit;

        public override bool CanHover => !lit;

        void Start()
        {
            sr = GetComponentInChildren<SpriteRenderer>();
            var lgo = new GameObject("LanternLight");
            lgo.transform.SetParent(transform, false);
            lgo.transform.localPosition = new Vector3(0, 3.1f, 0);
            light2D = LightUtil.AddPointLight(lgo, LightUtil.FireflyColor, lightRadius, 0f);
            source = lgo.AddComponent<LightSource>();
            source.radius = lightRadius * 0.7f;
            source.strength = 0f;

            if (GameState.I.IsDone(Key)) SetLit(true, instant: true);
            if (dropsShard && GameState.I.IsDone(Key)) SpawnShard();
        }

        public override void Interact(ItemId held)
        {
            var gs = GameState.I;
            if (held == ItemId.None && gs.data.hasJar) held = ItemId.Jar;
            if (held != ItemId.Jar || !gs.UseFirefly()) { gs.Refuse(ItemId.Jar); return; }
            gs.MarkDone(Key);
            Effects.FlyTo(PlayerMover.Current.transform.position + Vector3.up * 1.2f, light2D.transform, 1, LightUtil.FireflyColor);
            StartCoroutine(LightUp());
        }

        IEnumerator LightUp()
        {
            yield return new WaitForSeconds(0.7f);
            AudioManager.Sfx("lantern");
            SetLit(true, instant: false);
            if (dropsShard) SpawnShard();
        }

        void SpawnShard()
        {
            Pickup.SpawnShard(transform.position + (Vector3)shardOffset, name + "_Shard", sr);
        }

        void SetLit(bool on, bool instant)
        {
            lit = on;
            if (sr) sr.sprite = Art.Sprite(on ? "lantern_on" : "lantern_off");
            if (instant) { light2D.intensity = on ? 1.2f : 0f; source.strength = on ? 1f : 0f; }
            else StartCoroutine(FadeLight());
        }

        IEnumerator FadeLight()
        {
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                light2D.intensity = Mathf.Lerp(0, 1.2f, t) * (1f + Random.Range(-0.15f, 0.15f) * (1f - t));
                source.strength = t;
                yield return null;
            }
            light2D.intensity = 1.2f;
            source.strength = 1f;
        }

        void Update()
        {
            if (lit) light2D.intensity = 1.2f + Mathf.Sin(Time.time * 5f + transform.position.x) * 0.06f;
        }
    }
}
