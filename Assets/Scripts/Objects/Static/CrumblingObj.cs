using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrumblingObj : Components
{
    public bool isTouched = false;
    public bool YTrigger = true;
    public bool isActive = false;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycastOrigins();

        getDir();
    }

    void getDir()
    {
        float dirX = 0;
        float dirY = 0f;

        float rayLength = playerWidth * 2f;

        if (p.velocity.x != 0) dirX = -Mathf.Sign(p.velocity.x);
        if (p.velocity.y != 0) dirY = -Mathf.Sign(p.velocity.y);

        if (YTrigger)
        {
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
                    if (!isActive && !isTouched && hit.collider.tag == "Player") StartCoroutine(fall());
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
                if (!isActive && !isTouched && hit.collider.tag == "Player") StartCoroutine(fall());
            }
        }
    }

    IEnumerator fall()
    {
        isActive = true;
        sp.color = Color.gray;
        yield return new WaitForSeconds(.25f);

        colliders.enabled = false;

        sp.color = Color.black;

        yield return new WaitForSeconds(1.25f);

        sp.color = Color.gray;
        yield return new WaitForSeconds(.3f);

        sp.color = Color.white;
        isTouched = false;
        colliders.enabled = true;
        isActive = false;
    }
}

