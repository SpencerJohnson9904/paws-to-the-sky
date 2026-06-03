using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dev utility: logs the world-space bounds (footprint + height) of every Grass
/// platform prefab so we can pick the flat, wide ones for the generator.
/// Run via -executeMethod PlatformBoundsInspector.LogBounds
/// </summary>
public static class PlatformBoundsInspector
{
    public static void LogBounds()
    {
        string[] folders =
        {
            "Assets/Grass/Prefabs/Base",
            "Assets/Grass/Prefabs/Complex",
            "Assets/Grass/Prefabs/Top",
        };

        foreach (var folder in folders)
        {
            Debug.Log($"==== {folder} ====");
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            foreach (var guid in guids.OrderBy(g => g))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                var rends = go.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    foreach (var r in rends) b.Encapsulate(r.bounds);
                    bool hasCol = go.GetComponentInChildren<Collider>() != null;
                    string flat = (b.size.y <= 0.6f) ? "FLAT" : (b.size.y <= 1.2f ? "low" : "TALL");
                    Debug.Log($"  {System.IO.Path.GetFileNameWithoutExtension(path),-22} " +
                              $"footprint {b.size.x:F2} x {b.size.z:F2}, height {b.size.y:F2}  " +
                              $"[{flat}] collider:{hasCol}");
                }
                Object.DestroyImmediate(go);
            }
        }
    }
}
