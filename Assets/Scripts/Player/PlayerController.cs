using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (BoxCollider2D), typeof (InputManager))]
public class PlayerController : MonoBehaviour
{
    #region Parameters

    // Inputs
    internal InputManager inputManager;
    internal CollisionHandler c;

    [Space(3), Header("Gravity Parameters")]
    [SerializeField] float gravity;

    [Space(3), Header("Movement Parameters")]
    [SerializeField] float moveSpeed;
    [SerializeField] float accelerationGround,
                           accelerationAir,
                           decelerationGround,
                           decelerationAir;

    [Space(3), Header("Jump Parameters")]
    [SerializeField] float jumpHeight;
    [SerializeField] float jumpTime,
                           jumpBuffer,
                           jumpSmoothing,
                           coyoteTime;
    public float jumpBufferSeconds;

    [Space(3), Header("Wall Jump Parameters")]
    [SerializeField] Vector2 jumpWall;
    [SerializeField] float wallJumpTime;
    [SerializeField] float wallJumpCoyoteTime;
    [SerializeField] float wallJumpBufferSeconds;
    float wallJumpBuffer;
    internal bool climbing = false;
    internal bool onMovingPlatform = false;
    bool springAffect = false;

    #endregion

    #region Variables

    internal Vector3 spawnObj;

    // Player States
    public enum States
    {
        // Basic States
        Grounded,
        Jumping,
        Falling,
        Climbing,
        WallJumping,

        Dead,
    }

    [Header("Debugging")]
    // States
    public States state = States.Falling, prevState = States.Falling;
    float stateDur;

    // Velocity
    public Vector2 velocity;
    float velocityRef;

    #endregion

    private void Awake()
    {
        inputManager = GetComponent<InputManager>();
        c = GetComponent<CollisionHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        // Inputs
        Vector2 input = (state == States.WallJumping) ? new Vector2(0f, 0f) : inputManager.playerMovement.normalized;

        // Velocity
        float desiredVel = input.x * moveSpeed,
              groundAcceleration = (input.x != 0) ? accelerationGround : decelerationGround,
              airAcceleration = (input.x != 0) ? accelerationAir : decelerationAir,
              acceleration = c.collisions.bottom ? groundAcceleration : airAcceleration;

        // Initial Velocity Applications
        velocity.x = Mathf.SmoothDamp(velocity.x, desiredVel, ref velocityRef, acceleration);

        // Y Velocity Fixes
        if (!c.collisions.bottom && state != States.Climbing) velocity.y -= gravity * Time.deltaTime;

        jumpBufferSeconds = Mathf.MoveTowards(jumpBufferSeconds, 0f, Time.deltaTime);
        wallJumpBuffer = Mathf.MoveTowards(wallJumpBuffer, 0f, Time.deltaTime);

        // State Machine
        MainMovement(input);

        onMovingPlatform = false;

        // Velocity Fix
        velocity.y = Mathf.Clamp(velocity.y, -150f, Mathf.Infinity);

        // Movement
        if (state != States.Dead) c.Move(velocity * Time.deltaTime, false);
    }

    public void MainMovement(Vector2 input)
    {
        // State Change
        void ChangeState(States newState)
        {
            prevState = state;
            state = newState;
            stateDur = 0f;
        }

        // Inputs
        bool jumpDown = inputManager.jump.held,
             jumpReleased = !inputManager.jump.pressed;

        int wallDir = (c.collisions.left) ? -1 : 1;

        // On state enter
        if (stateDur == 0)
        {
            switch (state)
            {
                case (States.Grounded):
                    velocity.y = -.001f;
                    break;

                case (States.Falling):

                    break;

                case (States.Jumping):

                    velocity.y = 0f;
                    break;

                case (States.WallJumping):
                    velocity.y = velocity.x = 0f;
                    break;

                case (States.Climbing):
                    velocity.y = 0f;
                    break;
            }
        }

        stateDur += Time.deltaTime;

        switch (state)
        {
            case (States.Grounded):

                // Falling
                if (!c.collisions.bottom) ChangeState(States.Falling);

                // Jump States
                if ((jumpBufferSeconds > 0 || jumpDown))
                {
                    jumpBufferSeconds = 0f;
                    ChangeState(States.Jumping);
                }

                // Climbing
                if ((c.collisions.left || c.collisions.right) && (inputManager.climb.held || inputManager.climb.pressed)) ChangeState(States.Climbing);

                break;

            case (States.Falling):

                // Velocity Fixes
                if (c.collisions.top) velocity.y = 0f;

                // Jump Buffering
                if (jumpDown) jumpBufferSeconds = jumpBuffer;
                if (jumpDown && prevState == States.WallJumping) wallJumpBuffer = wallJumpBufferSeconds;
                if ((c.collisions.left || c.collisions.right) && jumpDown && wallJumpBuffer == 0) ChangeState(States.WallJumping);

                // Coyote Time
                if (prevState == States.Grounded && velocity.y < 0 && stateDur < coyoteTime && jumpDown)
                {
                    ChangeState(States.Jumping);
                    break;
                }

                if (prevState == States.Climbing && velocity.y < 0 && stateDur < wallJumpCoyoteTime && jumpDown)
                {
                    ChangeState(States.Jumping);
                    break;
                }

                if ((c.collisions.left || c.collisions.right) && (jumpDown || wallJumpBuffer > 0))
                {
                    wallJumpBuffer = 0f;
                    ChangeState(States.WallJumping);
                    break;
                }

                // State Changes
                if (c.collisions.bottom) ChangeState(States.Grounded);
                if ((c.collisions.left || c.collisions.right) && (inputManager.climb.held || inputManager.climb.pressed)) ChangeState(States.Climbing);
                if (!springAffect && (prevState == States.Jumping || prevState == States.WallJumping) && velocity.y >= 0 && jumpReleased) StartCoroutine(JumpCancel());

                break;

            case (States.Jumping):

                if ((c.collisions.left || c.collisions.right) && jumpDown) ChangeState(States.WallJumping);

                // Jump
                velocity.y = jumpHeight - (stateDur * jumpSmoothing);

                // Variablility
                if (jumpReleased)
                {
                    StartCoroutine(JumpCancel());
                    ChangeState(States.Falling);
                    break;
                }

                if (input.x != 0) velocity.x += (.15f * Mathf.Sign(input.x)) * (200f * Time.deltaTime);

                // States
                if (stateDur > jumpTime) ChangeState(States.Falling);
                if ((c.collisions.left || c.collisions.right) && (inputManager.climb.held || inputManager.climb.pressed)) ChangeState(States.Climbing);

                break;

            case (States.Climbing):

                // Ensure other objs know you're climbing
                climbing = true;

                // Wall movement
                if (input.y != 0)
                {
                    velocity.y = 16f * Mathf.Sign(input.y);
                }
                else velocity.y = 0f;

                // Wall Jump
                if (jumpDown)
                {
                    climbing = false;
                    ChangeState(States.WallJumping);
                    break;
                }

                // State Changes
                if ((!c.collisions.left && !c.collisions.right) || inputManager.climb.released || stateDur > 1f)
                {
                    climbing = false;

                    if (c.collisions.bottom) ChangeState(States.Grounded);
                    else
                        ChangeState(States.Falling);

                    break;
                }

                break;

            case (States.WallJumping):

                velocity = new Vector2(0f, 0f);
                velocity += new Vector2(jumpWall.x * wallDir, jumpWall.y);

                // Variable jump height
                if (jumpReleased)
                {
                    StartCoroutine(JumpCancel());
                    velocity.x = Mathf.Lerp(velocity.x, 0f, 5f * Time.deltaTime);
                    ChangeState(States.Falling);
                    break;
                }

                // State Changes
                if (stateDur > wallJumpTime)
                {
                    ChangeState(States.Falling);
                    break;
                }
                //if (Mathf.Abs(velocity.x) == jumpWall.x) ChangeState(States.Falling);

                break;

            case (States.Dead):

                if (stateDur > 0.2f)
                {
                    transform.position = spawnObj;
                    ChangeState(States.Falling);
                }

                break;
        }
    }

    IEnumerator JumpCancel()
    {
        while (!c.collisions.bottom && !c.collisions.right && !c.collisions.left && velocity.y > 0)
        {
            Debug.Log("hsfadgfasdf");

            velocity.y = Mathf.MoveTowards(velocity.y, 0, 10f * Time.deltaTime);
            yield return null;
        }
    }

    public IEnumerator spring(float x, float y, bool left, bool vertical, GameObject obj)
    {
        if (!vertical) transform.position = Vector3.Lerp(transform.position, new Vector3(obj.transform.position.x + ((left) ? -obj.GetComponent<SpriteRenderer>().bounds.size.x : obj.GetComponent<SpriteRenderer>().bounds.size.x), obj.transform.position.y, obj.transform.position.z), 15f * Time.deltaTime);
        yield return new WaitForSeconds(.0000001f);

        springAffect = true;

        state = prevState =  States.Falling;

        velocity.y = 0f;
        if (!vertical) velocity.x = 0f; 

        if (left)
        {
            velocity.y = y;
           if (inputManager.playerMovement.x != 0) velocity.x = -x;
        }
        else
        {
            velocity.y = y;
            if (inputManager.playerMovement.x != 0) velocity.x = x;
        }

        if (inputManager.jump.released) StartCoroutine(JumpCancel());

        yield return new WaitForSeconds(.25f * Time.deltaTime);

        velocity.y = Mathf.Lerp(velocity.y, 0f, 2f * Time.deltaTime);

        yield return new WaitForSeconds(2f * Time.deltaTime);

        springAffect = false;
    }


}
