using UnityEngine;

namespace SITF
{
    /// Выход в другую сцену. Если needsLight — пока нет света, туман не пускает (невидимая стена).
    /// Героиня, пришедшая из targetScene, появляется рядом с этим выходом.
    public class SceneExit : Interactable
    {
        public string targetScene = Scenes.Path;
        [Tooltip("Выход у левого края сцены (иначе — у правого)")]
        public bool leftSide;
        public bool needsLight = true;
        public float wallOffset = 1.2f;
        [Tooltip("На каком расстоянии от выхода появляется героиня, пришедшая из targetScene")]
        public float spawnInset = 2f;
        bool loading;
        bool armed;   // не срабатывает, пока героиня не отошла от выхода (после появления рядом)

        bool Open => !needsLight || GameState.I.JarHasLight;
        float Dir => leftSide ? -1f : 1f;

        public override bool CanHover => true;

        void Start()
        {
            var p = PlayerMover.Current;
            if (p && GameFlow.CameFrom == targetScene)
                p.Teleport(new Vector2(transform.position.x - Dir * spawnInset, p.transform.position.y), -Dir);
        }

        bool Inside(PlayerMover p) =>
            leftSide ? p.transform.position.x <= transform.position.x + 0.3f
                     : p.transform.position.x >= transform.position.x - 0.3f;

        void Update()
        {
            var p = PlayerMover.Current;
            if (!p) return;
            float wall = Open ? (leftSide ? float.NegativeInfinity : float.PositiveInfinity)
                              : transform.position.x - Dir * wallOffset;
            if (leftSide) p.blockMinX = wall; else p.blockMaxX = wall;

            bool inside = Inside(p);
            if (!inside) armed = true;
            if (Open && armed && inside && !loading) Go();
        }

        public override void Interact(ItemId held)
        {
            if (!Open) { GameState.I.Refuse(ItemId.Jar); return; }
            armed = true;
            PlayerMover.Current.WalkTo(transform.position + Vector3.right * Dir);
        }

        void Go()
        {
            loading = true;
            GameFlow.Load(targetScene);
        }
    }
}
