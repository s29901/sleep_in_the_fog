using System;
using System.Collections.Generic;
using UnityEngine;

namespace SITF
{
    /// Состояние игры: инвентарь, светлячки, осколки, флаги объектов. Живёт всю игру.
    public class GameState : MonoBehaviour
    {
        public const int MaxFireflies = 6;
        public const int TotalShards = 6;
        const string SaveKey = "sitf_save";

        public static GameState I { get; private set; }

        public SaveData data = new SaveData();
        public ItemId Selected { get; private set; } = ItemId.None;

        /// Вызывается при любом изменении инвентаря/выбора.
        public event Action Changed;
        /// Вызывается, когда пытались сделать что-то, чего нельзя (предмет «трясётся» в инвентаре).
        public event Action<ItemId> Refused;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
        }

        // ---------- Инвентарь ----------
        public bool JarHasLight => data.hasJar && data.lidOnJar && data.fireflies > 0;

        public List<ItemId> Items()
        {
            var list = new List<ItemId>();
            if (data.hasJar) list.Add(ItemId.Jar);
            if (data.hasLid && !data.lidOnJar) list.Add(ItemId.Lid);
            if (data.shards > 0) list.Add(ItemId.Shard);
            return list;
        }

        public void AddItem(ItemId id)
        {
            switch (id)
            {
                case ItemId.Jar: data.hasJar = true; break;
                case ItemId.Lid: data.hasLid = true; break;
                case ItemId.Shard: data.shards = Mathf.Min(TotalShards, data.shards + 1); break;
            }
            AudioManager.Sfx("pickup");
            Notify();
        }

        public int AddFireflies(int n)
        {
            int added = Mathf.Clamp(n, 0, MaxFireflies - data.fireflies);
            data.fireflies += added;
            Notify();
            return added;
        }

        public bool UseFirefly()
        {
            if (!JarHasLight) return false;
            data.fireflies--;
            Notify();
            return true;
        }

        public void Select(ItemId id)
        {
            Selected = id;
            Notify();
        }

        /// Клик по предмету в инвентаре: выбрать / снять выбор / соединить крышку с банкой.
        public void ClickInventoryItem(ItemId id)
        {
            if ((Selected == ItemId.Lid && id == ItemId.Jar) || (Selected == ItemId.Jar && id == ItemId.Lid))
            {
                data.lidOnJar = true;
                AudioManager.Sfx("combine");
                Selected = ItemId.None;
                Notify();
                return;
            }
            Select(Selected == id ? ItemId.None : id);
        }

        public void Refuse(ItemId id)
        {
            AudioManager.Sfx("refuse");
            Refused?.Invoke(id);
        }

        public void Notify() => Changed?.Invoke();

        // ---------- Флаги объектов ----------
        public bool IsDone(string key) => data.flags.Contains(key);
        public void MarkDone(string key) { if (!data.flags.Contains(key)) data.flags.Add(key); }

        public int GetInt(string key, int def)
        {
            string p = key + "=";
            foreach (var s in data.ints)
                if (s.StartsWith(p) && int.TryParse(s.Substring(p.Length), out int v)) return v;
            return def;
        }

        public void SetInt(string key, int value)
        {
            string p = key + "=";
            data.ints.RemoveAll(s => s.StartsWith(p));
            data.ints.Add(p + value);
        }

        // ---------- Сохранение ----------
        public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

        public void Save(string scene)
        {
            data.scene = scene;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool Load()
        {
            if (!HasSave) return false;
            data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey)) ?? new SaveData();
            Selected = ItemId.None;
            Notify();
            return true;
        }

        public void ResetGame()
        {
            data = new SaveData();
            Selected = ItemId.None;
            Notify();
        }

        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
