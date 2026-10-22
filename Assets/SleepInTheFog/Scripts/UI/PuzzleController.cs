using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SITF
{
    /// Пазл: стол сверху, контур ночника, 6 осколков. Тянешь осколок мышкой — рядом с местом он защёлкивается.
    /// Каждый осколок — картинка размером с весь ночник (nightlight_shard_1…6), кусочек стоит на своём месте.
    public class PuzzleController : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        public const float SnapDistance = 40f;
        static readonly Vector2 NightlightSize = new Vector2(500, 600);

        readonly List<PuzzlePiece> pieces = new List<PuzzlePiece>();
        RectTransform board;
        CanvasGroup group;

        public static void Open()
        {
            if (IsOpen) return;
            var go = new GameObject("SITF_Puzzle");
            go.AddComponent<PuzzleController>();
        }

        void Awake()
        {
            IsOpen = true;
            PointerController.Blocked = true;
            if (PlayerMover.Current) { PlayerMover.Current.Stop(); PlayerMover.Current.Locked = true; }
            GameState.I.Select(ItemId.None);

            var canvas = UIFactory.Canvas("PuzzleCanvas", 200, transform);
            group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;

            var table = UIFactory.Image(canvas.transform, Art.Sprite("puzzle_table"), Vector2.zero, "Table");
            UIFactory.Stretch(table.rectTransform);
            table.raycastTarget = true;

            board = UIFactory.Rect(canvas.transform, "Board", NightlightSize);
            UIFactory.Anchor(board, new Vector2(0.5f, 0.5f), Vector2.zero);
            var outline = UIFactory.Image(board, Art.Sprite("nightlight_outline"), NightlightSize, "Outline");
            outline.color = new Color(1, 1, 1, 0.6f);

            // разбрасываем осколки вокруг, но не на сам контур
            var spots = new List<Vector2>
            {
                new Vector2(-620, 250), new Vector2(-640, -60), new Vector2(-560, -320),
                new Vector2(600, 260), new Vector2(650, -40), new Vector2(560, -310)
            };
            for (int i = 0; i < spots.Count; i++) { int j = Random.Range(i, spots.Count); (spots[i], spots[j]) = (spots[j], spots[i]); }

            for (int i = 1; i <= GameState.TotalShards; i++)
            {
                var sprite = Art.Sprite("nightlight_shard_" + i);
                if (sprite == null) continue;
                var img = UIFactory.Image(board, sprite, NightlightSize, "Shard" + i);
                img.raycastTarget = true;
                if (sprite.texture.isReadable) img.alphaHitTestMinimumThreshold = 0.1f;   // клик только по самому кусочку
                else Debug.LogWarning("[SITF] У " + sprite.name + " выключен Read/Write — кусочки будут хвататься по прямоугольнику");
                var piece = img.gameObject.AddComponent<PuzzlePiece>();
                piece.controller = this;
                // смещение так, чтобы сам кусочек (а не пустой холст) оказался в точке разброса
                Vector2 center = PieceCenter(sprite);
                img.rectTransform.anchoredPosition = spots[(i - 1) % spots.Count] - center;
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
                pieces.Add(piece);
            }
            StartCoroutine(FadeIn());
        }

        /// Центр непрозрачной части осколка относительно центра холста (в пикселях UI).
        static Vector2 PieceCenter(Sprite s)
        {
            var tex = s.texture;
            if (!tex.isReadable) return Vector2.zero;
            var px = tex.GetPixels32();
            long sx = 0, sy = 0, n = 0;
            for (int y = 0; y < tex.height; y += 4)
                for (int x = 0; x < tex.width; x += 4)
                    if (px[y * tex.width + x].a > 20) { sx += x; sy += y; n++; }
            if (n == 0) return Vector2.zero;
            var c = new Vector2(sx / (float)n / tex.width - 0.5f, sy / (float)n / tex.height - 0.5f);
            return new Vector2(c.x * NightlightSize.x, c.y * NightlightSize.y);
        }

        IEnumerator FadeIn()
        {
            for (float t = 0; t < 0.6f; t += Time.deltaTime) { group.alpha = t / 0.6f; yield return null; }
            group.alpha = 1;
        }

        public void PieceSnapped()
        {
            AudioManager.Sfx("snap");
            foreach (var p in pieces) if (!p.Snapped) return;
            StartCoroutine(Complete());
        }

        IEnumerator Complete()
        {
            yield return new WaitForSeconds(0.4f);
            AudioManager.Sfx("complete");
            var whole = UIFactory.Image(board, Art.Sprite("nightlight_on"), NightlightSize, "NightlightOn");
            whole.color = new Color(1, 1, 1, 0);
            var glow = UIFactory.Image(board, Art.Sprite("glow"), NightlightSize * 2.2f, "Glow");
            glow.transform.SetAsFirstSibling();
            glow.color = Palette.WithAlpha(Palette.Apricot, 0);
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                float k = t / 1.5f;
                whole.color = new Color(1, 1, 1, k);
                glow.color = Palette.WithAlpha(Palette.Apricot, k * 0.8f);
                board.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.05f);
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            GameState.ClearSave();
            GameFlow.Load(Scenes.Final, false);
            yield return new WaitForSeconds(1f);
            IsOpen = false;
            PointerController.Blocked = false;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            IsOpen = false;
            PointerController.Blocked = false;
        }
    }
}
