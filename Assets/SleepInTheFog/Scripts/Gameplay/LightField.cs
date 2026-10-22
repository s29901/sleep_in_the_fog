using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    public static class LightField
    {
        /// 0 = темно и туман, 1 = полностью светло.
        public static float Clearance(Vector2 p)
        {
            float best = 0f;
            for (int i = 0; i < LightSource.All.Count; i++)
            {
                var l = LightSource.All[i];
                if (l.strength <= 0f || l.radius <= 0f) continue;
                float d = Vector2.Distance(p, l.transform.position) / l.radius;
                float v = (1f - Mathf.SmoothStep(0.55f, 1.15f, d)) * l.strength;
                if (v > best) best = v;
            }
            return best;
        }
    }
}
