using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that tracks the current checkpoint respawn position and
/// teleports the player back when they fall BELOW the last checkpoint's
/// Y position (minus a small buffer).
///
/// Setup:
///   1. Add this component to a persistent GameObject in your scene (e.g. "GameManager").
///   2. Assign the Player transform.
///   3. (Optional) Set a Default Spawn Point for the level start.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CheckpointManager Instance { get; private set; }

    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Player Reference")]
    [Tooltip("Drag the Player GameObject here.")]
    [SerializeField] Transform player;

    [Header("Fall Detection")]
    [Tooltip("How many units BELOW the last checkpoint the player must fall before respawning. " +
             "A small buffer (2–4) prevents false triggers when the player is just standing " +
             "slightly below the checkpoint object's centre.")]
    [SerializeField] float fallBuffer = 3f;

    [Header("Default Spawn")]
    [Tooltip("Where the player starts (before hitting any checkpoint). " +
             "Leave empty to use the player's initial position at scene start.")]
    [SerializeField] Transform defaultSpawnPoint;

    [Header("UI")]
    [Tooltip("Assign the CheckpointNotification component on your Canvas.")]
    [SerializeField] CheckpointNotification notification;

    [Header("Play Again")]
    [Tooltip("The cat's Renderer. The Victory Material is applied to this when the " +
             "player restarts after winning.")]
    [SerializeField] Renderer catRenderer;

    [Tooltip("Material applied to the cat after winning and choosing Play Again. " +
             "Leave empty to keep the cat's normal look on replay.")]
    [SerializeField] Material victoryMaterial;

    // ── Private state ─────────────────────────────────────────────────────────
    Vector3    currentRespawnPos;  // position to teleport back to
    Quaternion currentRespawnRot;  // facing direction when checkpoint was first reached
    float      checkpointY;        // Y of the last activated checkpoint — fall threshold
    Vector3    initialSpawnPos;    // level start — where Play Again returns the player
    Quaternion initialSpawnRot;
    Rigidbody playerRb;
    bool isRespawning;           // one-frame guard against re-triggering

    // ── Unity lifecycle ───────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("[CheckpointManager] No Player assigned! Assign it in the Inspector.");
            return;
        }

        playerRb = player.GetComponent<Rigidbody>();

        // Initialise respawn position (level start)
        currentRespawnPos = defaultSpawnPoint != null
            ? defaultSpawnPoint.position
            : player.position;
        currentRespawnRot = defaultSpawnPoint != null
            ? defaultSpawnPoint.rotation
            : player.rotation;

        checkpointY = currentRespawnPos.y;

        // Remember the level start so Play Again can return here later.
        initialSpawnPos = currentRespawnPos;
        initialSpawnRot = currentRespawnRot;
    }

    void Update()
    {
        if (!GameOptions.GameStarted) return;
        if (player == null || isRespawning) return;

        if (CheckpointsEnabled && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Respawn();

        // Respawn when the player falls below (last checkpoint Y - buffer)
        if (player.position.y < checkpointY - fallBuffer)
            Respawn();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// The player transform this manager controls. Exposed read-only so debug
    /// tooling can read the player's current position without owning the reference.
    /// </summary>
    public Transform Player => player;

    public bool CheckpointsEnabled => GameOptions.CheckpointsEnabled;

    /// <summary>
    /// Called by CheckpointTrigger when the player activates a checkpoint.
    /// Only saves the checkpoint if it is HIGHER than the current one —
    /// falling back onto a lower platform never downgrades progress.
    /// </summary>
    public void SetCheckpoint(Vector3 respawnPosition, Quaternion respawnRotation)
    {
        if (!CheckpointsEnabled) return;

        // Never go backwards — only save higher checkpoints
        if (respawnPosition.y < checkpointY) return;

        currentRespawnPos = respawnPosition;
        currentRespawnRot = respawnRotation;
        checkpointY       = respawnPosition.y;
        notification?.Show();
        Debug.Log($"[CheckpointManager] Checkpoint saved at {respawnPosition} " +
                  $"(respawn if Y < {checkpointY - fallBuffer:F1})");
    }

    /// <summary>
    /// Updates the saved facing rotation for the current checkpoint (called once the player lands).
    /// </summary>
    public void UpdateCheckpointRotation(Quaternion rotation)
    {
        currentRespawnRot = rotation;
    }

    /// <summary>
    /// Teleport the player to the current respawn position and reset movement state.
    /// </summary>
    public void Respawn()
    {
        TeleportTo(currentRespawnPos);
        Debug.Log($"[CheckpointManager] Respawned at {currentRespawnPos}");
    }

    /// <summary>
    /// Teleport the player to an arbitrary position and reset movement state.
    /// This is the single source of truth for moving the player — Respawn(),
    /// JumpToCheckpoint() and any debug tooling all route through here.
    /// Does NOT change the saved respawn point.
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (player == null) return;

        isRespawning = true;

        // Stop all physics motion
        if (playerRb != null)
        {
            playerRb.linearVelocity  = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // Teleport
        player.position = position;
        player.rotation = currentRespawnRot;

        // Reset the movement state machine so the player can aim immediately
        var pm = player.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.ResetState();

        isRespawning = false;
    }

    /// <summary>
    /// Debug helper: teleport the player to a checkpoint AND make it the active
    /// respawn point, so a subsequent fall returns there. Unlike SetCheckpoint(),
    /// this intentionally allows jumping to a LOWER checkpoint — the no-downgrade
    /// rule is for normal play, not debugging.
    /// </summary>
    public void JumpToCheckpoint(Vector3 spawnPos)
    {
        currentRespawnPos = spawnPos;
        checkpointY       = spawnPos.y;
        TeleportTo(spawnPos);
        Debug.Log($"[CheckpointManager] Debug jump to {spawnPos} " +
                  $"(now the active respawn).");
    }

    /// <summary>
    /// Restart the run in place after the player wins. Returns the player to the
    /// level start, resets the respawn point and fall threshold, re-arms every
    /// checkpoint and win zone so a replay works, and applies the victory material
    /// to the cat. Called by WinScreen.PlayAgain().
    /// </summary>
    public void RestartGame()
    {
        // Reset progress back to the level start.
        currentRespawnPos = initialSpawnPos;
        currentRespawnRot = initialSpawnRot;
        checkpointY       = initialSpawnPos.y;
        TeleportTo(initialSpawnPos);

        // Re-arm checkpoints and the win zone — their activated/won flags would
        // otherwise block them from firing again on the replay.
        foreach (var cp in FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None))
            cp.ResetCheckpoint();
        foreach (var wz in FindObjectsByType<WinTrigger>(FindObjectsSortMode.None))
            wz.ResetWin();

        // Give the cat its victory look for the next run.
        if (catRenderer != null && victoryMaterial != null)
            catRenderer.material = victoryMaterial;

        Debug.Log("[CheckpointManager] Game restarted at level start.");
    }
}
