using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
    /// Интерфейс во время игры: инвентарь внизу, иконка предмета у курсора, кнопка паузы, меню паузы и настройки.
    public class GameUI : MonoBehaviour
    {
        public static GameUI I { get; private set; }

        Canvas canvas;
        RectTransform bar;
        CanvasGroup barGroup;
        readonly List<SlotView> slots = new List<SlotView>();
        Image held;
        Button pauseButton;
        GameObject pausePanel, settingsPanel;
        float showTimer;
        float barShown;
        bool gameplay;

        class SlotView
        {
            public Image bg, icon;
            public Text count;
            public Button button;
            public ItemId item;
            public Vector2 basePos;
        }

        void Awake()
        {
            I = this;
            canvas = UIFactory.Canvas("SITF_GameUI", 100, transform);
            BuildInventory();
            BuildHeld();
            BuildPause();
            settingsPanel = BuildSettings(canvas.transform, () => { settingsPanel.SetActive(false); pausePanel.SetActive(true); });
            settingsPanel.SetActive(false);

            SceneManager.sceneLoaded += (s, m) => OnScene();
            OnScene();
        }

        void Start()
        {
            GameState.I.Changed += Refresh;
            GameState.I.Refused += Shake;
            Refresh();
        }

        void OnScene()
        {
            gameplay = FindObjectOfType<PlayerMover>() != null;
            canvas.gameObject.SetActive(gameplay);
            SetPaused(false);
        }

        // ---------- Инвентарь ----------
        void BuildInventory()
        {
            bar = UIFactory.Rect(canvas.transform, "Inventory", new Vector2(640, 160));
            UIFactory.Anchor(bar, new Vector2(0.5f, 1f), new Vector2(0, -95));   // сверху: внизу ходит героиня и лежат предметы
            var bg = UIFactory.Image(bar, Art.Sprite("ui_inventory_bar"), new Vector2(640, 160), "Bar");
            UIFactory.Stretch(bg.rectTransform);
            barGroup = bar.gameObject.AddComponent<CanvasGroup>();

            for (int i = 0; i < 4; i++)
            {
                var s = new SlotView();
                s.bg = UIFactory.Image(bar, Art.Sprite("ui_slot"), new Vector2(120, 120), "Slot" + i);
                s.basePos = new Vector2(-225 + i * 150, 0);
                UIFactory.Anchor(s.bg.rectTransform, new Vector2(0.5f, 0.5f), s.basePos);
                s.bg.raycastTarget = true;
                s.icon = UIFactory.Image(s.bg.transform, null, new Vector2(96, 96), "Icon");
                s.icon.preserveAspect = true;
                s.count = UIFactory.Label(s.bg.transform, "", 28, new Vector2(110, 40), TextAnchor.LowerRight);
                UIFactory.Anchor(s.count.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 22));
                s.button = s.bg.gameObject.AddComponent<Button>();
                s.button.transition = Selectable.Transition.None;
                var sv = s;
                s.button.onClick.AddListener(() => { if (sv.item != ItemId.None) GameState.I.ClickInventoryItem(sv.item); });
                slots.Add(s);
            }
        }

        void BuildHeld()
        {
            held = UIFactory.Image(canvas.transform, null, new Vector2(64, 64), "HeldItem");
            held.preserveAspect = true;
            held.rectTransform.anchorMin = held.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            held.rectTransform.pivot = new Vector2(0f, 1f);
            held.enabled = false;
        }

        public static string IconFor(ItemId id)
        {
            var d = GameState.I.data;
            switch (id)
            {
                case ItemId.Jar: return d.fireflies == 0 ? "icon_jar_empty" : d.fireflies <= 3 ? "icon_jar_half" : "icon_jar_full";
                case ItemId.Lid: return "icon_lid";
                case ItemId.Shard: return "icon_shards";
            }
            return null;
        }

        void Refresh()
        {
            var items = GameState.I.Items();
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                s.item = i < items.Count ? items[i] : ItemId.None;
                s.icon.enabled = s.item != ItemId.None;
                s.bg.raycastTarget = s.item != ItemId.None;   // пустая ячейка не перехватывает клики
                if (s.icon.enabled) s.icon.sprite = Art.Sprite(IconFor(s.item));
                s.count.text = s.item == ItemId.Shard ? GameState.I.data.shards + "/" + GameState.TotalShards
                             : s.item == ItemId.Jar && GameState.I.data.fireflies > 0 ? GameState.I.data.fireflies.ToString() : "";
                bool sel = s.item != ItemId.None && s.item == GameState.I.Selected;
                s.bg.sprite = Art.Sprite(sel ? "ui_slot_selected" : "ui_slot");
                // банка без крышки — чуть прозрачнее, пока крышка не надета
                s.icon.color = s.item == ItemId.Jar && !GameState.I.data.lidOnJar ? new Color(1, 1, 1, 0.75f) : Color.white;
            }
            var sel2 = GameState.I.Selected;
            held.enabled = sel2 != ItemId.None;
            if (held.enabled) held.sprite = Art.Sprite(IconFor(sel2));
            showTimer = 3f;
        }

        void Shake(ItemId id)
        {
            foreach (var s in slots)
                if (s.item == id) StartCoroutine(ShakeRoutine(s));
            showTimer = 2f;
        }

        IEnumerator ShakeRoutine(SlotView s)
        {
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                s.bg.rectTransform.anchoredPosition = s.basePos + new Vector2(Mathf.Sin(t * 60f) * 10f * (1f - t / 0.4f), 0);
                yield return null;
            }
            s.bg.rectTransform.anchoredPosition = s.basePos;
        }

        void Update()
        {
            if (!gameplay) return;

            if (Input.GetKeyDown(KeyCode.Escape) && !PuzzleController.IsOpen && !GameFlow.Busy)
                SetPaused(!pausePanel.activeSelf && !settingsPanel.activeSelf);

            // инвентарь появляется у нижнего края экрана, после изменений или когда предмет выбран
            showTimer -= Time.unscaledDeltaTime;
            bool nearTop = Input.mousePosition.y > Screen.height * 0.85f;
            bool show = (nearTop || showTimer > 0 || GameState.I.Selected != ItemId.None) && !PuzzleController.IsOpen && GameState.I.Items().Count > 0;
            barShown = Mathf.MoveTowards(barShown, show ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            barGroup.alpha = barShown;
            barGroup.blocksRaycasts = barShown > 0.5f;
            bar.anchoredPosition = new Vector2(0, Mathf.Lerp(90, -95, Mathf.SmoothStep(0, 1, barShown)));

            if (held.enabled)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, Input.mousePosition, null, out var local);
                held.rectTransform.anchoredPosition = local + new Vector2(24, -24);
            }
            pauseButton.gameObject.SetActive(!PuzzleController.IsOpen);
        }

        // ---------- Пауза ----------
        void BuildPause()
        {
            var pimg = UIFactory.Image(canvas.transform, Art.Sprite("ui_pause"), new Vector2(64, 64), "PauseButton");
            UIFactory.Anchor(pimg.rectTransform, new Vector2(1, 1), new Vector2(-56, -56));
            pimg.raycastTarget = true;
            pimg.color = new Color(1, 1, 1, 0.6f);
            pauseButton = pimg.gameObject.AddComponent<Button>();
            pauseButton.onClick.AddListener(() => SetPaused(true));

            pausePanel = Panel(canvas.transform, "PausePanel");
            var col = UIFactory.Column(pausePanel.transform, 30);
            UIFactory.Anchor(col, new Vector2(0.5f, 0.5f), Vector2.zero);
            UIFactory.Button(col, "Resume", () => SetPaused(false));
            UIFactory.Button(col, "Settings", () => { pausePanel.SetActive(false); settingsPanel.SetActive(true); });
            UIFactory.Button(col, "Main menu", () => { SetPaused(false); GameFlow.ToMenu(); });
            pausePanel.SetActive(false);
        }

        public static GameObject Panel(Transform parent, string name)
        {
            var dim = UIFactory.Image(parent, null, Vector2.zero, name);
            UIFactory.Stretch(dim.rectTransform);
            dim.color = Palette.WithAlpha(Palette.Ink, 0.75f);
            dim.raycastTarget = true;
            var panel = UIFactory.Image(dim.transform, Art.Sprite("ui_panel"), new Vector2(700, 600), "Panel");
            UIFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
            return dim.gameObject;
        }

        /// Окно настроек: музыка, звуки, полный экран. Используется и в паузе, и в главном меню.
        public static GameObject BuildSettings(Transform parent, UnityEngine.Events.UnityAction onBack)
        {
            var root = Panel(parent, "SettingsPanel");
            var col = UIFactory.Column(root.transform, 24);
            UIFactory.Anchor(col, new Vector2(0.5f, 0.5f), Vector2.zero);
            UIFactory.Label(col, "Settings", 48, new Vector2(600, 80));
            UIFactory.Slider(col, "Music", AudioManager.MusicVolume, v => AudioManager.MusicVolume = v);
            UIFactory.Slider(col, "Sounds", AudioManager.SfxVolume, v => AudioManager.SfxVolume = v);
            UIFactory.Toggle(col, "Fullscreen", Screen.fullScreen, v => Screen.fullScreen = v);
            UIFactory.Button(col, "Back", onBack);
            return root;
        }

        public void SetPaused(bool on)
        {
            if (pausePanel == null) return;
            pausePanel.SetActive(on);
            if (!on && settingsPanel) settingsPanel.SetActive(false);
            Time.timeScale = on ? 0f : 1f;
        }
    }
}
