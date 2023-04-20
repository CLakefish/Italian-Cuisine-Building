using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingObj : Components
{
    #region Parameters

    public GameObject origins;

    [Header("Fall-Speed")]
    public float fallSpeed;

    #endregion

    #region Variables

    [Header("Changables")]
    public bool triggeredUp = false;
    public bool stable = true, hitGround, isTouched = false;

    List<ObjMovement> objMovement;
    Dictionary<Transform, CollisionHandler> objDictionary = new Dictionary<Transform, CollisionHandler>();
    Vector3 vel;

    #endregion

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();

        vel = new Vector3(0f, fallSpeed - Time.fixedDeltaTime, 0f);

        if (!hitGround) checkPlayerDir(vel);

        if (!stable)
        {
            MoveObj(false);

            transform.Translate(vel * Time.deltaTime);

            MoveObj(true);

            fallSpeed -= .25f;
            fallSpeed = Mathf.Clamp(fallSpeed, -75f, Mathf.Infinity);
        }
    }

    void MoveObj(bool beforeMove)
    {
        if (objMovement.Count == 0) return;

        foreach (ObjMovement obj in objMovement)
        {
            if (!objDictionary.ContainsKey(obj.transform))
            {
                objDictionary.Add(obj.transform, obj.transform.GetComponent<CollisionHandler>());
            }

            if (obj.moveBeforePlatform == beforeMove)
            {
                if (objDictionary[obj.transform] == null) return;

                objDictionary[obj.transform].Move(obj.desiredVel * Time.deltaTime, obj.onPlatform);
            }
        }
    }

    void checkPlayerDir(Vector3 velocity)
    {
        HashSet<Transform> movedObj = new HashSet<Transform>();

        objMovement = new List<ObjMovement>();

        float dirX = -Mathf.Sign(p.velocity.x),
              dirY = (stable) ? 1 : -1;

        float rayLength = playerWidth * 2f;
        float xLength = playerWidth * 15f;

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
                if (dirY == 1 && triggeredUp && !isTouched)
                {
                    isTouched = true;
                    StartCoroutine(blockFall());
                }

                if (dirY == -1 && !stable && !hitGround)
                {
                    if (hit.collider.gameObject.layer == 7) continue;

                    hitGround = true;
                    StartCoroutine(ShakeObject());

                    stable = true;
                    velocity.y = 0;
                }

                if (dirY == -1 && hit.collider.gameObject.layer == 7)
                {
                    Debug.Log("dead!");
                    continue;
                }
            }
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (dirX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionsMask);

            Debug.DrawRay(rayOrigin, Vector2.right * dirX, Color.red);

            if (hit)
            {
                if (hit.collider.tag == "Player" && !isTouched)
                {
                    isTouched = true;
                    StartCoroutine(blockFall());
                }
            }
        }

        if (p.onMovingPlatform)
        {
            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (dirX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

                rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, xLength, collisionsMask);

                Debug.DrawRay(rayOrigin, Vector2.right * dirX, Color.red);

                if (hit)
                {
                    if (!movedObj.Contains(hit.transform))
                    {
                        movedObj.Add(hit.transform);

                        float pushY = velocity.y,
                              pushX = velocity.x;

                        objMovement.Add(new ObjMovement(hit.transform, new Vector3(pushX, pushY), true, true));
                    }
                }
            }
        }
    }
    IEnumerator blockFall()
    {
        StartCoroutine(ShakeObject());
        yield return new WaitForSeconds(.5f);

        stable = false;
    }

    IEnumerator ShakeObject()
    {
        Vector3 originPosition = transform.position;

        while (stable)
        {
            transform.position = originPosition + Random.insideUnitSphere * .05f;

            yield return new WaitForSeconds(.075f);
        }

        transform.position = originPosition;

        yield return null;
    }
}
