using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum boundTypes
    {
        Static,
        Horizontal,
        Vertical,
        Basic
    }

    // BUGS: Going out of bounds puts the camera at a different bound instead of null

    public CollisionHandler target;
    public Vector2 focusAreaSize;
    public float verticalOffset;

    [Header("Camera Size")]
    float camVertSize, camHorzSize;

    [Header("Variables")]
    public float verticalSmoothTime;

    [Header("Bounds")]
    float leftBound,
          rightBound,
          bottomBound,
          topBound;

    [Header("Misc")]
    Camera cam;
    Vector2 focusPos;

    InputManager p;
    CollisionHandler c;
    PlayerController pC;

    public List<cameraBounds> bounds = new List<cameraBounds>();
    cameraBounds currentBound, previousBound;

    Vector3 idk;
    focusBounds focusArea;

    private void Start()
    {
        cam = GetComponent<Camera>();
        p = FindObjectOfType<InputManager>().GetComponent<InputManager>();
        c = target.GetComponent<CollisionHandler>();
        pC = target.GetComponent<PlayerController>();

        focusArea = new focusBounds(c.colliders.bounds, focusAreaSize);

        camVertSize = cam.orthographicSize;
        camHorzSize = cam.aspect * camVertSize;
    }

    private void Update()
    {
        focusArea.Update(c.colliders.bounds);

        if ((pC.velocity.x != 0 || pC.velocity.y != 0) || p.playerMovement.y != 0 || currentBound == null)
        {
            foreach (cameraBounds obj in bounds)
            {
                obj.getBounds();

                if (obj.isActive && target.transform.position.x < obj.left || target.transform.position.x > obj.right || target.transform.position.y > obj.top || target.transform.position.y < obj.bottom)
                {
                    obj.isActive = false;
                }

                if (!obj.isActive && target.transform.position.x > obj.left && target.transform.position.x < obj.right && target.transform.position.y < obj.top && target.transform.position.y > obj.bottom)
                {
                    obj.getBounds();
                    Bounds(obj);

                    if (currentBound != obj) currentBound = obj;

                    obj.isActive = true;
                }
            }
        }
    }

    private void LateUpdate()
    {
        focusArea.Update(c.colliders.bounds);

        focusPos = focusArea.center + Vector2.up * verticalOffset;

        if (currentBound.transitionTime != 0) CameraMovement(currentBound.transitionTime);
        else CameraMovement(2f);

        // Size Change
        if (currentBound.cameraSize != 0)
        {
            // THE LERP!
            if (currentBound.scaleTransitionTime != 0) cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentBound.cameraSize, currentBound.scaleTransitionTime * Time.deltaTime);
            else cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentBound.cameraSize, .25f * Time.deltaTime);

            ResetBounds();
        }
    }

    private void OnDrawGizmos()
    {
        // Basic Gizmos Draw
        Gizmos.color = new Color(0f, 0f, 1f, .75f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize);

        foreach (cameraBounds obj in bounds)
        {
            switch (obj.boundType)
            {
                case (boundTypes.Static):
                    Gizmos.color = new Color(1, 0, 0, .25f);
                    break;
                case (boundTypes.Basic):
                    Gizmos.color = new Color(0, 1, 0, .25f);
                    break;
                case (boundTypes.Horizontal):
                    Gizmos.color = new Color(0, 0, 1, .25f);
                    break;
                case (boundTypes.Vertical):
                    Gizmos.color = new Color(0, 0.5f, 0.5f, .25f);
                    break;
            }

            Gizmos.DrawCube(obj.center, obj.size);
        }
    }

    void CameraMovement(float transitionTime)
    {
        Bounds(currentBound);

        focusPos.x = Mathf.Clamp(focusArea.center.x, leftBound, rightBound);
        focusPos.y = Mathf.Clamp(focusArea.center.y, bottomBound, topBound);

        switch (currentBound.boundType)
        {
            case (boundTypes.Static):
                transform.position = Vector3.Lerp(transform.position, new Vector3(currentBound.center.x, currentBound.center.y + currentBound.verticalOffset, transform.position.z), transitionTime * Time.deltaTime);
                break;

            case (boundTypes.Basic):
                if (currentBound.lookAhead)
                {
                    if (p.playerMovement.x != 0)
                    {
                        focusPos.x = Mathf.Clamp(focusArea.center.x, leftBound + currentBound.lookAheadXDistance, rightBound - currentBound.lookAheadXDistance);

                        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(focusPos.x + currentBound.lookAheadXDistance * Mathf.Sign(p.playerMovement.x), focusPos.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                    }
                    else
                    {
                        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(focusPos.x, focusPos.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                    }
                }
                else transform.position = Vector3.Lerp(transform.position, new Vector3(focusPos.x, focusPos.y + currentBound.verticalOffset, transform.position.z), transitionTime * Time.deltaTime);
                break;

            case (boundTypes.Horizontal):
                if (currentBound.lookAhead)
                {
                    if (p.playerMovement.x != 0)
                    {
                        focusPos.x = Mathf.Clamp(focusArea.center.x, leftBound + currentBound.lookAheadXDistance, rightBound - currentBound.lookAheadXDistance);

                        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(focusPos.x + currentBound.lookAheadXDistance * Mathf.Sign(p.playerMovement.x), currentBound.center.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                    }
                    else
                    {
                        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(focusPos.x, currentBound.center.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                    }
                }
                else transform.position = Vector3.SmoothDamp(transform.position, new Vector3(focusPos.x, currentBound.center.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                break;

            case (boundTypes.Vertical):
                transform.position = Vector3.SmoothDamp(transform.position, new Vector3(currentBound.center.x, focusPos.y + currentBound.verticalOffset, transform.position.z), ref idk, transitionTime);
                break;
        }
    }

    void ResetBounds()
    {
        #region Reseting Values

        Bounds(currentBound);

        focusArea.Update(c.colliders.bounds);

        focusPos = focusArea.center + Vector2.up * verticalOffset;

        focusPos.x = Mathf.Clamp(target.transform.position.x, leftBound, rightBound);
        focusPos.y = Mathf.Clamp(target.transform.position.y, bottomBound, topBound);

        #endregion
    }

    void Bounds(cameraBounds obj)
    {
        camVertSize = cam.orthographicSize;
        camHorzSize = cam.aspect * camVertSize;

        leftBound = obj.left + camHorzSize;
        rightBound = obj.right - camHorzSize;
        topBound = obj.top - camVertSize;
        bottomBound = obj.bottom + camVertSize;
    }

    public struct focusBounds
    {
        public Vector2 center;
        public Vector2 velocity;
        float left, right, top, bottom;

        public focusBounds(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            top = targetBounds.min.y + size.y;
            bottom = targetBounds.min.y;

            velocity = Vector2.zero;
            center = new Vector2((left + right) / 2f, (top + bottom) / 2);
        }

        public void Update(Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }
            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }
            top += shiftY;
            bottom += shiftY;

            center = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(shiftX, shiftY);
        }
    }

    [System.Serializable]
    public class cameraBounds
    {
        public string Name;
        [Space(2)]
        public boundTypes boundType;
        [Space(2)]
        public Vector2 center;
        public Vector2 size;
        [Space(2)]
        public float cameraSize;
        public float transitionTime, scaleTransitionTime;
        [Space(2)]
        public bool lookAhead;
        public float verticalOffset, lookAheadXDistance, LookAheadSmoothTime;

        public bool isActive = false;
        internal float left, right, top, bottom;

        public void getBounds()
        {
            left = center.x - size.x / 2;
            right = center.x + size.x / 2;
            top = center.y + size.y / 2;
            bottom = center.y - size.y / 2;
        }
    }
}