using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SITF
{
    public enum CursorKind { Default, Interact, Grab }

    /// Курсоры cursor_default / cursor_interact / cursor_grab, точка клика — в центре.
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager I { get; private set; }
        CursorKind current = (CursorKind)(-1);

        void Awake() { I = this; Set(CursorKind.Default); }

        public void Set(CursorKind kind)
        {
            if (kind == current) return;
            current = kind;
            string name = kind == CursorKind.Interact ? "cursor_interact" : kind == CursorKind.Grab ? "cursor_grab" : "cursor_default";
            var tex = Art.Texture(name);
            if (tex == null && kind == CursorKind.Grab) tex = Art.Texture("cursor_interact");
            if (tex == null) { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); return; }
            Cursor.SetCursor(tex, new Vector2(tex.width / 2f, tex.height / 2f), CursorMode.Auto);
        }
    }

    /// Любой объект, по которому можно кликнуть. Нужен Collider2D.
    [RequireComponent(typeof(Collider2D))]
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("На каком расстоянии по X героиня останавливается перед объектом")]
        public float interactDistance = 1.0f;

        /// Уникальный ключ объекта для сохранения: "Сцена/Имя".
        protected string Key => gameObject.scene.name + "/" + name;

        public virtual bool CanHover => true;
        public virtual CursorKind HoverCursor => CursorKind.Interact;
        public abstract void Interact(ItemId held);

        /// Порядок отрисовки — чтобы при наложении выбирался верхний объект.
        public int SortingOrder
        {
            get
            {
                var sr = GetComponentInChildren<SpriteRenderer>();
                return sr ? sr.sortingOrder : 0;
            }
        }
    }

    /// Мышь: наведение (смена курсора) и клик (идти / взаимодействовать). Правый клик — снять выбранный предмет.
    public class PointerController : MonoBehaviour
    {
        public static bool Blocked;     // пазл, пауза, катсцена
        Interactable hovered;
        readonly List<Collider2D> hits = new List<Collider2D>();

        void Update()
        {
            var player = PlayerMover.Current;
            var cam = Camera.main;
            if (player == null || cam == null || Blocked || GameFlow.Busy || Time.timeScale == 0f)
            {
                hovered = null;
                CursorManager.I.Set(CursorKind.Default);
                return;
            }

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
            hovered = overUI ? null : Pick(world);
            CursorManager.I.Set(hovered ? hovered.HoverCursor : CursorKind.Default);

            if (Input.GetMouseButtonDown(1)) GameState.I.Select(ItemId.None);
            if (!Input.GetMouseButtonDown(0) || overUI || player.Locked) return;

            if (hovered)
            {
                var target = hovered;
                var held = GameState.I.Selected;
                player.WalkToObject(target, () =>
                {
                    if (target) target.Interact(held);
                });
            }
            else
            {
                player.WalkTo(world);
            }
        }

        Interactable Pick(Vector2 world)
        {
            hits.Clear();
            var filter = new ContactFilter2D { useTriggers = true };
            filter.NoFilter();
            Physics2D.OverlapPoint(world, filter, hits);
            Interactable best = null;
            foreach (var h in hits)
            {
                var it = h.GetComponentInParent<Interactable>();
                if (it == null || !it.isActiveAndEnabled || !it.CanHover) continue;
                if (best == null || it.SortingOrder > best.SortingOrder) best = it;
            }
            return best;
        }
    }

    /// Героиня идёт туда, куда кликнули. Ставится инструментом вместо старых скриптов движения.
    public class PlayerMover : MonoBehaviour
    {
        public static PlayerMover Current { get; private set; }

        public float speed = 3f;
        [Tooltip("Где героиня может ходить (мир, в юнитах)")]
        public Vector2 minBounds = new Vector2(-9, -5), maxBounds = new Vector2(27, -3);

        /// Невидимая стена: дальше этого X героиня не пройдёт (туман у выхода).
        [NonSerialized] public float blockMaxX = float.PositiveInfinity;
        public bool Locked { get; set; }

        Animator anim;
        SpriteRenderer sr;
        Rigidbody2D rb;
        Vector2? target;
        Action onArrive;
        bool hasIsRun, hasSpeed;

        void Awake()
        {
            Current = this;
            anim = GetComponentInChildren<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            if (anim && anim.runtimeAnimatorController)
                foreach (var p in anim.parameters)
                {
                    if (p.name == "IsRun" && p.type == AnimatorControllerParameterType.Bool) hasIsRun = true;
                    if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                }
        }

        void OnDestroy() { if (Current == this) Current = null; }

        Vector2 Clamp(Vector2 p)
        {
            p.x = Mathf.Clamp(p.x, minBounds.x, Mathf.Min(maxBounds.x, blockMaxX));
            p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
            return p;
        }

        public void WalkTo(Vector2 point, Action arrive = null)
        {
            if (Locked) return;
            target = Clamp(point);
            onArrive = arrive;
        }

        /// Подойти к объекту так, чтобы он был на расстоянии interactDistance по X.
        public void WalkToObject(Interactable obj, Action arrive)
        {
            Vector2 me = transform.position;
            Vector2 p = obj.transform.position;
            float side = me.x <= p.x ? -1f : 1f;
            float stopX = p.x + side * obj.interactDistance;
            if (Mathf.Abs(me.x - p.x) <= obj.interactDistance) stopX = me.x;   // уже рядом
            float y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
            if (y > maxBounds.y || y < minBounds.y) y = me.y;
            WalkTo(new Vector2(stopX, y), () => { Face(p.x - transform.position.x); arrive?.Invoke(); });
        }

        public void Stop()
        {
            target = null;
            onArrive = null;
            SetAnim(0);
        }

        void Face(float dx)
        {
            if (sr && Mathf.Abs(dx) > 0.01f) sr.flipX = dx < 0;
        }

        void SetAnim(float v)
        {
            if (!anim) return;
            if (hasIsRun) anim.SetBool("IsRun", v > 0.01f);
            if (hasSpeed) anim.SetFloat("Speed", v);
        }

        void Update()
        {
            if (rb == null) Step(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (rb != null) Step(Time.fixedDeltaTime);
        }

        void Step(float dt)
        {
            Vector2 pos = rb ? rb.position : (Vector2)transform.position;

            // если стена тумана сдвинулась ближе — не стоим внутри неё
            if (pos.x > blockMaxX + 0.01f && target == null) target = Clamp(pos);

            if (target == null) { SetAnim(0); return; }
            Vector2 t = Clamp(target.Value);
            Vector2 next = Vector2.MoveTowards(pos, t, speed * dt);
            Face(t.x - pos.x);
            if (rb) rb.MovePosition(next); else transform.position = new Vector3(next.x, next.y, transform.position.z);
            SetAnim(speed);

            if ((next - t).sqrMagnitude < 0.0004f)
            {
                var cb = onArrive;
                target = null;
                onArrive = null;
                SetAnim(0);
                cb?.Invoke();
            }
        }
    }
}
