using UnityEngine;

namespace ArenaKit
{
    /// <summary>Destroys its object. Called by an animation event at the end of the explosion animation.</summary>
    public class DestroyHelper : MonoBehaviour
    {
        public void DestroyOnAnimationEvent()
        {
            Destroy(this.gameObject);
        }
    }
}
