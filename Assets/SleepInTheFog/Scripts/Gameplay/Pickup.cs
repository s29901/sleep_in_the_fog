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
            Debug.Log("[SITF] Подобрано: " + name);
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
            col.size = new Vector2(1f, 1f);
            col.offset = new Vector2(0, 0.3f);
            col.isTrigger = true;
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
}
