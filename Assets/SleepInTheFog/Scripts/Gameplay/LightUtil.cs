using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    public static class LightUtil
    {
        static FieldInfo layersField;

        /// Точечный 2D-свет, который освещает все сортировочные слои.
        public static Light2D AddPointLight(GameObject go, Color color, float radius, float intensity)
        {
            var l = go.GetComponent<Light2D>();
            if (l == null) l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.15f;
            l.falloffIntensity = 0.6f;
            ApplyToAllLayers(l);
            return l;
        }

        public static void ApplyToAllLayers(Light2D l)
        {
            if (layersField == null)
                layersField = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (layersField == null) return;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            layersField.SetValue(l, ids);
        }

        public static readonly Color FireflyColor = Palette.Apricot;
    }
}
