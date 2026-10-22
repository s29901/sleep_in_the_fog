using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Предмет на земле: банка, крышка, осколок. Клик — подобрать.
    public class Pickup : Interactable
    {
        public ItemId item = ItemId.Jar;

        public override CursorKind HoverCursor => CursorKind.Grab;

        void Start()
        {
            if (GameState.I != null && GameState.I.IsDone(Key)) Destroy(gameObject);
        }

        public override void Interact(ItemId held)
        {
            GameState.I.MarkDone(Key);
            GameState.I.AddItem(item);
            StartCoroutine(Vanish());
        }

        IEnumerator Vanish()
        {
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            var sr = GetComponentInChildren<SpriteRenderer>();
            Vector3 s = transform.localScale;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                transform.localScale = s * (1f + k * 0.3f);
                if (sr) { var c = sr.color; c.a = 1f - k; sr.color = c; }
                yield return null;
            }
            Destroy(gameObject);
        }

        /// Создать осколок в мире (после качелей или фонаря). key — чтобы помнить, что он уже подобран.
        public static Pickup SpawnShard(Vector3 pos, string name, SpriteRenderer reference)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Sprite("shard_world");
            if (reference) { sr.sortingLayerID = reference.sortingLayerID; sr.sortingOrder = reference.sortingOrder + 1; }
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.6f);
            col.offset = new Vector2(0, 0.2f);
            var p = go.AddComponent<Pickup>();
            p.item = ItemId.Shard;
            p.interactDistance = 0.4f;
            go.AddComponent<Bob>();
            if (GameState.I.IsDone(p.Key)) { Destroy(go); return null; }
            return p;
        }

        /// Лёгкое покачивание вверх-вниз, чтобы осколок было заметно.
        public class Bob : MonoBehaviour
        {
            Vector3 start; float phase;
            void Start() { start = transform.position; phase = Random.value * 6f; }
            void Update() { transform.position = start + Vector3.up * (Mathf.Sin(Time.time * 2f + phase) * 0.06f); }
        }
    }

    /// Рой светлячков. Клик с банкой (с крышкой!) — поймать.
    public class FireflySwarm : Interactable
    {
        public int count = 3;
        public FireflyParticles particles;
        int left;

        public override bool CanHover => left > 0;

        void Start()
        {
            left = GameState.I.GetInt(Key, count);
            if (!particles) particles = GetComponentInChildren<FireflyParticles>();
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (particles) particles.SetRate(left > 0 ? 1.5f + left * 1.2f : 0.3f);
        }

        public override void Interact(ItemId held)
        {
            var gs = GameState.I;
            if (held == ItemId.None && gs.data.hasJar) held = ItemId.Jar;   // банка используется сама, если ничего не выбрано
            if (held != ItemId.Jar || !gs.data.hasJar)
            {
                // без банки светлячков не поймать — они разлетаются
                if (particles) particles.Burst(6);
                if (gs.data.hasJar) gs.Refuse(ItemId.Jar);
                return;
            }
            if (!gs.data.lidOnJar)
            {
                // без крышки — улетают обратно
                if (particles) particles.Burst(8);
                gs.Refuse(ItemId.Jar);
                return;
            }
            int caught = gs.AddFireflies(left);
            if (caught <= 0) { gs.Refuse(ItemId.Jar); return; }
            left -= caught;
            gs.SetInt(Key, left);
            AudioManager.Sfx("catch");
            Effects.FlyTo(transform.position, PlayerMover.Current.transform, caught, LightUtil.FireflyColor);
            UpdateVisual();
        }
    }

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
            csr.color = new Color(1, 0.95f, 0.8f, 0);
            var light = LightUtil.AddPointLight(child, new Color(1f, 0.9f, 0.7f), 3f, 0f);

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
                csr.color = new Color(1, 0.95f, 0.8f, a * 0.85f);
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
                Pickup.SpawnShard(new Vector3(x, y, 0), name + "_Shard" + i, sr);
            }
        }
    }

    /// Выход в следующую сцену. Пока нет света — туман не пускает (невидимая стена).
    public class SceneExit : Interactable
    {
        public string targetScene = Scenes.Path;
        public bool needsLight = true;
        public float wallOffset = 1.2f;
        bool loading;

        bool Open => !needsLight || GameState.I.JarHasLight;

        public override bool CanHover => true;

        void Update()
        {
            var p = PlayerMover.Current;
            if (!p) return;
            p.blockMaxX = Open ? float.PositiveInfinity : transform.position.x - wallOffset;
            if (Open && !loading && p.transform.position.x >= transform.position.x - 0.3f) Go();
        }

        public override void Interact(ItemId held)
        {
            if (!Open) { GameState.I.Refuse(ItemId.Jar); return; }
            PlayerMover.Current.WalkTo(transform.position + Vector3.right);
        }

        void Go()
        {
            loading = true;
            GameFlow.Load(targetScene);
        }
    }

    /// Дом в конце пути: когда собраны все осколки — открывается пазл.
    public class FinalPoint : Interactable
    {
        public SpriteRenderer windowOn;   // тёплое окно, загорается когда все осколки собраны
        Light2D light2D;

        public override bool CanHover => GameState.I.data.shards >= GameState.TotalShards && !PuzzleController.IsOpen;

        void Start()
        {
            var lgo = new GameObject("WindowLight");
            lgo.transform.SetParent(transform, false);
            light2D = LightUtil.AddPointLight(lgo, new Color(1f, 0.8f, 0.5f), 4f, 0f);
        }

        void Update()
        {
            bool ready = GameState.I.data.shards >= GameState.TotalShards;
            float target = ready ? 1f : 0f;
            light2D.intensity = Mathf.MoveTowards(light2D.intensity, target, Time.deltaTime);
            if (windowOn) { var c = windowOn.color; c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime); windowOn.color = c; }
        }

        public override void Interact(ItemId held)
        {
            if (GameState.I.data.shards < GameState.TotalShards) return;
            PuzzleController.Open();
        }
    }
}
