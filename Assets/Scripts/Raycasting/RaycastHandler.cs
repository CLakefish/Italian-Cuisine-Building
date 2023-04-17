using System.Collections;
using UnityEngine;

public struct RaycastOrigins
{
    public Vector2 topLeft, topRight;
    public Vector2 bottomLeft, bottomRight;
}

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastHandler : MonoBehaviour
{
    internal BoxCollider2D colliders;
    internal RaycastOrigins raycastOrigins;

    [Header("Collision Layers")]
    public LayerMask collisionsMask;

    [Header("Raycasting Count")]
    internal int horizontalRayCount = 4,
                 verticalRayCount = 4;

    [Header("Raycasting Spacing")]
    internal float horizontalRaySpacing,
                 verticalRaySpacing;

    public const float playerWidth = .05f;
    const float rayBetweenDist = .25f;

    public virtual void Awake()
    {
        colliders = GetComponent<BoxCollider2D>();
    }

    public virtual void Start()
    {
        RaySpacing();
    }

    public void UpdateRaycastOrigins()
    {
        Bounds bounds = colliders.bounds;
        bounds.Expand(playerWidth * -2);

        #region Raycast Origins

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);

        #endregion
    }

    public void RaySpacing()
    {
        Bounds bounds = colliders.bounds;
        bounds.Expand(playerWidth * -2);

        float boundWidth = bounds.size.x,
              boundHeight = bounds.size.y;

        horizontalRayCount = Mathf.RoundToInt(boundHeight / rayBetweenDist);
        verticalRayCount = Mathf.RoundToInt(boundWidth / rayBetweenDist);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }
}
