using UnityEngine;

public class HorizontalClimb : MonoBehaviour, IPlayerInputInitializeble, IMovementModule
{
    [SerializeField] private float climbingSpeed;
    private PlayerInput playerInput;
    private new CapsuleCollider2D collider;
    private Animator animator;
    private Movement movement;

    private Bounds climbObjectInfo;
    private Vector3[] offset = new Vector3[4] { Vector3.zero, new Vector3(0.19f, 0f), Vector3.zero, new Vector3(0.6f, 0f) }, positions = new Vector3[2];
    private ContactFilter2D contactFilter = new();
    private RaycastHit2D[] raycastHit = new RaycastHit2D[1];
    private Vector3 pos;

    private bool[] climb = new bool[2];
    private bool climbingAnim, aboveClimb;
    private float couldown, t = 11, climbY;
    private int i;

    private float playerWidth, playerHeight;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;

        movement =GetComponent<Movement>();
        animator = GetComponent<Animator>();
        collider = GetComponent<CapsuleCollider2D>();
        playerWidth = collider.size.x * transform.localScale.x;
        playerHeight = collider.size.y * transform.localScale.y;
        contactFilter.SetLayerMask(1 << 9);
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < 2; i++)
        {
            Gizmos.DrawRay(transform.position + offset[i + 2], transform.up * -1 * 1.2f);
        }
    }

    public void Update()
    {
        if (couldown > 0)
            couldown -= Time.deltaTime;
    }

    public void Perform(ref Vector3 velocity, ref Vector3 momentum, ref MovementState state, ref int facingRight)
    {
        if (!climbingAnim)
        {
            // if player on the platform and can climb down
            if (state == MovementState.OnGround)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (Physics2D.Raycast(transform.position + offset[i + 2] * facingRight, transform.up * -1, contactFilter, raycastHit, 1.2f) > 0)
                        climb[i] = true;                    
                    else
                        climb[i] = false;
                }

                if (climb[0] && !climb[1])
                {
                    if (playerInput.Move.y < 0)
                    {
                        velocity = Vector3.zero;
                        state = MovementState.None;

                        GaverClimbInfo(-1);

                        StartClimbing(facingRight, -1);

                        movement.Flip();

                        animator.SetBool("grounded", false);
                        animator.Play("ClimbingDown");
                    }
                }
            }
            else// if player can climb under the platform
            {
                if (couldown <= 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (Physics2D.Raycast(transform.position + offset[i] * facingRight, transform.up, contactFilter, raycastHit, 1f) > 0)
                        {
                            if (i == 0 && state != MovementState.InClimb)
                            {
                                t = 0f;
                                climbY = raycastHit[0].point.y - 0.8125f;

                                animator.SetBool("grounded", false);
                                animator.SetBool("climbing", true);

                                state = MovementState.InClimb;
                                momentum = Vector3.zero;
                            }
                            climb[i] = true;
                        }
                        else
                            climb[i] = false;
                    }
                }

                if (t < 1)
                {
                    t += Time.fixedDeltaTime * 15f;
                    transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, climbY), t);
                }

                if (climb[0])
                {
                    if (climb[1])
                    {
                        if (playerInput.Move.x != 0)
                        {
                            animator.SetFloat("speed", 1f);
                            velocity.x = climbingSpeed * playerInput.Move.x;
                        }
                        else
                            animator.SetFloat("speed", -1f);
                    }
                    else
                    {
                        animator.SetFloat("speed", -1f);
                        velocity = Vector3.zero;

                        if (playerInput.Move.y > 0)
                        {
                            state = MovementState.InAir;

                            GaverClimbInfo(1);

                            animator.Play("ClimbingUp");
                            StartClimbing(facingRight, 1);

                            movement.Flip();
                        }
                    }

                    if (playerInput.Move.y < 0)
                    {
                        climb[0] = climb[1] = false;

                        couldown = 0.2f;
                        state = MovementState.InAir;
                        animator.SetBool("climbing", false);

                    }
                }
                else if (state == MovementState.InClimb)
                {
                    animator.SetBool("climbing", false);
                    state = MovementState.InAir;
                }
            }
        }
        else//Climb up/down Animation
        {
            velocity = Vector3.zero;
            if (i < 2)
            {
                t += Time.fixedDeltaTime * 7f;

                transform.position = Vector3.Lerp(pos, positions[i], t);
                if (t >= 1)
                {
                    pos = transform.position;
                    t = 0f;
                    i++;
                }
            }
            else
            {
                i = 0;
                t = 11;
                transform.position = positions[1];
                collider.excludeLayers = 0;
                climbingAnim = false;
            }
        }
    }

    private void GaverClimbInfo(int dir)
    {
        if (Physics2D.Raycast(transform.position, transform.up * dir, contactFilter, raycastHit, 1.2f) > 0)
            climbObjectInfo = raycastHit[0].collider.bounds;
    }

    private void StartClimbing(int facingRight, int dir)
    {
        t = 0f;
        collider.excludeLayers = ~0;

        positions[0] = climbObjectInfo.center + climbObjectInfo.size / 2 * facingRight;
        positions[0].x += playerWidth * facingRight;

        float x = (climbObjectInfo.center.x + (climbObjectInfo.size.x / 2) * facingRight) + playerWidth / 2 * -facingRight;
        float y = climbObjectInfo.center.y + (climbObjectInfo.size.y / 2 + playerHeight / 2f - 0.02f) * dir;

        positions[1] = new Vector3(x, y);
        pos = transform.position;
        climbingAnim = true;
        animator.SetBool("climbing", false);
    }
}