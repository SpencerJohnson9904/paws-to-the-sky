using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click demo: wires up a LevelBlockGenerator, generates a climb above the
/// current level summit, and tints the blocks into Grass → Desert → Snow → Volcanic
/// bands so the generator AND the recolored biome materials are visible at once.
///
/// Run it from the menu:  Tools ▸ Paws ▸ Build Demo Climb
/// Remove it any time with: Tools ▸ Paws ▸ Clear Demo Climb
/// </summary>
public static class DemoClimbBuilder
{
    const string Root = "LevelBlocks";

    static readonly string[] BlockPrefabs =
    {
        "Assets/Grass/Prefabs/Complex/Base_10.prefab",
        "Assets/Grass/Prefabs/Complex/Base_3.prefab",
        "Assets/Grass/Prefabs/Complex/Base_9.prefab",
    };

    // Biome bands applied bottom→top. Each is (body material, top material).
    static readonly (string body, string top)[] Biomes =
    {
        ("Assets/Grass/Materials/Dirt.mat",                  "Assets/Grass/Materials/Grass.mat"),         // grassland
        ("Assets/Grass/Materials/Biomes/Desert_Body.mat",    "Assets/Grass/Materials/Biomes/Desert_Top.mat"),
        ("Assets/Grass/Materials/Biomes/Snow_Body.mat",      "Assets/Grass/Materials/Biomes/Snow_Top.mat"),
        ("Assets/Grass/Materials/Biomes/Volcanic_Body.mat",  "Assets/Grass/Materials/Biomes/Volcanic_Top.mat"),
    };

    /// <summary>
    /// Batch-mode entry point (run via -executeMethod): opens the level scene,
    /// builds the demo climb, and saves. Use this when the editor GUI is closed.
    /// </summary>
    public static void BuildAndSave()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        Build();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DemoClimbBuilder] Saved scene with demo climb.");
    }

    [MenuItem("Tools/Paws/Build Demo Climb")]
    public static void Build()
    {
        // Fresh start
        Clear();

        var go = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(go, "Build Demo Climb");
        var gen = go.AddComponent<LevelBlockGenerator>();

        var prefabs = BlockPrefabs
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(p => p != null).ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogError("[DemoClimbBuilder] No block prefabs found — check the Grass pack paths.");
            return;
        }

        // Configure the generator's private serialized fields the proper way.
        var so = new SerializedObject(gen);
        var arr = so.FindProperty("blockPrefabs");
        arr.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        so.FindProperty("blockCount").intValue = 16;
        so.FindProperty("verticalStep").floatValue = 2.5f;
        so.FindProperty("horizontalStep").floatValue = 3.5f;
        so.FindProperty("checkpointEvery").intValue = 4;
        so.ApplyModifiedPropertiesWithoutUndo();

        gen.Generate();
        TintBiomeBands(go);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("[DemoClimbBuilder] Built demo climb with biome bands. Press Play and jump (Space).");
    }

    [MenuItem("Tools/Paws/Clear Demo Climb")]
    public static void Clear()
    {
        var existing = GameObject.Find(Root);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            EditorSceneManager.MarkSceneDirty(existing.scene);
        }
    }

    /// <summary>Recolor the generated blocks into vertical biome bands.</summary>
    static void TintBiomeBands(GameObject root)
    {
        var blocks = root.GetComponentsInChildren<Transform>()
            .Where(t => t.name.StartsWith("Block_"))
            .OrderBy(t => t.position.y)
            .ToList();
        if (blocks.Count == 0) return;

        int perBand = Mathf.CeilToInt(blocks.Count / (float)Biomes.Length);
        for (int i = 0; i < blocks.Count; i++)
        {
            var (bodyPath, topPath) = Biomes[Mathf.Min(i / perBand, Biomes.Length - 1)];
            var body = AssetDatabase.LoadAssetAtPath<Material>(bodyPath);
            var top  = AssetDatabase.LoadAssetAtPath<Material>(topPath);

            foreach (var r in blocks[i].GetComponentsInChildren<Renderer>())
            {
                // Preserve the base/top split: grass-coloured slots become the biome top,
                // everything else becomes the biome body.
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    bool isTop = mats[m] != null && mats[m].name.Contains("Grass");
                    mats[m] = isTop ? (top ?? mats[m]) : (body ?? mats[m]);
                }
                r.sharedMaterials = mats;
            }
        }
    }
}
