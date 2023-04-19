using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RaycastHandler), typeof (Components))]
public class CollisionHandler : Components
{
    public CollisionInfo collisions;
    internal Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    // Ref
    public void Move(Vector2 vel, bool onPlatform)
    {
        ApplyMotion(vel, new Vector2(0f, 0f), onPlatform);
    }


    // Apply motions
    public void ApplyMotion(Vector2 velocity, Vector2 input, bool onPlatform = false)
    {
        UpdateRaycastOrigins();

        collisions.Reset();
        playerInput = input;

        if (velocity.x != 0 && !p.onMovingPlatform && p.state != PlayerController.States.WallJumping) collisions.faceDir = (int)Mathf.Sign(velocity.x);

        HorizontalCollisions(ref velocity);

        if (velocity.y != 0) VerticalCollisions(ref velocity);

        transform.Translate(velocity);

        if (onPlatform) collisions.bottom = true;
    }


    // Horizontal Collision Handler
    void HorizontalCollisions(ref Vector2 velocity)
    {
        float dirX = collisions.faceDir;
        float rayLength = Mathf.Abs(velocity.x) + playerWidth;

        if (Mathf.Abs(velocity.x) < playerWidth) rayLength = 2 * playerWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (dirX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionsMask);
            Debug.DrawRay(rayOrigin, Vector2.right * dirX, Color.red);

            if (hit)
            {
                if ((p.inputManager.climb.pressed || p.inputManager.climb.held) && hit.collider.gameObject.layer == 8) p.onMovingPlatform = true;

                if (hit.collider.tag == "Spring") continue;
                if (hit.collider.tag == "ReverseThrough") continue;
                if (hit.collider.tag == "Hazard")
                {
                    Debug.Log("Dead");
                    p.state = PlayerController.States.Dead;
                    continue;
                }

                if (hit.collider.tag == "Respawn")
                {
                    p.spawnObj = hit.collider.gameObject.transform.position;
                    continue;
                }

                velocity.x = (hit.distance - playerWidth) * dirX;

                rayLength = hit.distance;

                collisions.left = dirX == 1;
                collisions.right = dirX == -1;
            }
        }
    }

    // Vertical Collision Handler
    void VerticalCollisions(ref Vector2 velocity)
    {
        float dirY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + playerWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (dirY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, collisionsMask);
            Debug.DrawRay(rayOrigin, Vector2.up * dirY, Color.red);

            if (hit)
            {
                if (hit.collider.tag == "Spring") continue;

                if (hit.collider.tag == "ReverseThroughFall")
                {
                    // If you're going through it
                    if (dirY == 1 || hit.distance == 0) continue;

                    if (collisions.fallingThroughPlatform) continue;

                    // Force Fall Through
                    if (p.inputManager.playerMovement.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", 0.5f);
                        continue;
                    }
                }

                if (hit.collider.tag == "Through")
                {
                    // Fall Through Platform
                    if (dirY == -1 || hit.distance == 0) continue;
                }

                if (hit.collider.tag == "ReverseThrough")
                {
                    // Simple 1-way
                    if (dirY == 1 || hit.distance == 0) continue;
                }

                if (hit.collider.tag == "Hazard")
                {
                    Debug.Log("Dead");
                    p.state = PlayerController.States.Dead;
                    continue;
                }

                if (hit.collider.tag == "Respawn")
                {
                    p.spawnObj = hit.collider.gameObject.transform.position;
                    continue;
                }

                velocity.y = (hit.distance - playerWidth) * dirY;
                rayLength = hit.distance;

                collisions.bottom = dirY == -1;
                collisions.top = dirY == 1;
            }
        }
    }

    public struct CollisionInfo
    {
        public bool top, bottom,
                    left, right;

        public int faceDir;
        public bool fallingThroughPlatform;

        public void Reset()
        {
            top = bottom = left = right = false;
        }
    }

}