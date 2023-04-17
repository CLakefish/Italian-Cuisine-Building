using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchingObj : Components
{
    [Space(3), Header("Layers to Effect")]
    public LayerMask objMask;

    [Space(3), Header("Positions")]
    public Vector3[] targetPosition;
    Vector3[] globalPos;

    [Space(3), Header("Movement")]
    public float speed;
    public float waitTime,
                 easeAmount;

    [Space(3), Header("Cycle")]
    public bool cycle;

    public bool ease,
                launch;

    int targetPosI;
    float percentageBetween,
          nextMoveTime;

    public float launchX,
                 launchY;

    public bool requireTouch;
    public bool isTouched;
    bool isAble = false;
    Vector3 originPos;


    List<ObjMovement> objMovement;
    Dictionary<Transform, CollisionHandler> objDictionary = new Dictionary<Transform, CollisionHandler>();

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        originPos = transform.position;

        globalPos = new Vector3[targetPosition.Length];

        for (int i = 0; i < targetPosition.Length; i++)
        {
            globalPos[i] = targetPosition[i] + transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (requireTouch)
        {
            // RayOrigins
            UpdateRaycastOrigins();

            // Current Platform Velocity
            Vector3 vel = platformMovementCheck();

            // Calculate Velocity
            MovementCalculations(vel);

            if (isTouched || isAble)
            {

                if (isTouched) MoveObj(true);

                if (isAble) transform.Translate(vel);

                MoveObj(false);
            }
        }
    }

    Vector3 platformMovementCheck()
    {
        if (transform.position == originPos && !isTouched && isAble)
        {
            isAble = false;

            return Vector2.zero;
        }

        if (isAble)
        {
            if (Time.time < nextMoveTime) return Vector2.zero;

            targetPosI %= globalPos.Length;

            int toI = (targetPosI + 1) % globalPos.Length;
            float distBetweenI = Vector3.Distance(globalPos[targetPosI], globalPos[toI]);

            percentageBetween += Time.deltaTime * speed / distBetweenI;
            percentageBetween = Mathf.Clamp01(percentageBetween);

            float easedPercent = 0f;

            if (ease) easedPercent = Ease(percentageBetween);
            if (launch) easedPercent = EaseOut(percentageBetween);

            Vector3 newPos = Vector3.Lerp(globalPos[targetPosI], globalPos[toI], easedPercent);

            if (launch && percentageBetween >= .5 && percentageBetween <= .95)
            {
                if (isTouched && (p.inputManager.jump.pressed || p.inputManager.jump.held))
                {
                    p.StartCoroutine(p.spring(launchX, launchY, false, true, gameObject)); 

                    isTouched = false;
                }
            }

            if (percentageBetween >= 1)
            {
                percentageBetween = 0f;
                targetPosI++;

                if (!cycle)
                {
                    if (targetPosI >= globalPos.Length - 1)
                    {
                        targetPosI = 0;
                        System.Array.Reverse(globalPos);
                    }
                }
                nextMoveTime = Time.time + waitTime;
            }

            return newPos - transform.position;
        }
        else
        {
            return Vector2.zero;
        }

    }

    void MoveObj(bool beforeMove)
    {
        foreach (ObjMovement obj in objMovement)
        {
            if (!objDictionary.ContainsKey(obj.transform))
            {
                objDictionary.Add(obj.transform, obj.transform.GetComponent<CollisionHandler>());
            }

            if (obj.moveBeforePlatform == beforeMove)
            {
                objDictionary[obj.transform].Move(obj.desiredVel, obj.onPlatform);
            }
        }
    }

    void MovementCalculations(Vector3 velocity)
    {
        float dirX = Mathf.Sign(velocity.x),
        dirY = Mathf.Sign(velocity.y);

        float playerXDir = Mathf.Sign(p.transform.position.x - transform.position.x);

        HashSet<Transform> movedObj = new HashSet<Transform>();

        objMovement = new List<ObjMovement>();

        float hitCount = 0f;

        float rayLengthV = Mathf.Abs(velocity.y) + playerWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            // RaycastOrigin is the struct
            Vector2 rayOrigin = dirY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            // Spacing
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            // Raycast
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLengthV, objMask);

            // Debugging
            Debug.DrawRay(rayOrigin, Vector2.up * dirY, Color.red);

            if (hit)
            {
                if (!movedObj.Contains(hit.transform))
                {
                    movedObj.Add(hit.transform);

                    if (!isTouched)
                    {
                        isTouched = true;
                    }

                    hitCount++;
                    isAble = true;

                    float pushY = velocity.y - (hit.distance - playerWidth) * dirY,
                            pushX = velocity.x;

                    objMovement.Add(new ObjMovement(hit.transform, new Vector3(pushX, pushY), dirY == 1, true));
                }
            }
        }

        if (dirY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = playerWidth * 2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i + velocity.x);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, objMask);

                if (hit)
                {
                    if (!movedObj.Contains(hit.transform))
                    {
                        movedObj.Add(hit.transform);

                        isTouched = true;

                        float pushY = velocity.y,
                              pushX = velocity.x;

                        objMovement.Add(new ObjMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }

        if (hitCount == 0 && isTouched) isTouched = false;


        if (p.onMovingPlatform)
        {
            float rayLength2 = playerWidth * 10f;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (playerXDir == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

                rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * playerXDir, rayLength2, collisionsMask);

                Debug.DrawRay(rayOrigin, Vector2.right * playerXDir, Color.red);

                if (hit)
                {
                    if (!movedObj.Contains(hit.transform))
                    {
                        movedObj.Add(hit.transform);

                        isTouched = true;

                        float pushY = velocity.y,
                              pushX = velocity.x;

                        objMovement.Add(new ObjMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }
        else
        {
            float rayLength = Mathf.Abs(velocity.x) + playerWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (playerXDir == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

                rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * playerXDir, rayLength, objMask);

                if (hit)
                {
                    if (!movedObj.Contains(hit.transform))
                    {
                        movedObj.Add(hit.transform);

                        if (!isTouched)
                        {
                            isTouched = true;
                        }

                        isAble = true;

                        float pushX = velocity.x - (hit.distance - playerWidth) * playerXDir,
                              pushY = 0f;

                        objMovement.Add(new ObjMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (targetPosition != null)
        {
            Gizmos.color = Color.red;
            float size = .3f;

            for (int i = 0; i < targetPosition.Length; i++)
            {
                Vector3 globalWaypointPos = targetPosition[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }

    // Basic Ease
    float Ease(float x)
    {
        // y = x^a/x^a + (1-x)^a
        float a = easeAmount + 1;

        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }


    // Complex? yes. Cool? Yes
    float EaseOut(float x)
    {
        return x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? Mathf.Pow(2, 20 * x - 10) / 2 : (2 - Mathf.Pow(2, -20 * x + 10)) / 2;
    }
}
