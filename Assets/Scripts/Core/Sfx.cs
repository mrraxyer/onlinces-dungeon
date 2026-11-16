using UnityEngine;

namespace OnLinces
{
    /// Efectos de sonido 2D.
    public static class Sfx
    {
        static AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => source = null;

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            if (source == null)
            {
                var go = GameManager.Instance.gameObject;
                source = go.GetComponent<AudioSource>();
                if (source == null) source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
            source.PlayOneShot(clip, volume);
        }
    }
}
