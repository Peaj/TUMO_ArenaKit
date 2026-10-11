using UnityEngine;

namespace ArenaKit
{
    /// <summary>Plays a sound, e.g. when a coin is collected or a door opens.</summary>
    public class PlaySound : MonoBehaviour
    {
        public AudioClip Sound;
        [Range(0f, 1f)] public float Volume = 0.7f;

        public void Play()
        {
            GameManager.PlaySound(Sound, Volume);
        }
    }
}
