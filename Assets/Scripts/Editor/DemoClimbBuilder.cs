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

    // The exact platform prefabs the existing spiral already uses (Complex bases, scale 1).
    static readonly string[] BlockPrefabs =
    {
        "Assets/Grass/Prefabs/Complex/Base.prefab",
        "Assets/Grass/Prefabs/Complex/Base_3.prefab",
        "Assets/Grass/Prefabs/Complex/Base_4.prefab",
        "Assets/Grass/Prefabs/Complex/Base_9.prefab",
        "Assets/Grass/Prefabs/Complex/Base_10.prefab",
    };

    // Checkpoint platform with a button — same prefab/scale as the existing CheckPoint1.
    const string CheckpointPrefab = "Assets/Grass/Prefabs/Props/ButtonBase.prefab";
    const float CheckpointScale = 0.5f;

    // Biome bands applied bottom→top. Each is (name, body material, top material).
    static readonly (string name, string body, string top)[] Biomes =
    {
        ("Grassland", "Assets/Grass/Materials/Dirt.mat",                  "Assets/Grass/Materials/Grass.mat"),
        ("Desert",    "Assets/Grass/Materials/Biomes/Desert_Body.mat",    "Assets/Grass/Materials/Biomes/Desert_Top.mat"),
        ("Snow",      "Assets/Grass/Materials/Biomes/Snow_Body.mat",      "Assets/Grass/Materials/Biomes/Snow_Top.mat"),
        ("Volcanic",  "Assets/Grass/Materials/Biomes/Volcanic_Body.mat",  "Assets/Grass/Materials/Biomes/Volcanic_Top.mat"),
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

        var prefabs = BlockPrefabs
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(p => p != null).ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogError("[DemoClimbBuilder] No block prefabs found — check the Grass pack paths.");
            return;
        }

        // The existing platforms are children of "Grass Level" (scale 2, 0.7, 2). Parent
        // our ring there too and work in that LOCAL space, so the new blocks inherit the
        // exact same scaling/spacing as the hand-placed ones.
        var grass = GameObject.Find("Grass Level");
        Transform space = grass != null ? grass.transform : null;

        var go = new GameObject(Root);
        Undo.RegisterCreatedObjectUndo(go, "Build Demo Climb");
        if (space != null) go.transform.SetParent(space, false);

        // Fit the circle the existing platforms revolve around, in Grass-Level local space.
        var (localCenter, fitRadius, topLocal) = FitSpiralCircle(space, go);

        // Find the existing checkpoint and CONTINUE the spiral from it: its angle around
        // the circle and its height become the starting point for the new platforms.
        const float angleStep = 30f, verticalStep = 0.47f;
        float startAngle = 0f, baseY = topLocal.y;
        var cp = Object.FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None).FirstOrDefault();
        if (cp != null)
        {
            Vector3 cpLocal = space != null ? space.InverseTransformPoint(cp.transform.position) : cp.transform.position;
            startAngle = Mathf.Atan2(cpLocal.z - localCenter.z, cpLocal.x - localCenter.x) * Mathf.Rad2Deg + angleStep;
            baseY = cpLocal.y;
        }

        // Place the generator object AT the ring centre, at the checkpoint's height, so the
        // first new platform sits one step beyond the checkpoint along the same spiral.
        go.transform.localPosition = new Vector3(localCenter.x, baseY, localCenter.z);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Debug.Log($"[DemoClimbBuilder] center ({localCenter.x:F2},{localCenter.z:F2}) radius {fitRadius:F2}, " +
                  $"continuing spiral from {startAngle - angleStep:F0}° at Y {baseY:F2}.");

        var gen = go.AddComponent<LevelBlockGenerator>();

        // Configure the generator's private serialized fields the proper way.
        var so = new SerializedObject(gen);
        var arr = so.FindProperty("blockPrefabs");
        arr.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        // Reuse the existing spiral's exact function: same radius, ~30°/0.47m per step,
        // existing prefabs at scale 1, ButtonBase checkpoints at 0.5 — nothing invented.
        so.FindProperty("blockCount").intValue = 14;
        so.FindProperty("circleRadius").floatValue = fitRadius;
        so.FindProperty("angleStep").floatValue = angleStep;
        so.FindProperty("startAngle").floatValue = startAngle;
        so.FindProperty("verticalStep").floatValue = verticalStep;
        so.FindProperty("blockScale").floatValue = 1f;   // existing platforms are scale 1
        so.FindProperty("checkpointEvery").intValue = 4;
        // Checkpoints on platforms #4, #7, #11 (first one moved off #3 onto #4).
        int[] cpIdx = { 4, 7, 11 };
        var ci = so.FindProperty("checkpointIndices");
        ci.arraySize = cpIdx.Length;
        for (int k = 0; k < cpIdx.Length; k++) ci.GetArrayElementAtIndex(k).intValue = cpIdx[k];
        so.FindProperty("checkpointScale").floatValue = CheckpointScale;
        var cpProp = so.FindProperty("checkpointPrefab");
        if (cpProp != null) cpProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefab);
        so.ApplyModifiedPropertiesWithoutUndo();

        gen.Generate();
        // Base segment stays grass-coloured; biome segments are added separately.

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        SceneView.FrameLastActiveSceneView();
        Debug.Log("[DemoClimbBuilder] Built circular climb anchored on the summit. Press Play and jump (Space).");
    }

    // ── Biome segments ────────────────────────────────────────────────────────
    // Each call continues the spiral from the current TOP platform, in a different
    // biome, with the topmost new platform as a checkpoint. Stack them: grass → desert → …

    [MenuItem("Tools/Paws/Add Biome Segment ▸ Desert")]   public static void AddDesert()   => BuildBiomeSegment(1);
    [MenuItem("Tools/Paws/Add Biome Segment ▸ Snow")]     public static void AddSnow()     => BuildBiomeSegment(2);
    [MenuItem("Tools/Paws/Add Biome Segment ▸ Volcanic")] public static void AddVolcanic() => BuildBiomeSegment(3);

    /// <summary>Batch entry: open scene, add a Desert segment on top, save.</summary>
    public static void BuildDesertSegmentAndSave()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        BuildBiomeSegment(1);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DemoClimbBuilder] Saved scene with biome segment.");
    }

    static void BuildBiomeSegment(int biomeIdx)
    {
        var (biomeName, bodyPath, topPath) = Biomes[Mathf.Clamp(biomeIdx, 0, Biomes.Length - 1)];

        var prefabs = BlockPrefabs.Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(p => p != null).ToArray();
        if (prefabs.Length == 0) { Debug.LogError("[DemoClimbBuilder] No block prefabs found."); return; }

        var grass = GameObject.Find("Grass Level");
        Transform space = grass != null ? grass.transform : null;

        // Fresh root per biome so segments can be rebuilt independently.
        string rootName = Root + "_" + biomeName;
        var prev = GameObject.Find(rootName);
        if (prev != null) Undo.DestroyObjectImmediate(prev);

        var go = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(go, "Add Biome Segment");
        if (space != null) go.transform.SetParent(space, false);

        // Continue from the CURRENT top platform of the whole climb.
        var (localCenter, fitRadius, topLocal) = FitSpiralCircle(space, go);
        const float angleStep = 30f, verticalStep = 0.47f;
        float startAngle = Mathf.Atan2(topLocal.z - localCenter.z, topLocal.x - localCenter.x) * Mathf.Rad2Deg + angleStep;

        go.transform.localPosition = new Vector3(localCenter.x, topLocal.y, localCenter.z);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var gen = go.AddComponent<LevelBlockGenerator>();
        var so = new SerializedObject(gen);
        var arr = so.FindProperty("blockPrefabs");
        arr.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];

        const int count = 14;
        so.FindProperty("blockCount").intValue = count;
        so.FindProperty("circleRadius").floatValue = fitRadius;
        so.FindProperty("angleStep").floatValue = angleStep;
        so.FindProperty("startAngle").floatValue = startAngle;
        so.FindProperty("verticalStep").floatValue = verticalStep;
        so.FindProperty("blockScale").floatValue = 1f;
        so.FindProperty("checkpointScale").floatValue = CheckpointScale;
        var cpProp = so.FindProperty("checkpointPrefab");
        if (cpProp != null) cpProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefab);
        // The TOP-MOST platform of this segment is the checkpoint.
        var ci = so.FindProperty("checkpointIndices");
        ci.arraySize = 1;
        ci.GetArrayElementAtIndex(0).intValue = count - 1;
        so.ApplyModifiedPropertiesWithoutUndo();

        gen.Generate();
        TintSegment(go, bodyPath, topPath);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"[DemoClimbBuilder] Added {biomeName} segment ({count} platforms) continuing from " +
                  $"{startAngle - angleStep:F0}° at Y {topLocal.y:F2}; top platform is a checkpoint.");
    }

    /// <summary>Recolor a whole segment's platforms into one biome (button left alone).</summary>
    static void TintSegment(GameObject root, string bodyPath, string topPath)
    {
        var body = AssetDatabase.LoadAssetAtPath<Material>(bodyPath);
        var top  = AssetDatabase.LoadAssetAtPath<Material>(topPath);

        foreach (Transform block in root.transform)
        {
            foreach (var r in block.GetComponentsInChildren<Renderer>())
            {
                // Skip the checkpoint button subtree — only tint the platform itself.
                bool underButton = false;
                for (var p = r.transform; p != null && p != block; p = p.parent)
                    if (p.name == "Button") { underButton = true; break; }
                if (underButton) continue;

                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    if (mats[m].name.Contains("Grass")) mats[m] = top ?? mats[m];
                    else if (mats[m].name.Contains("Dirt")) mats[m] = body ?? mats[m];
                }
                r.sharedMaterials = mats;
            }
        }
    }

    /// <summary>
    /// Unpacks every generated block so they become plain GameObjects you can freely
    /// select, move, and delete (prefab-instance children can't be deleted individually).
    /// </summary>
    [MenuItem("Tools/Paws/Unpack Blocks (make editable)")]
    public static void UnpackBlocks()
    {
        var root = GameObject.Find(Root);
        if (root == null) { Debug.LogWarning("[DemoClimbBuilder] No LevelBlocks found."); return; }

        int n = 0;
        // Snapshot children first — unpacking mutates the hierarchy.
        foreach (Transform child in root.transform.Cast<Transform>().ToArray())
        {
            var go = child.gameObject;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                n++;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"[DemoClimbBuilder] Unpacked {n} blocks — they're now freely deletable.");
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

    /// <summary>
    /// Finds the circle the existing platforms revolve around — a least-squares
    /// (Kåsa) fit over the platform instances' X/Z, ignoring trees/rocks/cars and
    /// anything under our own root. Returns the fitted centre and radius.
    /// </summary>
    static (Vector3 center, float radius, Vector3 topLocal) FitSpiralCircle(Transform space, GameObject ourRoot)
    {
        var pts = new List<Vector2>();
        Vector3 topLocal = Vector3.zero;
        float topY = float.NegativeInfinity;
        foreach (var root in ourRoot.scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                var inst = t.gameObject;
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(inst)) continue;
                if (inst.transform.IsChildOf(ourRoot.transform)) continue;

                var src = PrefabUtility.GetCorrespondingObjectFromSource(inst);
                if (src == null) continue;
                string path = AssetDatabase.GetAssetPath(src);
                if (path.Contains("/Complex/") || path.Contains("/Top/") || path.Contains("ButtonBase"))
                {
                    // Express each platform in the chosen local space so the fit matches
                    // the coordinates the generator will place blocks in.
                    Vector3 local = space != null ? space.InverseTransformPoint(t.position) : t.position;
                    pts.Add(new Vector2(local.x, local.z));
                    if (local.y > topY) { topY = local.y; topLocal = local; }
                }
            }
        }
        var (c, r) = Fit(pts);
        return (c, r, topLocal);
    }

    /// <summary>Kåsa least-squares circle fit. Falls back to centroid if degenerate.</summary>
    static (Vector3 center, float radius) Fit(List<Vector2> pts)
    {
        int n = pts.Count;
        Vector2 mean = Vector2.zero;
        foreach (var p in pts) mean += p;
        if (n == 0) return (new Vector3(7.64f, 0f, 2.52f), 4.5f);
        mean /= n;

        double Suu = 0, Svv = 0, Suv = 0, Suuu = 0, Svvv = 0, Suvv = 0, Svuu = 0;
        foreach (var p in pts)
        {
            double u = p.x - mean.x, v = p.y - mean.y;
            Suu += u * u; Svv += v * v; Suv += u * v;
            Suuu += u * u * u; Svvv += v * v * v; Suvv += u * v * v; Svuu += v * u * u;
        }
        double A = Suu, B = Suv, C = Svv;
        double D = 0.5 * (Suuu + Suvv), E = 0.5 * (Svvv + Svuu);
        double det = A * C - B * B;
        if (n < 3 || System.Math.Abs(det) < 1e-6)
            return (new Vector3(mean.x, 0f, mean.y), 4.5f);   // centroid fallback

        double uc = (D * C - B * E) / det, vc = (A * E - B * D) / det;
        float cx = (float)(mean.x + uc), cz = (float)(mean.y + vc);
        float r = (float)System.Math.Sqrt(uc * uc + vc * vc + (Suu + Svv) / n);
        return (new Vector3(cx, 0f, cz), r);
    }

}
