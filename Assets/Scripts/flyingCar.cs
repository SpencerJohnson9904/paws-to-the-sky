using UnityEngine;

public class FlyingCar : MonoBehaviour
{
    [Header("Orbit")]
    [SerializeField] Transform platform;      // drag the platform it circles around
    [SerializeField] float radius = 5f;       // distance from platform center
    [SerializeField] float height = 3f;       // how high above the platform
    [SerializeField] float speed = 60f;       // degrees per second

    [Header("Knockback")]
    [SerializeField] float knockbackForce = 8f;
    [SerializeField] float knockbackUpward = 4f;

    float angle = 0f;

    void Update()
    {
        if (platform == null) return;

        angle += speed * Time.deltaTime;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        Vector3 targetPos = platform.position + new Vector3(x, height, z);
        transform.position = targetPos;

        // face the direction it's moving
        Vector3 nextX = Mathf.Cos((angle + 1f) * Mathf.Deg2Rad) * radius * Vector3.right;
        Vector3 nextZ = Mathf.Sin((angle + 1f) * Mathf.Deg2Rad) * radius * Vector3.forward;
        Vector3 nextPos = platform.position + nextX + nextZ + Vector3.up * height;
        transform.LookAt(nextPos);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
        if (rb == null) return;

        // knock in the direction the car is traveling
        Vector3 knockDir = collision.gameObject.transform.position - transform.position;
        knockDir.y = 0f;
        knockDir = knockDir.normalized;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockDir * knockbackForce + Vector3.up * knockbackUpward, ForceMode.Impulse);
    }
}