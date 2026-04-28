using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class BaseUnityActionPickerWindow : EditorWindow
{
    private const float MinWindowWidth = 420f;
    private const float MinWindowHeight = 320f;

    private GameObject _targetGameObject;
    private Action<BaseUnityAction> _onAdded;
    private string _searchText = string.Empty;
    private Vector2 _scroll;
    private List<Type> _allTypes;

    public static void Show(GameObject targetGameObject, Action<BaseUnityAction> onAdded = null)
    {
        var window = GetWindow<BaseUnityActionPickerWindow>(true, "Select BaseUnityAction", true);
        window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        window._targetGameObject = targetGameObject;
        window._onAdded = onAdded;
        window.RefreshTypes();
        window.ShowUtility();
        window.Focus();
    }

    private void OnGUI()
    {
        if (_targetGameObject == null)
        {
            EditorGUILayout.HelpBox("Target GameObject is missing.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Target: {_targetGameObject.name}", EditorStyles.boldLabel);
        _searchText = EditorGUILayout.TextField("Search", _searchText);

        EditorGUILayout.Space(4f);

        var filtered = FilteredTypes();

        EditorGUILayout.LabelField($"Results: {filtered.Count}", EditorStyles.miniBoldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox("No BaseUnityAction classes found for this search.", MessageType.Info);
        }
        else
        {
            foreach (var actionType in filtered)
            {
                DrawTypeRow(actionType);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshTypes()
    {
        _allTypes = TypeCache.GetTypesDerivedFrom<BaseUnityAction>()
            .Where(t => !t.IsAbstract && !t.IsGenericType && typeof(Component).IsAssignableFrom(t))
            .OrderBy(t => t.FullName)
            .ToList();
    }

    private List<Type> FilteredTypes()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return _allTypes;
        }

        var term = _searchText.Trim();
        return _allTypes
            .Where(t => t.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        t.FullName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    private void DrawTypeRow(Type actionType)
    {
        var addedCount = _targetGameObject.GetComponents(actionType).Length;
        var addedCountStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            richText = true
        };

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(actionType.Name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(actionType.FullName ?? actionType.Name, EditorStyles.wordWrappedMiniLabel);
        if (addedCount > 0)
        {
            EditorGUILayout.LabelField($"<color=#3B82F6>Added: {addedCount}</color>", addedCountStyle);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Add", GUILayout.Width(90f)))
        {
            var component = Undo.AddComponent(_targetGameObject, actionType) as BaseUnityAction;
            EditorUtility.SetDirty(_targetGameObject);
            Selection.activeObject = component;
            EditorGUIUtility.PingObject(component);
            _onAdded?.Invoke(component);
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }
}
