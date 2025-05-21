using UnityEngine;

public class DestroyHelper : MonoBehaviour
{
    public void DestroyOnAnimationEvent()
    {
        Destroy(this.gameObject);
    }
}