using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    enum AimState { Aiming, Locked, Charging, Airborne }

    [Header("Aim")]
    [SerializeField] Transform aimArrow;
    [SerializeField] Transform visualModel;
    [SerializeField] float aimSweepAngle = 60f;
    [SerializeField] float aimSweepSpeed = 2f;

    [Header("Jump Power")]
    [SerializeField] float minVerticalJumpForce = 4f;
    [SerializeField] float maxVerticalJumpForce = 9f;
    [SerializeField] float minHorizontalJumpForce = 2f;
    [SerializeField] float maxHorizontalJumpForce = 8f;
    [SerializeField] float maxChargeTime = 1.5f;

    [Header("Squish")]
    [SerializeField] float squishAmount = 0.6f;
    [SerializeField] float squishSpeed = 5f;

    Rigidbody rb;
    AimState state = AimState.Aiming;
    float chargeTime;
    float currentYaw;
    bool leftGroundSinceJump;
    bool grounded;
    Vector3 originalScale;

    public float ChargeFraction => Mathf.Clamp01(chargeTime / maxChargeTime);

    //debug
    void Start()
    {
        originalScale = visualModel != null ? visualModel.localScale : transform.localScale;
        Debug.Log("Original Scale: " + originalScale);
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // The aim arrow is a visual indicator. Its child meshes ship with
        // colliders that, as children of this Rigidbody, would otherwise fold
        // into the cat's compound collider and ram nearby geometry as it sweeps.
        if (aimArrow != null)
            foreach (var col in aimArrow.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
    }

    void Update()
    {
        var space = Keyboard.current?.spaceKey;
        if (space == null) return;

        switch (state)
        {
            case AimState.Aiming:
                UpdateArrowOscillation();
                if (space.wasPressedThisFrame)
                {
                    transform.Rotate(0f, currentYaw, 0f);
                    if (aimArrow != null) aimArrow.localRotation = Quaternion.identity;
                    state = AimState.Locked;
                }
                break;

            case AimState.Locked:
                if (space.wasPressedThisFrame)
                {
                    state = AimState.Charging;
                    chargeTime = 0f;
                    StartSquish();
                }
                break;

            case AimState.Charging:
                chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);
                if (space.wasReleasedThisFrame)
                {
                    StopSquish();
                    Jump(ChargeFraction);
                    state = AimState.Airborne;
                    leftGroundSinceJump = false;
                    chargeTime = 0f;
                    if (aimArrow != null) aimArrow.gameObject.SetActive(false);
                }
                break;

            case AimState.Airborne:
                if (!grounded)
                {
                    leftGroundSinceJump = true;
                }
                else if (leftGroundSinceJump)
                {
                    state = AimState.Aiming;
                    if (aimArrow != null) aimArrow.gameObject.SetActive(true);
                }
                break;
        }
    }

    void UpdateArrowOscillation()
    {
        currentYaw = Mathf.Sin(Time.time * aimSweepSpeed) * aimSweepAngle;
        if (aimArrow != null) aimArrow.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    void Jump(float chargeFraction)
    {
        float vertical = Mathf.Lerp(minVerticalJumpForce, maxVerticalJumpForce, chargeFraction);
        float horizontal = Mathf.Lerp(minHorizontalJumpForce, maxHorizontalJumpForce, chargeFraction);

        Vector3 dir = visualModel != null ? visualModel.forward : transform.forward;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;

        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(dir * horizontal + Vector3.up * vertical, ForceMode.Impulse);
    }


    //Squish animation
    void StartSquish()
    {
        StopAllCoroutines();
        StartCoroutine(SquishRoutine());
    }

    void StopSquish()
    {
        StopAllCoroutines();
        StartCoroutine(SpringBack());
    }

    IEnumerator SquishRoutine()
    {

        Transform t = visualModel != null ? visualModel : transform;

        while (true)
        {
            float squish = Mathf.Lerp(1f, squishAmount, ChargeFraction);
            Vector3 target = new Vector3(
                originalScale.x * Mathf.Lerp(1f, 1.1f, ChargeFraction), // barely widens
                originalScale.y * squish,                                  // slightly squishes down
                originalScale.z * Mathf.Lerp(1f, 1.1f, ChargeFraction)
            );

            t.localScale = Vector3.Lerp(t.localScale, target, Time.deltaTime * squishSpeed);
            yield return null;
        }

    }
    IEnumerator SpringBack()
    {
        Transform t = visualModel != null ? visualModel : transform;

        while (Vector3.Distance(t.localScale, originalScale) > 0.01f)
        {
            t.localScale = Vector3.Lerp(t.localScale, originalScale, Time.deltaTime * squishSpeed * 2f);
            yield return null;
        }
        t.localScale = originalScale;
    }





    void FixedUpdate()
    {
        grounded = false;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                grounded = true;
                return;
            }
        }
    }

    /// <summary>
    /// Called by CheckpointManager after a respawn so the player can aim immediately.
    /// </summary>
    public void ResetState()
    {
        state = AimState.Aiming;
        chargeTime = 0f;
        leftGroundSinceJump = false;
        currentYaw = 0f;
        if (aimArrow != null)
        {
            aimArrow.gameObject.SetActive(true);
            aimArrow.localRotation = Quaternion.identity;
        }
    }
}
