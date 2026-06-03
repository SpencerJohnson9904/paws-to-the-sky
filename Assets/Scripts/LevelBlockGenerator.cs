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

    [Tooltip("Degrees travelled around the circle per block. ~30 matches the existing spiral.")]
    [SerializeField] float angleStep = 70f;

    [Tooltip("Angle (degrees) of the FIRST block around the circle. Set this to continue " +
             "an existing spiral from where it left off.")]
    [SerializeField] float startAngle = 0f;

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
    [Tooltip("Every Nth block becomes a checkpoint (gets a CheckpointTrigger). 0 = none.")]
    [SerializeField] int checkpointEvery = 3;

    [Tooltip("Prefab used for checkpoint blocks — e.g. Grass/Prefabs/Props/ButtonBase " +
             "(the flat platform with a button). Leave empty to reuse a normal block " +
             "with just the trigger added.")]
    [SerializeField] GameObject checkpointPrefab;

    [Tooltip("Scale of the button sitting on a checkpoint platform (existing one is 0.5).")]
    [SerializeField] float checkpointScale = 1f;

    [Tooltip("Local height to lift the button above its platform so it rests on top.")]
    [SerializeField] float checkpointButtonHeight = 0.7f;

    [Header("Coins (optional)")]
    [Tooltip("Optional collectible prefab (e.g. Grass/Prefabs/Props/Coin) floated above " +
             "each block. Leave empty for none.")]
    [SerializeField] GameObject coinPrefab;

    [Tooltip("Height above each block to float the coin.")]
    [SerializeField] float coinHeight = 1.2f;

    // Blocks are placed in this object's LOCAL space, around its own origin. Position
    // (and parent) this GameObject at the centre of the ring you want — if it's a child
    // of a scaled container (like "Grass Level"), the blocks inherit that scale too, so
    // they match the existing platforms exactly.

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

        // All positions are LOCAL to this object: the helix winds around its origin.
        var rng = new System.Random(seed);

        for (int i = 0; i < blockCount; i++)
        {
            float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            float r = circleRadius + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * r,
                verticalStep * (i + 1) + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter * 0.5f,
                Mathf.Sin(angle) * r);

            bool isCheckpoint = checkpointEvery > 0 && (i + 1) % checkpointEvery == 0;

            // Every block — checkpoint or not — is a real, landable platform.
            GameObject prefab = blockPrefabs[rng.Next(blockPrefabs.Length)];
            if (prefab == null) continue;

            Quaternion rot = randomYRotation
                ? Quaternion.Euler(0f, rng.Next(4) * 90f, 0f)
                : Quaternion.identity;

            GameObject block = InstantiateBlock(prefab);
            block.transform.SetParent(transform, false);   // adopt parent's (scaled) space
            block.transform.localPosition = localPos;
            block.transform.localRotation = rot;
            block.transform.localScale = Vector3.one * blockScale;
            block.name = isCheckpoint ? $"Checkpoint_{i:000}" : $"Block_{i:000}";

            if (ensureCollider && block.GetComponentInChildren<Collider>() == null)
            {
                var mf = block.GetComponentInChildren<MeshFilter>();
                if (mf != null) block.AddComponent<MeshCollider>();
                else block.AddComponent<BoxCollider>();
            }

            if (isCheckpoint)
            {
                // Sit the button ON TOP of the platform (like the base-version checkpoint),
                // and put the trigger on the platform the player actually lands on.
                if (block.GetComponent<CheckpointTrigger>() == null)
                    block.AddComponent<CheckpointTrigger>();

                if (checkpointPrefab != null)
                {
                    GameObject button = InstantiateBlock(checkpointPrefab);
                    button.transform.SetParent(block.transform, false);
                    button.transform.localPosition = Vector3.up * checkpointButtonHeight;
                    button.transform.localScale = Vector3.one * checkpointScale;
                    button.name = "Button";
                    spawned.Add(button);
                }
            }

            spawned.Add(block);

            if (coinPrefab != null)
            {
                GameObject coin = InstantiateBlock(coinPrefab);
                coin.transform.SetParent(transform, false);
                coin.transform.localPosition = localPos + Vector3.up * coinHeight;
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

    GameObject InstantiateBlock(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Keep the prefab connection so the blocks stay linked to their source asset.
            var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Generate Block");
            return go;
        }
#endif
        return Instantiate(prefab);
    }

    void DestroyBlock(GameObject go)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
        Destroy(go);
    }
}
