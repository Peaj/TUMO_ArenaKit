using UnityEngine;

public class Character : MonoBehaviour
{
    public float Speed = 1f;
    public float JumpForce = 10f;
    
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
        
        Animator animator = GetComponent<Animator>();
        animator.SetFloat("Speed", Mathf.Abs(horizontal));
        
        // 1. Get Input GetButtonDown("Jump")
        bool jump = Input.GetButtonDown("Jump");
        if (jump)
        {
            // 2. Add force
            rigidbody2D.AddForce(new Vector2(0f, this.JumpForce));
            // 3. Play jump animation
            animator.SetTrigger("Jump");
        }
        // (4). Check ground
    }
}
