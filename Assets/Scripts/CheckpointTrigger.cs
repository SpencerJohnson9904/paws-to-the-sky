using UnityEngine;

/// <summary>
/// Attach this to ANY 3D object in the scene to make it a checkpoint.
///
/// How it works:
///   • A SphereCollider trigger is added automatically at runtime — it never
///     interferes with the object's existing physics colliders.
///   • When the player (tagged "Player") walks into the trigger zone, the
///     object's position (or an optional linked respawnPoint) is saved as
///     the current respawn location.
///   • Optional visual feedback: assign Renderer(s) in the Inspector to see
///     the checkpoint change colour when activated.
///
/// Setup:
///   1. Select any platform / object in the scene.
///   2. Add Component → CheckpointTrigger.
///   3. (Optional) Set Respawn Point to an empty child object positioned
///      exactly where you want the player to land after falling.
///      If left empty the checkpoint saves this object's own position.
///   4. (Optional) Assign Indicator Renderers for colour feedback.
/// </summary>
public class CheckpointTrigger : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("Respawn Location")]
    [Tooltip("Where the player will appear after falling. " +
             "Leave empty to use this object's position (plus Spawn Height Offset).")]
    [SerializeField] Transform respawnPoint;

    [Tooltip("Added to the Y of this object's position when no Respawn Point is set. " +
             "Lifts the player above the surface so they don't clip through on respawn.")]
    [SerializeField] float spawnHeightOffset = 1.5f;

    [Tooltip("Player facing rotation after respawning at this checkpoint. Set manually per checkpoint.")]
    [SerializeField] Vector3 respawnEulerRotation;

    [Header("Trigger Zone")]
    [Tooltip("Radius of the invisible sphere that detects the player. " +
             "Resize to cover the platform comfortably.")]
    [SerializeField] float triggerRadius = 1f;

    [Header("Visual Feedback")]
    [Tooltip("Renderers whose material colour changes when the checkpoint activates.")]
    [SerializeField] Renderer[] indicatorRenderers;

    [SerializeField] Color inactiveColor = new Color(0.6f, 0.6f, 0.6f); // grey
    [SerializeField] Color activeColor   = new Color(0.2f, 0.9f, 0.2f); // green

    // ── Private state ─────────────────────────────────────────────────────────
    bool activated;
    SphereCollider triggerCollider;

    // ── Public API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// World position the player respawns at for this checkpoint — the linked
    /// respawnPoint if set, otherwise this object's position lifted by the offset.
    /// Debug tooling reads this to teleport directly to the checkpoint.
    /// </summary>
    public Vector3 SpawnPosition => respawnPoint != null
        ? respawnPoint.position
        : transform.position + Vector3.up * spawnHeightOffset;

    public Quaternion SpawnRotation => Quaternion.Euler(respawnEulerRotation);

    /// <summary>Display name for this checkpoint (the GameObject's name).</summary>
    public string CheckpointName => gameObject.name;

    /// <summary>Whether the player has already activated this checkpoint in play.</summary>
    public bool IsActivated => activated;

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        // Add a dedicated SphereCollider used only as a trigger.
        // This does NOT touch any existing MeshCollider / BoxCollider on the object.
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        // Treat triggerRadius as WORLD units: a SphereCollider's effective radius is
        // scaled by the object's lossyScale, so divide it out. Without this, a checkpoint
        // on a platform parented under the scaled "Grass Level" gets a giant trigger that
        // fires from far away (and banks higher checkpoints prematurely).
        Vector3 ls = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
        triggerCollider.radius = triggerRadius / Mathf.Max(maxScale, 0.0001f);
    }

    void Start()
    {
        RefreshVisuals();
    }

    void OnTriggerEnter(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("[CheckpointTrigger] No CheckpointManager found in scene!");
            return;
        }

        if (!CheckpointManager.Instance.CheckpointsEnabled) return;

        CheckpointManager.Instance.SetCheckpoint(SpawnPosition, SpawnRotation);
        activated = true;
        RefreshVisuals();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void RefreshVisuals()
    {
        if (indicatorRenderers == null || indicatorRenderers.Length == 0) return;

        Color target = activated ? activeColor : inactiveColor;
        foreach (var r in indicatorRenderers)
        {
            if (r == null) continue;
            // Work on a material instance so we don't change the shared asset
            r.material.color = target;
        }
    }

    /// <summary>
    /// Call this to deactivate the checkpoint (e.g., if you add a reset mechanic).
    /// </summary>
    public void ResetCheckpoint()
    {
        activated = false;
        RefreshVisuals();
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Show the trigger sphere in the editor
        Gizmos.color = activated ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        // Show where the player will spawn
        Vector3 spawnPos = respawnPoint != null
            ? respawnPoint.position
            : transform.position + Vector3.up * spawnHeightOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(spawnPos, 0.25f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
#endif
}
