using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SITF.EditorTools
{
    /// Tools → Sleep in the Fog → Rebuild heroine walk animation.
    /// Собирает клипы ходьбы и стояния из кадров в Assets/animations/hero_2 (walk_1…walk_N)
    /// и заново настраивает контроллер Player: стоит ⇄ идёт по параметру IsRun.
    public static class SITF_HeroAnimTool
    {
        const string FramesFolder = "Assets/animations/hero_2";
        const string ControllerPath = "Assets/animations/postac/Player.controller";
        const string WalkClipPath = "Assets/animations/walk_hero_2.anim";
        const string IdleClipPath = "Assets/animations/hero_2_idle.anim";
        const float Fps = 10f;
        const int IdleFrame = 3;   // какой кадр использовать, когда героиня стоит

        [MenuItem("Tools/Sleep in the Fog/Rebuild heroine walk animation")]
        static void Rebuild()
        {
            var frames = AssetDatabase.FindAssets("t:Sprite walk_", new[] { FramesFolder })
                .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => FrameNumber(s.name))
                .ToArray();
            if (frames.Length == 0)
            {
                EditorUtility.DisplayDialog("Sleep in the Fog", "Не нашла кадры walk_1…walk_N в " + FramesFolder, "OK");
                return;
            }

            var walk = MakeClip(WalkClipPath, "walk_hero_2", frames);
            var idle = MakeClip(IdleClipPath, "hero_2_idle", new[] { frames[Mathf.Clamp(IdleFrame - 1, 0, frames.Length - 1)] });
            SetupController(idle, walk);

            // героиня в сцене сразу показывает кадр стояния
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                var sr = player.GetComponentInChildren<SpriteRenderer>();
                var anim = player.GetComponentInChildren<Animator>();
                if (sr) { Undo.RecordObject(sr, "SITF"); sr.sprite = frames[Mathf.Clamp(IdleFrame - 1, 0, frames.Length - 1)]; }
                if (anim && sr && anim.gameObject != sr.gameObject)
                    Debug.LogWarning("[SITF] Animator и Sprite Renderer героини на разных объектах — анимация не будет менять кадры. Они должны быть на одном объекте.");
                if (anim && anim.runtimeAnimatorController == null)
                {
                    Undo.RecordObject(anim, "SITF");
                    anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Sleep in the Fog",
                $"Готово: ходьба из {frames.Length} кадров ({Fps} кадров/с), стояние — кадр {IdleFrame}.\n" +
                "Контроллер Player: IsRun = true → ходьба, false → стоит.", "OK");
        }

        static int FrameNumber(string name)
        {
            var digits = new string(name.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int n) ? n : 0;
        }

        static AnimationClip MakeClip(string path, string name, Sprite[] frames)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();
            clip.name = name;
            clip.frameRate = Fps;

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / Fps, value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.stopTime = frames.Length / Fps;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void SetupController(AnimationClip idle, AnimationClip walk)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            if (!ctrl.parameters.Any(p => p.name == "IsRun"))
                ctrl.AddParameter("IsRun", AnimatorControllerParameterType.Bool);

            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);

            var idleState = sm.AddState("Idle", new Vector3(300, 100, 0));
            idleState.motion = idle;
            var walkState = sm.AddState("Walk", new Vector3(300, 0, 0));
            walkState.motion = walk;
            sm.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsRun");
            toWalk.hasExitTime = false;
            toWalk.duration = 0;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRun");
            toIdle.hasExitTime = false;
            toIdle.duration = 0;

            EditorUtility.SetDirty(ctrl);
        }
    }
}
