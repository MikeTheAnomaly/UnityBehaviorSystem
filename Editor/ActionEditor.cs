using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BaseUnityAction), true)]
public class ActionEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (targets.Length != 1)
		{
			return;
		}

		var action = target as BaseUnityAction;
		if (action == null)
		{
			return;
		}

		var gameObject = action.gameObject;
		if (gameObject.GetComponent<UnityActionOrchestrationProvider>() != null)
		{
			return;
		}

		EditorGUILayout.Space();
		if (GUILayout.Button("Add UnityActionOrchestrationProvider"))
		{
			var provider = Undo.AddComponent<UnityActionOrchestrationProvider>(gameObject);
			EditorUtility.SetDirty(gameObject);
			Selection.activeObject = provider;
			EditorGUIUtility.PingObject(provider);
		}
	}
}
