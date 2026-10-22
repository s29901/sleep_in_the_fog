using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SITF
{
    /// Рой светлячков. Клик с банкой (с крышкой!) — поймать.
    public class FireflySwarm : Interactable
    {
        public int count = 3;
        public FireflyParticles particles;
        int left;

        public override bool CanHover => left > 0;

        void Start()
        {
            left = GameState.I.GetInt(Key, count);
            if (!particles) particles = GetComponentInChildren<FireflyParticles>();
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (particles) particles.SetRate(left > 0 ? 1.5f + left * 1.2f : 0.3f);
        }

        public override void Interact(ItemId held)
        {
            var gs = GameState.I;
            if (held == ItemId.None && gs.data.hasJar) held = ItemId.Jar;   // банка используется сама, если ничего не выбрано
            if (held != ItemId.Jar || !gs.data.hasJar)
            {
                // без банки светлячков не поймать — они разлетаются
                if (particles) particles.Burst(6);
                if (gs.data.hasJar) gs.Refuse(ItemId.Jar);
                return;
            }
            if (!gs.data.lidOnJar)
            {
                // без крышки — улетают обратно
                if (particles) particles.Burst(8);
                gs.Refuse(ItemId.Jar);
                return;
            }
            int caught = gs.AddFireflies(left);
            if (caught <= 0) { gs.Refuse(ItemId.Jar); return; }
            left -= caught;
            gs.SetInt(Key, left);
            AudioManager.Sfx("catch");
            Effects.FlyTo(transform.position, PlayerMover.Current.transform, caught, LightUtil.FireflyColor);
            UpdateVisual();
        }
    }
}
