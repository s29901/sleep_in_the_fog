using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SITF
{
    public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public PuzzleController controller;
        public bool Snapped { get; private set; }
        RectTransform rt;
        Vector2 grabOffset;

        void Awake() { rt = (RectTransform)transform; }

        public void OnBeginDrag(PointerEventData e)
        {
            if (Snapped) return;
            rt.SetAsLastSibling();
            rt.localRotation = Quaternion.identity;          // при взятии осколок выпрямляется
            rt.localScale = Vector3.one * 1.05f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, null, out var local);
            grabOffset = rt.anchoredPosition - local;
            AudioManager.Sfx("paper");
        }

        public void OnDrag(PointerEventData e)
        {
            if (Snapped) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, null, out var local);
            rt.anchoredPosition = local + grabOffset;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (Snapped) return;
            rt.localScale = Vector3.one;
            if (rt.anchoredPosition.magnitude < PuzzleController.SnapDistance)
            {
                Snapped = true;
                StartCoroutine(SnapAnim());
            }
        }

        IEnumerator SnapAnim()
        {
            Vector2 from = rt.anchoredPosition;
            for (float t = 0; t < 0.15f; t += Time.deltaTime)
            {
                rt.anchoredPosition = Vector2.Lerp(from, Vector2.zero, t / 0.15f);
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
            var img = GetComponent<Image>();
            img.raycastTarget = false;
            controller.PieceSnapped();
        }
    }
}
