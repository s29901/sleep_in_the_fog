using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SITF
{
    /// Громкость музыки и звуков. Звуки кладутся в Resources/SITF/Audio (например pickup.wav) — если файла нет, просто тишина.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        AudioSource music, sfx;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("sitf_music", 0.7f);
            set { PlayerPrefs.SetFloat("sitf_music", value); if (I) I.music.volume = value; }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("sitf_sfx", 0.8f);
            set { PlayerPrefs.SetFloat("sitf_sfx", value); if (I) I.sfx.volume = value; }
        }

        void Awake()
        {
            I = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = MusicVolume;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.volume = SfxVolume;
            var clip = Resources.Load<AudioClip>("SITF/Audio/music");
            if (clip) { music.clip = clip; music.Play(); }
        }

        public static void Sfx(string name)
        {
            if (!I) return;
            var clip = Resources.Load<AudioClip>("SITF/Audio/" + name);
            if (clip) I.sfx.PlayOneShot(clip);
        }
    }
}
