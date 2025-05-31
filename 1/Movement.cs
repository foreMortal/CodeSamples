using UnityEngine;

public class Movement : MonoBehaviour, IPlayerInputInitializeble, IMovementModule
{
    [SerializeField] private float speed = 5f, jumpSpeed, gravity, fadeSpeed;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private ContactFilter2D contactFilter;
    
    private PlayerInput playerInput; 
    private Rigidbody2D rigidBody;
    private SpriteRenderer sprite;
    private Animator animator;
    private Collider2D[] groundCheck = new Collider2D[1];

    private IMovementModule[] modules;
    private MovementState state;
    private Vector3 momentum = Vector3.zero;
    private Vector3 velocity;

    private float groundBuffer;
    public bool grounded = false;
    private int facingRight = 1;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;
    }

    private void Awake()
    {
        modules = GetComponents<IMovementModule>();
        rigidBody = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        contactFilter.SetLayerMask(groundMask);
    }

    public void FixedUpdate()
    {
        bool momentumAllowed = state == MovementState.OnGround ||
                               state == MovementState.InAir;

        if(momentumAllowed)
            Gravitation();

        if (state != MovementState.InDash)
            velocity = new Vector3(playerInput.Move.x * speed, 0f);
        else
            velocity = Vector3.zero;

        if(state == MovementState.OnGround)
        {
            if (velocity != Vector3.zero)
                animator.SetFloat("speed", 1f);
            else
                animator.SetFloat("speed", -1f);
        }

        if (state != MovementState.InDash)
        {
            if (velocity.x < 0f && facingRight > 0)
                Flip();
            else if (velocity.x > 0f && facingRight < 0)
                Flip();
        }

        for(int i = 0; i < modules.Length; i++)
        {
            modules[i].Perform(ref velocity, ref momentum, ref state, ref facingRight);
        }

        if(momentumAllowed || state == MovementState.OnWall)
            CalculateMomentum();

        rigidBody.velocity = velocity;
    }

    public void Perform(ref Vector3 _, ref Vector3 __, ref MovementState state, ref int ___)
    {   
        if (state == MovementState.OnGround)
            Jump();
    }

    private void Jump()
    {
        if (playerInput.Jump)
        {
            momentum.y = jumpSpeed;
            playerInput.Jump = false;
            animator.Play("JumpState", 0);
        }
    }

    private void Gravitation()
    {
        if (Physics2D.OverlapBox(transform.position + new Vector3(0.05f, -1.14f), new Vector2(0.65f, 0.25f), 0f, contactFilter, groundCheck) > 0)
        {
            state = MovementState.OnGround;

            if (groundBuffer < 0.1f)
                groundBuffer = 0.05f;

            if (momentum.x != 0)
                momentum.x = 0;

            if (momentum.y <= 0f)
                momentum.y = -2f;

            if (!grounded)
            {
                grounded = true;
                animator.SetBool("grounded", grounded);
            }
        }
        else
        {
            if(groundBuffer > 0)
                groundBuffer -= Time.fixedDeltaTime;
            else if (state != MovementState.InAir)
                state = MovementState.InAir;

            momentum.y += -gravity * Time.deltaTime;
            if (grounded)
            {
                grounded = false;
                animator.SetBool("grounded", grounded);
            }
        }
    }

    public void Flip()
    {
        sprite.flipX = !sprite.flipX;
        facingRight = -facingRight;
    }

    private void CalculateMomentum()
    {
        if (state == MovementState.InAir)
        {
            if(momentum.x != 0f || momentum.y > 0f)
            {
                if (Physics2D.OverlapBox(transform.position + new Vector3(0.007f, -0.02f), new Vector2(0.886f, 1.688f), 0f, contactFilter, groundCheck) > 0)
                {
                    momentum.x = 0;

                    if (momentum.y > 0)
                        momentum.y = 0;
                }
            }
        }

        if (momentum.x > 0)
        {
            momentum.x -= fadeSpeed * Time.deltaTime;

            if (velocity.x < 0f)
            {
                momentum.x += velocity.x * Time.deltaTime * 4f;

                if (momentum.x <= 0f)
                    momentum.x = 0f;
            }

            if (momentum.x > velocity.x)
                velocity.x = momentum.x;
        }
        if (momentum.x < 0)
        {
            momentum.x += fadeSpeed * Time.deltaTime;

            if (velocity.x > 0f)
            {
                momentum.x += velocity.x * Time.deltaTime * 4f;

                if (momentum.x >= 0f)
                    momentum.x = 0f;
            }

            if (momentum.x < velocity.x)
                velocity.x = momentum.x;
        }

        if(momentum.y != 0)
            velocity.y = momentum.y;
    }
}

public enum MovementState
{
    InAir,
    OnGround,
    InDash,
    InDoubleJump,
    OnWall,
    InClimb,
    None,
}