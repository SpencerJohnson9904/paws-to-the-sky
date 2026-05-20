using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float minJumpForce = 5f;
    [SerializeField] float maxJumpForce = 15f;
    [SerializeField] float maxChargeTime = 1.5f;

    [SerializeField] float groundCheckDistance = 0.15f;
    [SerializeField] LayerMask groundLayers = ~0;

    Rigidbody rb;
    float chargeTime;
    bool isCharging;

    public float ChargeFraction => Mathf.Clamp01(chargeTime / maxChargeTime);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        var space = Keyboard.current?.spaceKey;
        if (space == null) return;

        if (space.wasPressedThisFrame && IsGrounded())
        {
            isCharging = true;
            chargeTime = 0f;
        }

        if (isCharging)
        {
            chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);

            if (space.wasReleasedThisFrame)
            {
                Jump(ChargeFraction);
                isCharging = false;
                chargeTime = 0f;
            }
        }
    }

    void Jump(float chargeFraction)
    {
        float force = Mathf.Lerp(minJumpForce, maxJumpForce, chargeFraction);
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
    }

    bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position + Vector3.up * 0.05f,
            Vector3.down,
            groundCheckDistance + 0.05f,
            groundLayers,
            QueryTriggerInteraction.Ignore);
    }
}
