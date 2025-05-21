using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class Character : MonoBehaviour
{
    public float MoveForce = 1f;
    public float MaxSpeed = 10f;
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
        // Run();
        RunAdvanced();
        FlipCharacterInMoveDirection();
    }

    void OnMove(InputValue value)
    {
        this.horizontal = value.Get<Vector2>().x;
    }

    void OnJump()
    {
        // Ground check
        this.grounded = Physics2D.Raycast(this.transform.position, Vector2.down, 1f, ~this.CharacterLayer);
        Debug.DrawRay(this.transform.position, Vector2.down, Color.red);
        
        if (this.grounded)
        {
            this.rigidbody2D.AddForce(new Vector2(0f, this.JumpForce));
            this.animator.SetTrigger("Jump");
        }
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
        this.rigidbody2D.AddForce(new Vector2(this.MoveForce * this.horizontal * Time.deltaTime, 0f));
        this.animator.SetFloat("Speed", Mathf.Abs(this.horizontal));
    }

    void RunAdvanced()
    {
        float velocity = Mathf.Abs(this.rigidbody2D.linearVelocityX);
        float ratio = 1f - Mathf.Clamp01(velocity / this.MaxSpeed);

        if (this.horizontal * this.rigidbody2D.linearVelocityX < 0f) ratio = 1f;
        
        this.rigidbody2D.AddForce(new Vector2(this.MoveForce * this.horizontal * Time.deltaTime * ratio, 0f));
        this.animator.SetFloat("Speed", Mathf.Abs(this.horizontal));
    }
}
