using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally spawns a chain of platform "blocks" climbing upward, so you can
/// extend the level without hand-placing each one.
///
/// HOW TO USE
///   1. Create an empty GameObject (e.g. "LevelBlocks") and add this component.
///      Position it where you want the climb to continue from (or assign Start From).
///   2. Drag one or more platform prefabs into Block Prefabs
///      (recommended: Assets/Grass/Prefabs/Complex/Base_10, Base_3, Base_9 — these
///      are self-contained islands with a grass top).
///   3. Right-click the component header → "Generate Blocks".
///      Right-click → "Clear Blocks" to remove them and try different settings.
///
/// SPACING SAFETY
///   The player jumps with a Rigidbody of mass 1 under gravity -9.81, with a
///   full-charge impulse of ~9 up / ~8 forward. That yields a theoretical max
///   rise of ~4.1m and horizontal reach of ~14m. The defaults below sit well
///   inside that envelope so every gap is clearable on a strong-but-not-perfect
///   charge. Raise verticalStep toward ~3.5 only if you want a tighter challenge.
/// </summary>
[ExecuteAlways]
public class LevelBlockGenerator : MonoBehaviour
{
    [Header("Block Prefab(s)")]
    [Tooltip("Platform prefab(s) to spawn. With more than one, the generator picks " +
             "among them for visual variety. Recommended: Grass/Prefabs/Complex/Base_*.")]
    [SerializeField] GameObject[] blockPrefabs;

    [Header("Layout")]
    [Tooltip("How many blocks to spawn.")]
    [SerializeField] int blockCount = 14;

    [Tooltip("Radius of the circle the blocks wind around (matches the existing level: " +
             "~4). The chain spirals around this circle rather than shooting straight up.")]
    [SerializeField] float circleRadius = 4f;

    [Tooltip("Degrees travelled around the circle per block. ~70 gives ~5 blocks per loop.")]
    [SerializeField] float angleStep = 70f;

    [Tooltip("Vertical rise per block. The existing platforms step up only ~0.5 each, so " +
             "keep this small for a gentle, walkable climb (not a steep tower).")]
    [SerializeField] float verticalStep = 0.6f;

    [Tooltip("Random wobble (radius & height) added per block so the ring feels organic " +
             "rather than perfectly geometric. 0 = perfectly regular.")]
    [SerializeField] float jitter = 0.6f;

    [Tooltip("Give each block a random 90° yaw, like the varied rotations in the existing " +
             "level. Off = all face the same way.")]
    [SerializeField] bool randomYRotation = true;

    [Tooltip("Uniform scale applied to each spawned block.")]
    [SerializeField] float blockScale = 1f;

    [Header("Checkpoints")]
    [Tooltip("Attach a CheckpointTrigger to every Nth block so falls don't reset the " +
             "whole climb. 0 = no checkpoints.")]
    [SerializeField] int checkpointEvery = 3;

    [Header("Coins (optional)")]
    [Tooltip("Optional collectible prefab (e.g. Grass/Prefabs/Props/Coin) floated above " +
             "each block. Leave empty for none.")]
    [SerializeField] GameObject coinPrefab;

    [Tooltip("Height above each block to float the coin.")]
    [SerializeField] float coinHeight = 1.2f;

    [Header("Start")]
    [Tooltip("First block is placed one step beyond this point. Leave empty to start " +
             "from this generator's own position (or the auto-detected summit below).")]
    [SerializeField] Transform startFrom;

    [Tooltip("If true and no Start From is set, the chain begins above the HIGHEST " +
             "collider already in the scene — so it always connects to the current level top.")]
    [SerializeField] bool startFromHighestPlatform = true;

    [Header("Safety")]
    [Tooltip("If a spawned block has no collider, add a MeshCollider so the player can " +
             "actually land on it.")]
    [SerializeField] bool ensureCollider = true;

    [Tooltip("Deterministic seed for the jitter/variety randomness. Change it to get a " +
             "different but repeatable layout.")]
    [SerializeField] int seed = 12345;

    // Tracks what we created so "Clear Blocks" can remove exactly those objects.
    [SerializeField, HideInInspector] List<GameObject> spawned = new List<GameObject>();

    [ContextMenu("Generate Blocks")]
    public void Generate()
    {
        Clear();

        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("[LevelBlockGenerator] Assign at least one prefab to Block Prefabs first.", this);
            return;
        }

        // Centre the circle on the current summit so the new ring winds around the
        // existing climb rather than rocketing straight up from one edge.
        Vector3 center = ResolveStart();
        float startY = center.y;
        var rng = new System.Random(seed);

        for (int i = 0; i < blockCount; i++)
        {
            float angle = (i * angleStep) * Mathf.Deg2Rad;
            float r = circleRadius + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
            Vector3 pos = new Vector3(
                center.x + Mathf.Cos(angle) * r,
                startY + verticalStep * (i + 1) + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter * 0.5f,
                center.z + Mathf.Sin(angle) * r);

            GameObject prefab = blockPrefabs[rng.Next(blockPrefabs.Length)];
            if (prefab == null) continue;

            Quaternion rot = randomYRotation
                ? Quaternion.Euler(0f, rng.Next(4) * 90f, 0f)
                : Quaternion.identity;

            GameObject block = InstantiateBlock(prefab, pos, rot);
            block.transform.SetParent(transform, true);
            block.transform.localScale = Vector3.one * blockScale;
            block.name = $"Block_{i:000}";

            if (ensureCollider && block.GetComponentInChildren<Collider>() == null)
            {
                var mf = block.GetComponentInChildren<MeshFilter>();
                if (mf != null) block.AddComponent<MeshCollider>();
                else block.AddComponent<BoxCollider>();
            }

            if (checkpointEvery > 0 && (i + 1) % checkpointEvery == 0)
                block.AddComponent<CheckpointTrigger>();

            spawned.Add(block);

            if (coinPrefab != null)
            {
                GameObject coin = InstantiateBlock(coinPrefab, pos + Vector3.up * coinHeight, Quaternion.identity);
                coin.transform.SetParent(transform, true);
                coin.name = $"Coin_{i:000}";
                spawned.Add(coin);
            }
        }

        Debug.Log($"[LevelBlockGenerator] Spawned {spawned.Count} blocks rising " +
                  $"{verticalStep * spawned.Count:F1}m.", this);
    }

    [ContextMenu("Clear Blocks")]
    public void Clear()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null) continue;
            DestroyBlock(spawned[i]);
        }
        spawned.Clear();
    }

    /// <summary>
    /// Decides where the chain begins: an explicit Start From wins; otherwise, if
    /// enabled, the top of the highest collider already in the scene (the current
    /// level summit); otherwise this object's own position.
    /// </summary>
    Vector3 ResolveStart()
    {
        if (startFrom != null) return startFrom.position;

        if (startFromHighestPlatform)
        {
            float bestY = float.NegativeInfinity;
            Vector3 best = transform.position;
            foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (col.isTrigger) continue;                      // ignore checkpoint zones
                if (col.GetComponentInParent<PlayerMovement>() != null) continue;  // ignore the cat
                if (IsOurChild(col.transform)) continue;          // ignore blocks we already made
                float topY = col.bounds.max.y;
                if (topY > bestY) { bestY = topY; best = new Vector3(col.bounds.center.x, topY, col.bounds.center.z); }
            }
            if (!float.IsNegativeInfinity(bestY)) return best;
        }

        return transform.position;
    }

    bool IsOurChild(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            if (p == transform) return true;
        return false;
    }

    GameObject InstantiateBlock(GameObject prefab, Vector3 position, Quaternion rotation)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Keep the prefab connection so the blocks stay linked to their source asset.
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(position, rotation);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Generate Block");
            return go;
        }
#endif
        return Instantiate(prefab, position, rotation);
    }

    void DestroyBlock(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
        Destroy(go);
    }
}
