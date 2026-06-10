using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class cameraFollow : MonoBehaviour
{
    public Transform cat;
    public float distance = 5f;
    public float height = 2f;
    public float smoothSpeed = 5f;
    public float tiltAngle = 10f;

    [Header("Free Look")]
    [Tooltip("Hold right mouse and move the mouse to swivel the camera around the cat. Degrees of swivel per unit of mouse delta.")]
    public float lookSensitivity = 0.2f;
    [Tooltip("Lowest the free-look can pitch (degrees). Stops the camera flipping under the cat.")]
    public float pitchMin = -30f;
    [Tooltip("Highest the free-look can pitch (degrees). Stops the camera flipping over the cat.")]
    public float pitchMax = 70f;
    [Tooltip("How fast the swivel eases back behind the cat after a jump direction is locked.")]
    public float recenterSpeed = 5f;

    // Persistent orbit offset applied on top of the cat-following rotation.
    float yawOffset;
    float pitchOffset;
    // True while we're easing the offsets back to zero after a jump lock.
    bool recentering;
    // True while the right mouse button is held for a swivel.
    bool dragging;
    // Cached so we can unsubscribe; may be null if the cat has no PlayerMovement.
    PlayerMovement playerMovement;

    [Header("Occlusion")]
    [Tooltip("Objects between the camera and the cat on these layers get hidden so they never block the view.")]
    public LayerMask occlusionMask = ~0;
    [Tooltip("Radius of the cast toward the cat. Larger values hide objects that clip the edges of the view.")]
    public float occlusionRadius = 0.4f;
    [Tooltip("How far short of the cat to stop checking, so the cat's own colliders are never hidden.")]
    public float occlusionPadding = 0.5f;
    [Tooltip("Extra height above the cat's head before an object counts as blocking. Keeps the ground/platform the cat stands on visible.")]
    public float occlusionHeightBuffer = 0.1f;

    // Renderers we've hidden this frame so we can restore them once they no longer block the view.
    readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
    readonly HashSet<Renderer> stillBlocking = new HashSet<Renderer>();
    readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    // Distance from the cat's pivot up to the top of its bounds, so we only hide
    // things above the cat's head rather than the surface it's standing on.
    float catTopOffset = 1f;

    void OnEnable()
    {
        if (cat == null) return;

        playerMovement = cat.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.JumpDirectionLocked += OnJumpDirectionLocked;
    }

    void OnJumpDirectionLocked()
    {
        // Ease the free-look back behind the cat for the jump itself.
        recentering = true;
    }

    void Start()
    {
        if (cat == null) return;

        bool found = false;
        Bounds b = new Bounds(cat.position, Vector3.zero);
        foreach (var r in cat.GetComponentsInChildren<Renderer>())
        {
            if (!found) { b = r.bounds; found = true; }
            else b.Encapsulate(r.bounds);
        }
        if (found) catTopOffset = b.max.y - cat.position.y;
    }

    void LateUpdate()
    {
        if (cat == null) return;

        HandleFreeLookInput();

        if (recentering)
        {
            yawOffset = FreeLookMath.StepRecenter(yawOffset, recenterSpeed, Time.deltaTime);
            pitchOffset = FreeLookMath.StepRecenter(pitchOffset, recenterSpeed, Time.deltaTime);
            if (Mathf.Abs(yawOffset) < 0.05f && Mathf.Abs(pitchOffset) < 0.05f)
            {
                yawOffset = 0f;
                pitchOffset = 0f;
                recentering = false;
            }
        }

        Quaternion targetRotation =
            FreeLookMath.OrbitRotation(cat.forward, yawOffset, pitchOffset, tiltAngle);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );

        Vector3 desiredPosition = cat.position
            - transform.forward * distance
            + Vector3.up * height;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );

        UpdateOcclusion();
    }

    void HandleFreeLookInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Don't allow swivel before the game has started.
        if (!GameOptions.GameStarted)
        {
            if (dragging) EndDrag();
            return;
        }

        if (mouse.rightButton.wasPressedThisFrame)
            BeginDrag();

        if (dragging && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            Vector2 offsets = FreeLookMath.ApplyLookDelta(
                new Vector2(yawOffset, pitchOffset), delta,
                lookSensitivity, pitchMin, pitchMax);
            yawOffset = offsets.x;
            pitchOffset = offsets.y;
            recentering = false; // The player has taken manual control.
        }

        if (mouse.rightButton.wasReleasedThisFrame)
            EndDrag();
    }

    void BeginDrag()
    {
        dragging = true;
        recentering = false;
        // Pen the cursor inside the window but keep it visible (locked, not hidden).
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void EndDrag()
    {
        dragging = false;
        Cursor.lockState = CursorLockMode.None;
    }

    void UpdateOcclusion()
    {
        if (cat == null) return;

        stillBlocking.Clear();

        Vector3 origin = transform.position;
        Vector3 toCat = cat.position - origin;
        float castDistance = toCat.magnitude - occlusionPadding;

        if (castDistance > 0f)
        {
            Vector3 dir = toCat.normalized;
            int count = Physics.SphereCastNonAlloc(
                origin, occlusionRadius, dir, hitBuffer, castDistance,
                occlusionMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform t = hitBuffer[i].transform;
                if (t == null || t == cat || t.IsChildOf(cat)) continue;

                // Only hide things above the cat's head. Ground and the blocks the
                // cat stands on sit around its feet, so they stay visible.
                if (hitBuffer[i].point.y <= cat.position.y + catTopOffset + occlusionHeightBuffer) continue;

                foreach (var r in t.GetComponentsInChildren<Renderer>())
                {
                    if (r.enabled) r.enabled = false;
                    stillBlocking.Add(r);
                }
            }
        }

        // Restore anything that was hidden last frame but is no longer in the way.
        foreach (var r in hidden)
        {
            if (r != null && !stillBlocking.Contains(r))
                r.enabled = true;
        }

        hidden.Clear();
        foreach (var r in stillBlocking)
            hidden.Add(r);
    }

    void OnDisable()
    {
        // Don't leave the world full of invisible geometry if this component is turned off.
        foreach (var r in hidden)
            if (r != null) r.enabled = true;
        hidden.Clear();

        if (playerMovement != null)
            playerMovement.JumpDirectionLocked -= OnJumpDirectionLocked;

        // Never leave the cursor confined if we're torn down mid-drag.
        if (dragging) EndDrag();
    }
}
