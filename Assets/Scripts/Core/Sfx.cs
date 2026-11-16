using UnityEngine;

namespace OnLinces
{
    /// Efectos de sonido 2D (sin distancia). Usa una fuente de audio en el GameManager.
    public static class Sfx
    {
        static AudioSource source;

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null || GameManager.Instance == null) return;
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
