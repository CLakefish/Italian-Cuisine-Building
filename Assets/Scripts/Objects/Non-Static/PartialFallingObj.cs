using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartialFallingObj : Components
{
    public Transform origins;
    public Transform offset;

    [Header("Fall-Speed")]
    public float fallSpeed;

    List<ObjMovement> objMovement;
    Dictionary<Transform, CollisionHandler> objDictionary = new Dictionary<Transform, CollisionHandler>();

    public bool isTouched = false;
    public bool isAble = false;
    bool animated = false;

    Vector3 vel;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        // RayOrigins
        UpdateRaycastOrigins();

        // Current Platform Velocity
        vel = (isTouched) ? new Vector3(0f, fallSpeed, 0f) : (animated) ? new Vector3(0f, Mathf.Lerp(transform.position.y, (transform.position.y - (10f - Mathf.Abs(origins.transform.position.y - transform.position.y))), .8f), transform.position.z) : (origins.transform.position - transform.position) * 2f;

        if (!isAble && offset != null) transform.position = offset.transform.position;

        // Calculate Velocity
        checkPlayerDir(vel);

        if (isTouched) MoveObj(false);

        if (isAble) transform.Translate(vel * Time.deltaTime);
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
                objDictionary[obj.transform].Move(obj.desiredVel * Time.deltaTime, obj.onPlatform);
            }
        }
    }

    void checkPlayerDir(Vector3 velocity)
    {
        HashSet<Transform> movedObj = new HashSet<Transform>();

        objMovement = new List<ObjMovement>();

        float hitCount = 0f;

        float dirX = Mathf.Sign(p.transform.position.x - transform.position.x),
              dirY = 1;

        float rayLength = playerWidth * 2f;

        for (int i = 0; i < verticalRayCount; i++)
        {

            // RaycastOrigin is the struct
            Vector2 rayOrigin = dirY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            // Spacing
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            // Raycast
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, collisionsMask);

            // Debugging
            Debug.DrawRay(rayOrigin, Vector2.up * dirY, Color.red);

            if (hit)
            {
                hitCount++;

                isAble = true;

                if (!isTouched)
                {
                    isTouched = true;
                }
            }
        }

        if ((p.inputManager.jump.pressed || p.state == PlayerController.States.Jumping) && isAble && isTouched)
        {
            StartCoroutine(jumpFix());
            isTouched = false;
        }

        if (hitCount == 0 && isTouched) isTouched = false;

        if (dirY == 1 || velocity.y == 0 && velocity.x != 0)
        {
            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i + velocity.x);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength * 1.5f, collisionsMask);

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
    }

    IEnumerator jumpFix()
    {
        animated = true;

        yield return new WaitForSeconds(.1f);

        animated = false;
    }
}
