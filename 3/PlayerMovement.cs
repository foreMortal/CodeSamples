using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform groundCheck;
    [SerializeField] private SettingsScriptableObject settings;
    [SerializeField] private LayerMask layerMask = 1 << 6;

    private Transform cam;
    private Controlls controlls;
    private CharacterController controller;
    private Animator animator;

    public event Action startClimbing;
    public event Action stopClimbing;

    private bool onWall, decrouching, lockedMovement, climb;
    private bool isGrounded, crouch, sliding, ads, jumpAllowed;
    
    private Vector3 velocity, climbVelocity, move, miniMove, wallMove, lockedMove;
    private LayerMask whatIsWall = 1 << 7;
    private RebindType rebindType;
    private InputAction crouchAct, fireAct, moveHorAct, moveVerAct;

    public float slideCdTimer;
    
    private float currSpeed = 8.5f, gravity = -40f, lockedSpeed, deltaSpeed = 4.8f;
    private float groundDistance = 0.4f, lockedSpeedReset, dopSpeed, jumpFrames;
    private float wallCheckDistance = 0.85f, jumpHeight = 3f, doubleJumpTimer, cantClimb, climbTime;
    private float ver, hor, absHor, absVer;

    public delegate void VoidDelegate();
    public event VoidDelegate WallJump;

    public Animator Animator { get { return animator; } set { animator = value; } }

    private void Awake()
    {
        controlls = settings.GetControlls();

        cam = GetComponentInChildren<Camera>().transform;
        controller = GetComponent<CharacterController>();

        if (isActiveAndEnabled)
        {
            jumpAllowed = true;
        }
    }

    private void Start()
    {
        rebindType = settings.rebinds.Type;
        switch (rebindType)
        {
            case RebindType.Apex:
                controlls.GamepadControl.Jump.performed += ctx => Jump();

                crouchAct = controlls.GamepadControl.CrouchOnPressed;
                fireAct = controlls.GamepadControl.Fire;
                moveHorAct = controlls.GamepadControl.MoveHor;
                moveVerAct = controlls.GamepadControl.MoveVer;
                break;
            case RebindType.Universal:
                controlls.Universal.GpdJump.performed += ctx => Jump();

                crouchAct = controlls.Universal.GpdCrouch;
                fireAct = controlls.Universal.GpdFire;
                moveHorAct = controlls.Universal.GpdMoveHor;
                moveVerAct = controlls.Universal.GpdMoveVer;
                break;
            case RebindType.Valorant:
                controlls.ValorantControl.MnKJump.performed += ctx => Jump();

                crouchAct = controlls.ValorantControl.MnKCrouch;
                fireAct = controlls.ValorantControl.MnKFire;
                moveHorAct = controlls.ValorantControl.MnKMoveHor;
                moveVerAct = controlls.ValorantControl.MnKMoveVer;
                break;
            case RebindType.CSGO:
                controlls.CSGOControl.MnKJump.performed += ctx => Jump();

                crouchAct = controlls.CSGOControl.MnKCrouch;
                fireAct = controlls.CSGOControl.MnKFire;
                moveHorAct = controlls.CSGOControl.MnKMoveHor;
                moveVerAct = controlls.CSGOControl.MnKMoveVer;
                break;
        }
    }

    private void OnEnable()
    {
        controlls.Enable();
    }

    private void OnDisable()
    {
        controlls.Disable();
    }

    private void Jump()
    {
        if (isGrounded && jumpAllowed)
        {
            animator.SetBool("Jump", true);
            jumpFrames = 0f;


            if (doubleJumpTimer <= 0f)
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            else
                velocity.y = Mathf.Sqrt(0.3f * jumpHeight * -2 * gravity);

            if (crouch)
                lockedSpeed = currSpeed * 0.45f;
            else if (lockedSpeed <= 0f)
                lockedSpeed = currSpeed;

            doubleJumpTimer = 0.3f;
            lockedSpeedReset = 0f;

            if(lockedMove == Vector3.zero)
                lockedMove = Vector3.Normalize(transform.right * hor + transform.forward * ver);
        }
        else if (climb && jumpAllowed)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.9f, whatIsWall))
            {
                WallJump?.Invoke();
                float dotWallHit = Vector3.Dot(hit.transform.forward, transform.forward);

                if (dotWallHit < -0.3f)
                {
                    ClimbThings(true);

                    if (move != Vector3.zero)
                        lockedMove = Vector3.Normalize(move + hit.transform.forward * 2f);
                    else
                        lockedMove = hit.transform.forward;

                    lockedSpeed = 8.5f;

                    velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                }

                else if (dotWallHit > 0.3f)
                {
                    ClimbThings(true);

                    if (move != Vector3.zero)
                        lockedMove = Vector3.Normalize(move + -hit.transform.forward * 2f);
                    else
                        lockedMove = -hit.transform.forward;

                    lockedSpeed = 8.5f;

                    velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                }

                lockedSpeedReset = 0f;
            }
        }
    }

    void Update()
    {
        GetHorVerValues();

        AutoRun();

        Gravitation();

        WallDetection();
        Climb();

        Crouch();

        Movement();

        DecrouchNew();

        if (jumpFrames < 2f)
        {
            jumpFrames++;
            if (jumpFrames > 1f)
                animator.SetBool("Jump", false);
        }
    }

    private void GetHorVerValues()
    {
        ver = moveVerAct.ReadValue<float>();
        hor = moveHorAct.ReadValue<float>();

        absVer = Mathf.Abs(ver);
        absHor = Mathf.Abs(hor);
    }

    private void Movement()
    {
        if (!lockedMovement && isGrounded && !crouch)
        {
            move = transform.right * hor + transform.forward * ver;

            move.Normalize();

            controller.Move(currSpeed * Time.deltaTime * move);
        }
        else
        {
            LockedSpeedDecreasing();
            if (!climb)
            {
                Vector3 innerMiniMove = transform.right * hor;

                miniMove = innerMiniMove + transform.forward * ver;
                miniMove.Normalize();

                if (lockedMove != Vector3.zero)
                {
                    if (Vector3.Dot(lockedMove, miniMove) < -0.3f)
                    {
                        lockedSpeed -= 2.5f * deltaSpeed * absVer * Time.deltaTime;

                        if(lockedSpeed <= 0f)
                        {
                            lockedSpeed = 0f;
                            lockedMove = Vector3.zero;
                        }

                        controller.Move(currSpeed * 0.4f * Time.deltaTime * miniMove);
                    }
                    else
                    {
                        controller.Move(currSpeed * 0.4f * Time.deltaTime * innerMiniMove);
                    }
                }
                else
                {
                    controller.Move(currSpeed * 0.4f * Time.deltaTime * miniMove);
                }
                controller.Move(lockedSpeed * Time.deltaTime * lockedMove);
            }
            else
            {
                if (ver >= 0f)
                {
                    wallMove = transform.right * hor + transform.up * ver + transform.forward * ver;

                    wallMove.Normalize();

                    controller.Move(0.45f * dopSpeed * Time.deltaTime * wallMove);
                }
                else if (ver < 0f)
                {
                    ClimbThings(false);

                    wallMove = transform.right * hor + transform.forward * ver;

                    wallMove.Normalize();

                    controller.Move(0.45f * dopSpeed * Time.deltaTime * wallMove);
                }
            }
        }
    }

    private void LockedSpeedDecreasing()
    {
        if (lockedSpeed > 0f && isGrounded && lockedMovement)
        {
            lockedSpeed -= deltaSpeed * Time.deltaTime;
            if (lockedSpeed < 0f)
            {
                lockedSpeed = 0f;
                lockedMove = Vector3.zero;
            }
        }
    }

    private void Gravitation()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, layerMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (isGrounded && doubleJumpTimer >= 0f)
            doubleJumpTimer -= Time.deltaTime;

        if (!climb)
        {
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

        }
        else
        {
            climbTime += Time.deltaTime;
            if (climbTime >= 2f)
            {
                ClimbThings(false);
            }
            if (wallMove == Vector3.zero)
            {
                climbVelocity.y += gravity * Time.deltaTime;
                controller.Move(climbVelocity * Time.deltaTime);
            }
            else
            {
                if (velocity.y != 0f)
                    velocity.y = 0f;
                if (climbVelocity.y != 0f)
                    climbVelocity.y = 0f;
            }
        }

        if (climb && isGrounded)
            ClimbThings(false);
    }

    private void ClimbThings(bool jump)
    {
        climb = false;
        cantClimb = 1f;
        climbTime = 0f;
        climbVelocity.y = 0f;
        if (!jump)
        {
            lockedMove = Vector3.zero;
            lockedSpeed = 0f;
        }
        stopClimbing?.Invoke();
    }

    private void DecrouchNew()
    {
        if (crouchAct.ReadValue<float>() <= 0f)
        {
            if (sliding)
            {
                sliding = false;
                decrouching = true;
                lockedMovement = false;
                controller.height = 3.37f;
                controller.center = new Vector3(0f, -0.92f, 0f);
                AnimCrouch(sliding);
            }
            if (crouch)
            {
                crouch = false;
                decrouching = true;
                controller.height = 3.37f;
                controller.center = new Vector3(0f, -0.92f, 0f);
                AnimCrouch(crouch);
            }
        }
        if (decrouching)
        {
            if (cam.localPosition.y < 0f)
            {
                cam.localPosition = new Vector3(0f, Mathf.MoveTowards(cam.localPosition.y, 0f, 6f * Time.deltaTime), 0f);
            }
            else
            {
                decrouching = false;
                cam.localPosition = Vector3.zero;
            }
        }
    }

    private void Crouch()
    {
        if (slideCdTimer > 0f)
            slideCdTimer -= Time.deltaTime;
        if (crouchAct.ReadValue<float>() > 0f)
        {
            if (lockedSpeed > 2.6f && isGrounded)
            {
                if (!sliding && slideCdTimer <= 0f)
                {
                    if (lockedMove == Vector3.zero)
                    {
                        lockedMove = move;
                        lockedMove.Normalize();
                    }
                    sliding = true;
                    decrouching = false;
                    lockedMovement = true;
                    lockedSpeed = currSpeed * 1.4f;
                    controller.height = 2.23f;
                    lockedSpeedReset = 0f;
                    slideCdTimer = 1.7f;
                    controller.center = new Vector3(0f, -1.49f, 0f);
                    AnimCrouch(sliding);
                }
                else if (!sliding && slideCdTimer > 0f)
                {
                    if (lockedMove == Vector3.zero)
                    {
                        lockedMove = move;
                        lockedMove.Normalize();
                    }
                    sliding = true;
                    decrouching = false;
                    lockedMovement = true;
                    controller.height = 2.23f;
                    lockedSpeedReset = 0f;
                    controller.center = new Vector3(0f, -1.49f, 0f);
                    AnimCrouch(sliding);
                }
            }
            else if (currSpeed >= 11f && !crouch && isGrounded)
            {
                if (!sliding && slideCdTimer <= 0f)
                {
                    if (lockedMove == Vector3.zero)
                    {
                        lockedMove = move;
                        lockedMove.Normalize();
                    }
                    sliding = true;
                    decrouching = false;
                    lockedMovement = true;
                    lockedSpeedReset = 0f;
                    lockedSpeed = currSpeed * 1.4f;
                    controller.height = 2.23f;
                    slideCdTimer = 1.7f;
                    controller.center = new Vector3(0f, -1.49f, 0f);
                    AnimCrouch(sliding);
                }
                else if (!sliding && slideCdTimer > 0f)
                {
                    if (lockedMove == Vector3.zero)
                    {
                        lockedMove = move;
                        lockedMove.Normalize();
                    }
                    lockedSpeedReset = 0f;
                    sliding = true;
                    decrouching = false;
                    lockedMovement = true;
                    controller.height = 2.23f;
                    lockedSpeed = currSpeed * 0.45f;
                    controller.center = new Vector3(0f, -1.49f, 0f);
                    AnimCrouch(sliding);
                }
            }
            if (lockedSpeed <= 2.6f && isGrounded || lockedSpeed <= 2.6f && sliding)
            {
                crouch = true;
                decrouching = false;
                sliding = false;
                lockedMovement = false;
                controller.height = 2.23f;
                lockedSpeedReset = 0f;
                controller.center = new Vector3(0f, -1.49f, 0f);
                AnimCrouch(crouch); 
            }
            if (cam.localPosition.y > -0.96f && isGrounded)
            {
                if (crouch)
                {
                    cam.localPosition = new Vector3(0f, Mathf.MoveTowards(cam.localPosition.y, -0.96f, 3.4f * Time.deltaTime), 0f);

                    move = transform.right * hor + transform.forward * ver;

                    move.Normalize();

                    controller.Move(currSpeed * Time.deltaTime * move);
                }
                else if (sliding)
                {
                    cam.localPosition = new Vector3(0f, Mathf.MoveTowards(cam.localPosition.y, -0.96f, 3.4f * Time.deltaTime), 0f);
                }
            }
            else if (crouch && isGrounded)
            {
                move = transform.right * hor + transform.forward * ver;

                move.Normalize();

                controller.Move(0.45f * currSpeed * Time.deltaTime * move);
            }
            if (climb)
            {
                ClimbThings(false);
                lockedSpeedReset = 0f;
            }
        }
        if (isGrounded && !lockedMovement)
        {
            if(lockedSpeed > 0f || lockedMove != Vector3.zero)
            {
                if (lockedSpeedReset < 0.1f)
                    lockedSpeedReset += Time.deltaTime;
                if (lockedSpeedReset >= 0.1f)
                {
                    lockedSpeed = 0f;
                    lockedMove = Vector3.zero;
                }
            }
        }
    }

    public void AdsOn(bool ads)
    {
        this.ads = ads;
    }

    private void AnimCrouch(bool state)
    {
        animator.SetBool("Crouch", state);
    }

    private void AutoRun()
    {
        if (isGrounded)
        {
            if (rebindType != RebindType.CSGO && rebindType != RebindType.Valorant && ver >= 1f && !ads && !crouch && !climb &&
                fireAct.ReadValue<float>() <= 0f)
            {
                if (currSpeed < 11.5f)
                {
                    currSpeed += 92f * Time.deltaTime;
                }
                else if (currSpeed >= 11.5f)
                {
                    currSpeed = 11.5f;
                }
            }
            else if (ads && absVer >= absHor)
            {
                if (currSpeed < 0.7f * 8f * absVer)
                {
                    currSpeed += 0.7f * 64f * absVer * Time.deltaTime;
                }
                else if (currSpeed >= 0.7f * 8f * absVer)
                {
                    currSpeed = 0.7f * 8f * absVer;
                }
            }
            else if (ads && absVer < absHor)
            {
                if (currSpeed < 0.7f * 8f * absHor)
                {
                    currSpeed += 0.7f * 64f * absHor * Time.deltaTime;
                }
                else if (currSpeed >= 0.7f * 8f * absHor)
                {
                    currSpeed = 0.7f * 8f * absHor;
                }
            }
            else if (absVer >= absHor)
            {
                if (currSpeed < 8f * absVer)
                {
                    currSpeed += 64f * absVer * Time.deltaTime;
                }
                else if (currSpeed >= 8f * absVer)
                {
                    currSpeed = 8f * absVer;
                }
            }
            else
            {
                if (currSpeed < 8f * absHor)
                {
                    currSpeed += 64f * absHor * Time.deltaTime;
                }
                else if (currSpeed >= 8f * absHor)
                {
                    currSpeed = 8f * absHor;
                }
            }
            animator.SetFloat("Speed", currSpeed);
        }
        else
        {
            if (absVer >= absHor)
            {
                dopSpeed = 8f * absVer;
            }
            else
            {
                dopSpeed = 8f * absHor;
            }
            animator.SetFloat("Speed", 0f);
        }
    }

    private void Climb()
    {
        if (onWall && !climb && cantClimb <= 0f)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.9f, whatIsWall))
            {
                if (!isGrounded && !crouch && !sliding)
                {
                    float dotMove = Vector3.Dot(hit.transform.forward, move);
                    if (dotMove < -0.3f || dotMove > 0.3f)//0.3f
                    {
                        climb = true;
                        velocity.y = 0f;
                        startClimbing?.Invoke();
                    }
                    else
                    {
                        float dotMiniMove = Vector3.Dot(hit.transform.forward, miniMove);

                        if (dotMiniMove < -0.3f || dotMiniMove > 0.3f)//0.5f
                        {
                            climb = true;
                            velocity.y = 0f;
                            startClimbing?.Invoke();
                        }
                    }
                }
            }
        }
        else if (!onWall && climb)
        {
            ClimbThings(false);
        }
        if (cantClimb > 0f)
            cantClimb -= Time.deltaTime;
    }

    private void WallDetection()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit _, 0.9f, whatIsWall))
            onWall = true;
        else
            onWall = false;
    }

    public SettingsScriptableObject GetSettings()
    {
        return settings;
    }
}