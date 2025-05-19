using UnityEngine;

public class Character : MonoBehaviour
{
    public float Speed = 1f;
    
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        this.transform.position += new Vector3(this.Speed * horizontal * Time.deltaTime, 0f, 0f);
    }
}
