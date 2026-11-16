using System.Collections;
using UnityEngine;

namespace OnLinces
{
    /// Música de fondo: una sola pista a la vez, sigue sonando al cambiar de escena si es la misma,
    /// y cambia con un fundido corto.
    public static class Music
    {
        static AudioSource source;
        static Coroutine fading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { source = null; fading = null; }

        public static void Play(AudioClip clip, float volume = 0.5f)
        {
            if (clip == null) return;
            if (source == null)
            {
                var go = new GameObject("Music");
                go.transform.SetParent(GameManager.Instance.transform, false);
                source = go.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
            }
            if (source.clip == clip && source.isPlaying) return;
            if (fading != null) GameManager.Instance.StopCoroutine(fading);
            fading = GameManager.Instance.StartCoroutine(Swap(clip, volume));
        }

        static IEnumerator Swap(AudioClip clip, float volume)
        {
            float start = source.volume;
            for (float t = 0; t < 0.4f && source.isPlaying; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / 0.4f);
                yield return null;
            }
            source.clip = clip;
            source.volume = volume;
            source.Play();
            fading = null;
        }
    }
}
