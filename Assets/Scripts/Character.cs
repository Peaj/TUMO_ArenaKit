using UnityEngine;

public class Character : MonoBehaviour
{
    public float Speed = 1f;
    
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
        rigidbody2D.AddForce(new Vector2(this.Speed * horizontal * Time.deltaTime, 0f));

        if (horizontal < 0f)
        {
            this.transform.localScale = new Vector3(-1.2f, 1.2f, 1.2f);
        }
        else if (horizontal > 0f)
        {
            this.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }
}
