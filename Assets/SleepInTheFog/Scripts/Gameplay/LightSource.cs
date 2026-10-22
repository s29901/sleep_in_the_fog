using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Источник света, который разгоняет туман и проявляет скрытые объекты.
    public class LightSource : MonoBehaviour
    {
        public static readonly List<LightSource> All = new List<LightSource>();
        [Tooltip("Радиус, в котором туман полностью рассеян (юниты)")]
        public float radius = 2f;
        [Range(0, 1)] public float strength = 1f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
