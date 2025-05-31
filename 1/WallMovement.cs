using UnityEngine;

public class WallMovement : MonoBehaviour, IPlayerInputInitializeble, IMovementModule
{
    [SerializeField] private float climbingSpeed, jumpLength, jumpHeight;

    private PlayerInput playerInput;
    private Movement movement;
    private Animator animator;
    private new Collider2D collider;

    private Vector3 climbPos;
    private Vector3[] offset = new Vector3[2] { new Vector3(0f, 1f), new Vector3(0f, -0.745f) };
    private ContactFilter2D contactFilter;
    private RaycastHit2D[] raycastHit = new RaycastHit2D[1];

    private bool[] checks = new bool[2];
    private bool climb;
    private float t;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;

        collider = GetComponent<Collider2D>();
        movement = GetComponent<Movement>();
        animator = GetComponent<Animator>();

        contactFilter.SetLayerMask(1 << 7);
    }

    public void Perform(ref Vector3 velocity, ref Vector3 momentum, ref MovementState state, ref int facingRight)
    {
        if (!climb)
        {
            for (int i = 0; i < 2; i++)
            {
                if (Physics2D.Raycast(transform.position + offset[i], transform.right * facingRight, contactFilter, raycastHit, 0.51f) > 0)
                    checks[i] = true;
                else
                    checks[i] = false;
            }

            if (checks[0] && checks[1] && state != MovementState.OnWall)
            {
                state = MovementState.OnWall;

                animator.SetBool("onWall", true);
                momentum = Vector3.zero;
            }
            else if (!checks[0] && state == MovementState.OnWall ||
                     !checks[1] && state == MovementState.OnWall)
            {
                animator.SetBool("onWall", false);
                state = MovementState.InAir;
            }

            //wall climbing and jump
            if (checks[0] && checks[1])
            {
                if (playerInput.Jump)
                {
                    movement.Flip();
                    momentum = new Vector3(jumpLength * facingRight, jumpHeight);

                    animator.SetBool("onWall", false);
                    animator.Play("JumpState");

                    playerInput.Jump = false;
                }
                else if (playerInput.Move.y != 0)
                {
                    //just climb up
                    velocity += new Vector3(0f, playerInput.Move.y * climbingSpeed);

                    // if player can climb on top of the wall
                    for (int i = 0; i < 2; i++)
                    {
                        if (Physics2D.Raycast(transform.position + offset[i] + new Vector3(0f, 0.1f), transform.right * facingRight, contactFilter, raycastHit, 0.51f) > 0)
                            checks[i] = true;
                        else
                            checks[i] = false;
                    }

                    //climb on top of the wall
                    if (!checks[0] && checks[1] && playerInput.Move.y > 0)
                    {
                        t = 0f;
                        climb = true;
                        collider.excludeLayers = ~0;
                        climbPos = transform.position + new Vector3(0.77f * facingRight, 2.19f);
                    }//y=2.19; x=0.77;
                }
            }
        }
        else
        {
            velocity = Vector3.zero;
            t += Time.deltaTime * 4f;
            transform.position = Vector3.Lerp(transform.position, climbPos, t);
            if (t >= 1f)
            {
                collider.excludeLayers = 0;
                state = MovementState.OnGround;
                animator.SetBool("onWall", false);
                climb = false;
            }
        }
    }
}