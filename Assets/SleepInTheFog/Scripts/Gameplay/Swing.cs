using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Качели: видны только в свете. Клик — воспоминание (качаются сами, светящийся силуэт ребёнка), потом осколки.
    public class Swing : Interactable
    {
        public Transform pivot;              // верх верёвок, вокруг него качается
        public int shards = 3;
        public float memorySeconds = 4f;
        bool busy;

        public override bool CanHover => !busy && !GameState.I.IsDone(Key);

        void Start()
        {
            if (GameState.I.IsDone(Key))
            {
                var r = GetComponent<RevealByLight>();
                if (r) r.ForceReveal();
                SpawnShards();
            }
        }

        public override void Interact(ItemId held)
        {
            if (busy || GameState.I.IsDone(Key)) return;
            Debug.Log("[SITF] Качели: воспоминание");
            StartCoroutine(Memory());
        }

        IEnumerator Memory()
        {
            busy = true;
            var player = PlayerMover.Current;
            player.Stop();
            player.Locked = true;
            PointerController.Blocked = true;
            AudioManager.Sfx("memory");

            // светящийся силуэт ребёнка
            var child = new GameObject("MemoryChild");
            child.transform.SetParent(pivot ? pivot : transform, false);
            child.transform.localPosition = new Vector3(0, -3.6f, 0);
            var csr = child.AddComponent<SpriteRenderer>();
            csr.sprite = Art.Sprite("child_glow");
            var mat = Resources.Load<Material>("SITF/GlowMaterial");
            if (mat) csr.sharedMaterial = mat;
            csr.sortingOrder = 55;
            csr.color = Palette.WithAlpha(Palette.Dusty, 0);
            var light = LightUtil.AddPointLight(child, Palette.Dusty, 3f, 0f);

            var fx = new GameObject("MemoryFireflies");
            fx.transform.position = transform.position + Vector3.up * 1.5f;
            var ps = fx.AddComponent<ParticleSystem>();
            FireflyParticles.Configure(ps, 1.8f, 14f);

            float t = 0;
            while (t < memorySeconds)
            {
                t += Time.deltaTime;
                float fadeIn = Mathf.Clamp01(t / 1f), fadeOut = Mathf.Clamp01((memorySeconds - t) / 1f);
                float a = Mathf.Min(fadeIn, fadeOut);
                csr.color = Palette.WithAlpha(Palette.Dusty, a * 0.85f);
                light.intensity = a * 0.9f;
                if (pivot) pivot.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.4f) * 22f * a + Mathf.Sin(t * 2.4f) * 4f);
                yield return null;
            }
            if (pivot) pivot.localRotation = Quaternion.identity;
            Destroy(child);
            ps.Stop();
            Destroy(fx, 6f);

            GameState.I.MarkDone(Key);
            SpawnShards();
            player.Locked = false;
            PointerController.Blocked = false;
            busy = false;
        }

        void SpawnShards()
        {
            var sr = PlayerMover.Current ? PlayerMover.Current.GetComponentInChildren<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            float y = PlayerMover.Current ? PlayerMover.Current.transform.position.y : transform.position.y;
            for (int i = 0; i < shards; i++)
            {
                float x = transform.position.x + (i - (shards - 1) / 2f) * 1.2f;
                Pickup.SpawnShard(new Vector3(x, y + 0.3f, 0), name + "_Shard" + i, sr);
            }
        }
    }
}
