using UnityEngine;

public class DoubleJump : MonoBehaviour, IPlayerInputInitializeble, IMovementModule
{
    [SerializeField] private ContactFilter2D doubleJumpFilter;
    [SerializeField] private float doubleJumpCD, jumpLength, jumpHeight, speed;

    private PlayerInput playerInput;
    private Animator animator;
    private Collider2D[] doubleCheck = new Collider2D[1];

    private LayerMask mask = 1 << 3;
    private Vector3 momentum, wallJumpPosition, playerPos;
    private float doubleTimer = -1f, t;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;

        animator = GetComponent<Animator>();
        doubleJumpFilter.SetLayerMask(mask);
    }

    private void Update()
    {
        if(doubleTimer > 0)
            doubleTimer -= Time.deltaTime;
    }

    public void Perform(ref Vector3 velocity, ref Vector3 momentum, ref MovementState state, ref int __)
    {
        if(state == MovementState.InDoubleJump)
        {
            if(t < 1f)
            {
                velocity = Vector3.zero;
                t += Time.fixedDeltaTime * speed;
                transform.position = Vector3.Lerp(playerPos, wallJumpPosition, t);
            }
            else
            {
                Vector3 move = playerInput.Move;
                momentum.y = 0f;
                this.momentum = Vector3.zero;

                if (move != Vector3.zero)
                {
                    this.momentum = momentum;
                    momentum = CalculateMomentum(move, velocity.x) + this.momentum;
                    state = MovementState.InAir;
                }
                else if(momentum != Vector3.zero)
                    momentum = Vector3.zero;
            }
        }
        else if (doubleTimer <= 0f && state == MovementState.InAir)
        {
            if (playerInput.Jump)
            {
                if (Physics2D.OverlapBox(transform.position, new Vector2(1.15f, 2f), 0f, doubleJumpFilter, doubleCheck) > 0)
                {
                    t = 0f;
                    playerInput.Jump = false;
                    doubleTimer = doubleJumpCD;
                    momentum.y = 0f;

                    state = MovementState.InDoubleJump;

                    playerPos = transform.position;
                    wallJumpPosition = doubleCheck[0].transform.position;
                }
            }
        }
    }

    private Vector3 CalculateMomentum(Vector3 move, float velocity)
    {
        if (move.y >= 1)
            return new Vector3(velocity, jumpHeight);

        else if (move.y <= -1)
            return Vector3.zero;

        else if (move.x >= 1)
            return new Vector3(jumpLength, jumpHeight * 0.75f);

        else if (move.x <= -1)
            return new Vector3(-jumpLength, jumpHeight * 0.75f);

        return Vector3.zero;
    }
}