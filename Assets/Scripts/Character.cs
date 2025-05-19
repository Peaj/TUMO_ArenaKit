using UnityEngine;

public class Character : MonoBehaviour
{
    void Update()
    {
        this.transform.position = this.transform.position + new Vector3(0.1f, 0f, 0f);
    }
}
