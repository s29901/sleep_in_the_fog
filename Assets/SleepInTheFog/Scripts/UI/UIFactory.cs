using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SITF
{
    /// Собирает интерфейс из кода, чтобы не настраивать вручную префабы.
    /// Все размеры — для экрана 1920×1080 (как в документе «что нарисовать для UI»).
    public static class UIFactory
    {
        public static readonly Color TextColor = Palette.Milk;
        public static readonly Color Warm = Palette.Apricot;

        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent) go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(Transform parent, Sprite sprite, Vector2 size, string name = "Image")
        {
            var rt = Rect(parent, name, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Vector2 box, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, "Text", box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = TextColor;
            t.raycastTarget = false;
            return t;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        /// Кнопка 360×100 с подложкой ui_button / ui_button_hover и надписью.
        public static Button Button(Transform parent, string label, UnityAction onClick, Vector2? size = null)
        {
            var img = Image(parent, Art.Sprite("ui_button"), size ?? new Vector2(360, 100), "Button " + label);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = Art.Sprite("ui_button_hover"),
                selectedSprite = Art.Sprite("ui_button"),
                pressedSprite = Art.Sprite("ui_button_hover")
            };
            b.onClick.AddListener(() => AudioManager.Sfx("click"));
            b.onClick.AddListener(onClick);
            var t = Label(img.transform, label, 40, img.rectTransform.sizeDelta);
            Stretch(t.rectTransform);
            return b;
        }

        public static Slider Slider(Transform parent, string label, float value, UnityAction<float> onChange)
        {
            var row = Rect(parent, "Row " + label, new Vector2(700, 80));
            var l = Label(row, label, 34, new Vector2(260, 80), TextAnchor.MiddleLeft);
            Anchor(l.rectTransform, new Vector2(0, 0.5f), new Vector2(130, 0));

            var bar = Image(row, Art.Sprite("ui_slider_bar"), new Vector2(400, 24), "Slider");
            Anchor(bar.rectTransform, new Vector2(1, 0.5f), new Vector2(-200, 0));
            bar.raycastTarget = true;
            var slider = bar.gameObject.AddComponent<Slider>();

            var area = Rect(bar.transform, "Handle Area", Vector2.zero);
            Stretch(area);
            area.offsetMin = new Vector2(20, 0);
            area.offsetMax = new Vector2(-20, 0);
            var knob = Image(area, Art.Sprite("ui_slider_knob"), new Vector2(40, 40), "Knob");
            knob.raycastTarget = true;

            slider.targetGraphic = knob;
            slider.handleRect = knob.rectTransform;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        public static Toggle Toggle(Transform parent, string label, bool value, UnityAction<bool> onChange)
        {
            var row = Rect(parent, "Row " + label, new Vector2(700, 80));
            var l = Label(row, label, 34, new Vector2(400, 80), TextAnchor.MiddleLeft);
            Anchor(l.rectTransform, new Vector2(0, 0.5f), new Vector2(200, 0));

            var bg = Image(row, Art.Sprite("ui_slot"), new Vector2(56, 56), "Toggle");
            Anchor(bg.rectTransform, new Vector2(1, 0.5f), new Vector2(-28, 0));
            bg.raycastTarget = true;
            var check = Image(bg.transform, Art.Sprite("ui_slider_knob"), new Vector2(34, 34), "Check");
            var tg = bg.gameObject.AddComponent<Toggle>();
            tg.targetGraphic = bg;
            tg.graphic = check;
            tg.isOn = value;
            tg.onValueChanged.AddListener(onChange);
            return tg;
        }

        /// Вертикальная колонка с отступами (для кнопок меню).
        public static RectTransform Column(Transform parent, float spacing)
        {
            var rt = Rect(parent, "Column", new Vector2(800, 800));
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = false;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            return rt;
        }

        /// Плавное покачивание (кнопки «плавают», как во сне).
        public class Floater : MonoBehaviour
        {
            public float amplitude = 8f, speed = 0.8f, phase;
            RectTransform rt;
            Vector2 basePos;
            bool ready;

            void Start() { rt = (RectTransform)transform; }

            void LateUpdate()
            {
                if (!ready) { basePos = rt.anchoredPosition; ready = true; }
                float t = Time.unscaledTime * speed + phase;
                rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 0.7f) * amplitude * 0.6f, Mathf.Sin(t) * amplitude);
            }
        }

        /// Пропускает клики сквозь объект (например, иконку у курсора).
        public static void NoRaycast(GameObject go)
        {
            foreach (var g in go.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }
    }
}
