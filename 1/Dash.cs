using UnityEngine;

public class Dash : MonoBehaviour, IPlayerInputInitializeble, IMovementModule
{
    [SerializeField]private float time = 0.15f, speed, couldown;

    private PlayerInput playerInput;
    private Animator animator;
    private new CapsuleCollider2D collider;

    private Vector3 changedcollider = new Vector3(-0.05f, 0.23f), defaultCollider;
    private float timer;
    private bool onCD;

    public void Initialize(PlayerInput playerInput)
    {
        this.playerInput = playerInput;

        animator = GetComponent<Animator>();

        collider = GetComponent<CapsuleCollider2D>();

        defaultCollider.x = collider.offset.y;
        defaultCollider.y = collider.size.y;
    }

    public void Perform(ref Vector3 velocity, ref Vector3 momentum, ref MovementState state, ref int facingRight)
    {
        if (!onCD)
        {
            if (state != MovementState.InDash && state != MovementState.OnWall)
            {
                if (playerInput.Dash)
                {
                    state = MovementState.InDash;
                    timer = time;
                    playerInput.Dash = false;

                    collider.offset = new Vector2(collider.offset.x, changedcollider.x);
                    collider.size = new Vector2(collider.size.x, changedcollider.y);

                    animator.SetBool("dash", true);
                }
            }

            if (state == MovementState.InDash)
            {
                if (timer > 0f)
                {
                    momentum = Vector3.zero;
                    velocity.x = speed * facingRight;
                    timer -= Time.deltaTime;
                }
                else
                {
                    onCD = true;
                    timer = couldown;
                    momentum = new Vector3(6.5f * facingRight, 0f);
                    state = MovementState.InAir;

                    collider.offset = new Vector2(collider.offset.x, defaultCollider.x);
                    collider.size = new Vector2(collider.size.x, defaultCollider.y);

                    animator.SetBool("dash", false);
                }
            }
            else if(timer > 0f)
            {
                onCD = true;
                timer = couldown;

                collider.offset = new Vector2(collider.offset.x, defaultCollider.x);
                collider.size = new Vector2(collider.size.x, defaultCollider.y);

                animator.SetBool("dash", false);
            }
        }
    }

    private void Update()
    {
        if (onCD)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                onCD = false;
        }
    }
}