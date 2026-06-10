using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] float snapDistance = 1.5f;   // how close the cat needs to be above the car
    [SerializeField] float snapHeight = 0.8f;      // how high above the car the cat snaps to

    Transform player;
    Rigidbody playerRb;
    bool playerOnTop = false;

    void Update()
    {
        if (player == null || playerOnTop) return;

        // check if cat is above the car and within snap distance
        Vector3 toPlayer = player.position - transform.position;
        float horizontalDist = new Vector2(toPlayer.x, toPlayer.z).magnitude;
        float verticalDist = toPlayer.y;

        if (horizontalDist < snapDistance && verticalDist > 0f && verticalDist < snapDistance)
        {
            SnapPlayerOnTop();
        }
    }

    void SnapPlayerOnTop()
    {
        playerOnTop = true;
        playerRb.linearVelocity = Vector3.zero;
        player.position = transform.position + Vector3.up * snapHeight;
        player.SetParent(transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;

        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                player = collision.transform;
                playerRb = collision.gameObject.GetComponent<Rigidbody>();
                SnapPlayerOnTop();
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;
        if (playerOnTop)
        {
            playerOnTop = false;
            player.SetParent(null);
            player = null;
            playerRb = null;
        }
    }
}