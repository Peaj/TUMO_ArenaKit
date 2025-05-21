using UnityEngine;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public float Speed = 1f;
    public float JumpForce = 10f;
    public LayerMask CharacterLayer;

    private bool grounded = false;
    
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
        
        // (4). Check ground
        this.grounded = Physics2D.Raycast(this.transform.position, Vector2.down, 1f, ~this.CharacterLayer);
        Debug.DrawRay(this.transform.position, Vector2.down, Color.red);
        
        // 1. Get Input GetButtonDown("Jump")
        bool jump = Input.GetButtonDown("Jump");
        if (jump && this.grounded)
        {
            // 2. Add force
            rigidbody2D.AddForce(new Vector2(0f, this.JumpForce));
            // 3. Play jump animation
            animator.SetTrigger("Jump");
        }
    }
}
