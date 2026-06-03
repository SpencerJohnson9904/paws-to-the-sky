using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds "Generate Blocks" / "Clear Blocks" buttons to the LevelBlockGenerator
/// inspector so you don't have to hunt through the component's context menu.
/// </summary>
[CustomEditor(typeof(LevelBlockGenerator))]
public class LevelBlockGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        var gen = (LevelBlockGenerator)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate Blocks", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Generate Blocks");
                gen.Generate();
            }

            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("Clear Blocks", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Clear Blocks");
                gen.Clear();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
