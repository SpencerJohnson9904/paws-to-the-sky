using UnityEngine;

/// <summary>
/// Attach this to the object at the END of the level to make it the win zone.
///
/// How it works:
///   • A SphereCollider trigger is added automatically at runtime — it never
///     interferes with the object's existing physics colliders (same approach
///     as CheckpointTrigger).
///   • When the player (tagged "Player") walks into the trigger zone, the
///     assigned WinScreen is shown and the game freezes until "Play Again?".
///
/// Setup:
///   1. Select the goal object in the scene.
///   2. Add Component → WinTrigger.
///   3. Assign the WinScreen component (on your Canvas) in the Inspector.
///   4. (Optional) Resize Trigger Radius to cover the goal comfortably.
/// </summary>
public class WinTrigger : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("Trigger Zone")]
    [Tooltip("Radius of the invisible sphere that detects the player. " +
             "Resize to cover the goal comfortably.")]
    [SerializeField] float triggerRadius = 1f;

    [Header("UI")]
    [Tooltip("Assign the WinScreen component on your Canvas.")]
    [SerializeField] WinScreen winScreen;

    // ── Private state ─────────────────────────────────────────────────────────
    bool won;
    SphereCollider triggerCollider;

    /// <summary>Whether the player has already crossed the win zone this run.</summary>
    public bool HasWon => won;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        // Add a dedicated SphereCollider used only as a trigger.
        // This does NOT touch any existing MeshCollider / BoxCollider on the object.
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        // Treat triggerRadius as WORLD units: a SphereCollider's effective radius is
        // scaled by the object's lossyScale, so divide it out. Without this, a win zone
        // on an object parented under a scaled level gets a giant trigger that fires
        // from far away. (Same compensation as CheckpointTrigger.)
        Vector3 ls = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        triggerCollider.radius = triggerRadius / Mathf.Max(maxScale, 0.0001f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (won) return;
        if (!other.CompareTag("Player")) return;

        if (winScreen == null)
        {
            Debug.LogWarning("[WinTrigger] No WinScreen assigned in the Inspector!");
            return;
        }

        won = true;
        winScreen.Show();
    }

    /// <summary>
    /// Re-arm the win zone so it can fire again on a replay. Called by
    /// CheckpointManager.RestartGame().
    /// </summary>
    public void ResetWin()
    {
        won = false;
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f); // gold
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
#endif
}
