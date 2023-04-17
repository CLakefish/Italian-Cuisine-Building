using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Horizontal,
    Vertical
}

public class Spring : Components
{
    [Header("Directons")]
    public Direction springDir;

    [Header("Force")]
    public float xForce;
    public float yForce;

    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();

        raycastingDur();
    }

    void raycastingDur()
    {
        float dirX = Mathf.Sign(p.transform.position.x - transform.position.x),
              dirY = Mathf.Sign(p.transform.position.y - transform.position.y);

        float rayLength = playerWidth * 5f;

        if (colliders.isActiveAndEnabled)
        {
            switch (springDir)
            {
                case (Direction.Horizontal):
                    for (int i = 0; i < horizontalRayCount; i++)
                    {
                        Vector2 rayOrigin = (dirX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

                        rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionsMask);

                        Debug.DrawRay(rayOrigin, Vector2.right * dirX, Color.red);

                        if (hit)
                        {
                            if (hit.collider.gameObject.layer == 7)
                            p.StartCoroutine(p.spring(xForce, yForce, dirX == -1, false, gameObject));
                        }
                    }
                    break;

                case (Direction.Vertical):
                    for (int i = 0; i < verticalRayCount; i++)
                    {
                        // RaycastOrigin is the struct
                        Vector2 rayOrigin = dirY == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                        // Spacing
                        rayOrigin += Vector2.right * (verticalRaySpacing * i);
                        // Raycast
                        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, collisionsMask);

                        // Debugging
                        Debug.DrawRay(rayOrigin, Vector2.up * dirY, Color.red);

                        if (hit)
                        {
                            if (hit.collider.gameObject.layer == 7)
                                p.StartCoroutine(p.spring(xForce, yForce, dirY == -1, true, gameObject));
                        }
                    }
                    break;
            }
        }
    }
}
