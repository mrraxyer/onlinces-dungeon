using UnityEngine;

namespace OnLinces
{
    /// Música de la escena.
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] AudioClip clip;
        [Tooltip("Solo End: si perdiste")]
        [SerializeField] AudioClip loseClip;
        [Range(0, 1)] [SerializeField] float volume = 0.5f;

        void Start()
        {
            var c = loseClip != null && GameManager.Instance.Run.Won == false ? loseClip : clip;
            Music.Play(c, volume);
        }
    }
}
