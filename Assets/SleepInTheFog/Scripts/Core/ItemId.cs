using System;
using System.Collections.Generic;
using UnityEngine;

namespace SITF
{
    public enum ItemId { None, Jar, Lid, Shard }

    /// Всё, что сохраняется между сценами и в автосейве.
    [Serializable]
    public class SaveData
    {
        public string scene = "";
        public bool hasJar;
        public bool hasLid;
        public bool lidOnJar;
        public int fireflies;
        public int shards;
        public List<string> flags = new List<string>();   // "Scene/ObjectName" = сделано (подобрано, зажжено…)
        public List<string> ints = new List<string>();    // "key=value"
    }
}
