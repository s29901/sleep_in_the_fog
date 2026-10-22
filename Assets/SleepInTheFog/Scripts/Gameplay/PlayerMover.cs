using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SITF
{
    /// Героиня идёт туда, куда кликнули. Ставится инструментом вместо старых скриптов движения.
    public class PlayerMover : MonoBehaviour
    {
        public static PlayerMover Current { get; private set; }

        public float speed = 3f;
        [Tooltip("Где героиня может ходить (мир, в юнитах)")]
        public Vector2 minBounds = new Vector2(-9, -5), maxBounds = new Vector2(27, -3);

        /// Невидимая стена: дальше этого X героиня не пройдёт (туман у выхода).
        [NonSerialized] public float blockMaxX = float.PositiveInfinity;
        [NonSerialized] public float blockMinX = float.NegativeInfinity;
        public bool Locked { get; set; }

        Animator anim;
        SpriteRenderer sr;
        Rigidbody2D rb;
        Vector2? target;
        float stuckTime;
        Vector2 lastPos;
        Action onArrive;
        bool hasIsRun, hasSpeed;

        void Awake()
        {
            Current = this;
            anim = GetComponentInChildren<Animator>();
            sr = GetComponentInChildren<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            // героиня ходит по клику и не должна упираться в предметы — физика ей не нужна
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
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
            p.x = Mathf.Clamp(p.x, Mathf.Max(minBounds.x, blockMinX), Mathf.Min(maxBounds.x, blockMaxX));
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

        /// Мгновенно поставить героиню в точку (при входе в сцену).
        public void Teleport(Vector2 point, float faceDir)
        {
            point = Clamp(point);
            Stop();
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            if (rb) rb.position = point;
            Face(faceDir);
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
            if ((pos.x > blockMaxX + 0.01f || pos.x < blockMinX - 0.01f) && target == null) target = Clamp(pos);

            if (target == null) { SetAnim(0); return; }
            Vector2 t = Clamp(target.Value);
            Vector2 next = Vector2.MoveTowards(pos, t, speed * dt);
            Face(t.x - pos.x);
            if (rb) rb.MovePosition(next); else transform.position = new Vector3(next.x, next.y, transform.position.z);
            SetAnim(speed);

            // если что-то не пускает дальше — считаем, что пришла (чтобы клик не «терялся»)
            if ((pos - lastPos).sqrMagnitude < 0.000001f) stuckTime += dt; else stuckTime = 0f;
            lastPos = pos;

            if ((next - t).sqrMagnitude < 0.0004f || stuckTime > 0.4f)
            {
                stuckTime = 0f;
                var cb = onArrive;
                target = null;
                onArrive = null;
                SetAnim(0);
                cb?.Invoke();
            }
        }
    }
}
