using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Дом в конце пути: когда собраны все осколки — открывается пазл.
    public class FinalPoint : Interactable
    {
        public SpriteRenderer windowOn;   // тёплое окно, загорается когда все осколки собраны
        Light2D light2D;

        public override bool CanHover => GameState.I.data.shards >= GameState.TotalShards && !PuzzleController.IsOpen;

        void Start()
        {
            var lgo = new GameObject("WindowLight");
            lgo.transform.SetParent(transform, false);
            light2D = LightUtil.AddPointLight(lgo, Palette.Apricot, 4f, 0f);
        }

        void Update()
        {
            bool ready = GameState.I.data.shards >= GameState.TotalShards;
            float target = ready ? 1f : 0f;
            light2D.intensity = Mathf.MoveTowards(light2D.intensity, target, Time.deltaTime);
            if (windowOn) { var c = windowOn.color; c.a = Mathf.MoveTowards(c.a, target, Time.deltaTime); windowOn.color = c; }
        }

        public override void Interact(ItemId held)
        {
            if (GameState.I.data.shards < GameState.TotalShards) return;
            PuzzleController.Open();
        }
    }
}
