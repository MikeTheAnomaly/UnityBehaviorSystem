using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityActionOrchestrationProvider), true)]
public class UnityActionOrchestrationProviderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length != 1)
        {
            return;
        }

        var provider = target as UnityActionOrchestrationProvider;
        if (provider == null)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Action Utilities", EditorStyles.boldLabel);

        if (GUILayout.Button("Add BaseUnityAction Component"))
        {
            BaseUnityActionPickerWindow.Show(provider.gameObject);
        }

        if (GUILayout.Button("Open Orchestration Graph Window"))
        {
            OrchestrationGraphWindow.OpenFor(provider.gameObject);
        }
    }
}
