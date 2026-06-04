using System.Collections.Generic;
using UnityEngine;

public class cameraFollow : MonoBehaviour
{
    public Transform cat;
    public float distance = 5f;
    public float height = 2f;
    public float smoothSpeed = 5f;
    public float tiltAngle = 10f;

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
    Quaternion targetRotation = Quaternion.LookRotation(cat.forward);
    targetRotation *= Quaternion.Euler(tiltAngle, 0, 0);
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

        //transform.LookAt(cat.position + Vector3.up * 1f);

        UpdateOcclusion();
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
    }
}
