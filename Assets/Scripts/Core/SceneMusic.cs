using UnityEngine;

namespace OnLinces
{
    /// Pon esto en cada escena: al empezar suena su música.
    /// En End elige victoria o derrota según cómo terminó la partida.
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Tooltip("Solo End: música si perdiste (si está vacío usa 'clip')")]
        [SerializeField] AudioClip loseClip;
        [Range(0, 1)] [SerializeField] float volume = 0.5f;

        void Start()
        {
            var c = loseClip != null && GameManager.Instance.Run.Won == false ? loseClip : clip;
            Music.Play(c, volume);
        }
    }
}
