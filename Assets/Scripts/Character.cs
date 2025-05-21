using UnityEngine;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public float Speed = 1f;
    public float JumpForce = 10f;
    public LayerMask CharacterLayer;

    private bool grounded = false;
    private float horizontal;
    
    private new Rigidbody2D rigidbody2D; //Override existing obsolete rigidbody2D
    private Animator animator;

    void Awake()
    {
        this.rigidbody2D = GetComponent<Rigidbody2D>();
        this.animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        this.horizontal = Input.GetAxis("Horizontal");

        Run();
        FlipCharacterInMoveDirection();
        Jump();
    }

    private void FlipCharacterInMoveDirection()
    {
        if (this.horizontal < 0f)
        {
            this.transform.localScale = new Vector3(-1.2f, 1.2f, 1.2f);
        }
        else if (this.horizontal > 0f)
        {
            this.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
    }

    void Run()
    {
        this.rigidbody2D.AddForce(new Vector2(this.Speed * this.horizontal * Time.deltaTime, 0f));
        this.animator.SetFloat("Speed", Mathf.Abs(this.horizontal));
    }

    void Jump()
    {
        // Ground check
        this.grounded = Physics2D.Raycast(this.transform.position, Vector2.down, 1f, ~this.CharacterLayer);
        Debug.DrawRay(this.transform.position, Vector2.down, Color.red);
        
        bool jump = Input.GetButtonDown("Jump");
        if (jump && this.grounded)
        {
            this.rigidbody2D.AddForce(new Vector2(0f, this.JumpForce));
            this.animator.SetTrigger("Jump");
        }
    }
}
