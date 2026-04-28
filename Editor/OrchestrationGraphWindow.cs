using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class OrchestrationGraphWindow : EditorWindow
{
    private const float NodeWidth = 220f;
    private const float NodeHeight = 120f;
    private const float HorizontalSpacing = 280f;
    private const float VerticalSpacing = 180f;

    private Label _orchestrationLabel;
    private EnumField _strategyField;
    private ObjectField _targetField;
    private OrchestrationGraphView _graphView;
    private IMGUIContainer _inspectorContainer;
    private GameObject _targetGameObject;
    private UnityActionOrchestrationProvider _provider;
    private BaseUnityAction _selectedAction;
    private Editor _selectedActionEditor;
    private bool _isRefreshingGraph;

    [MenuItem("Window/Behavior System/Orchestration Graph")]
    public static void OpenWindowFromMenu()
    {
        var window = GetWindow<OrchestrationGraphWindow>("Orchestration Graph");
        window.minSize = new Vector2(800f, 500f);
        window.Show();
        window.TrySetTarget(Selection.activeGameObject);
    }

    public static void OpenFor(GameObject gameObject)
    {
        var window = GetWindow<OrchestrationGraphWindow>("Orchestration Graph");
        window.minSize = new Vector2(800f, 500f);
        window.Show();
        window.TrySetTarget(gameObject);
    }

    private void OnEnable()
    {
        BuildUi();
        TrySetTarget(_targetGameObject != null ? _targetGameObject : Selection.activeGameObject);
    }

    private void OnDisable()
    {
        DestroySelectedActionEditor();

        if (_graphView != null)
        {
            _graphView.RemoveFromHierarchy();
        }
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject == _targetGameObject)
        {
            return;
        }

        TrySetTarget(Selection.activeGameObject);
    }

    private void BuildUi()
    {
        rootVisualElement.Clear();

        var toolbar = new Toolbar();
        _targetField = new ObjectField("Target")
        {
            objectType = typeof(GameObject),
            value = _targetGameObject
        };
        _targetField.RegisterValueChangedCallback(evt => TrySetTarget(evt.newValue as GameObject));

        var refreshButton = new ToolbarButton(() => RefreshGraph()) { text = "Refresh" };
        var addNodeButton = new ToolbarButton(() => ShowAddNodePicker()) { text = "Add Node" };

        toolbar.Add(_targetField);
        toolbar.Add(addNodeButton);
        toolbar.Add(refreshButton);
        rootVisualElement.Add(toolbar);

        _orchestrationLabel = new Label("Current Orchestration: None")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginLeft = 6,
                marginTop = 4,
                marginBottom = 6
            }
        };
        rootVisualElement.Add(_orchestrationLabel);

        _strategyField = new EnumField("Orchestration Type", UnityActionOrchestrationProvider.OrchestrationStrategy.AllAtOnce);
        _strategyField.style.marginLeft = 6;
        _strategyField.style.marginRight = 6;
        _strategyField.style.marginBottom = 6;
        _strategyField.RegisterValueChangedCallback(evt =>
        {
            if (_provider == null)
            {
                return;
            }

            Undo.RecordObject(_provider, "Change Orchestration Strategy");

            var strategyField = typeof(UnityActionOrchestrationProvider).GetField("_strategy", BindingFlags.Instance | BindingFlags.NonPublic);
            strategyField?.SetValue(_provider, evt.newValue);

            EditorUtility.SetDirty(_provider);
            RefreshGraph();
        });
        rootVisualElement.Add(_strategyField);

        var contentRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1
            }
        };

        _graphView = new OrchestrationGraphView();
        _graphView.SetUnsupportedActionCallback(OnUnsupportedGraphAction);
        _graphView.style.flexGrow = 1;
        contentRow.Add(_graphView);

        var inspectorPanel = new VisualElement
        {
            style =
            {
                width = 360,
                minWidth = 320,
                borderLeftWidth = 1,
                borderLeftColor = new Color(0.2f, 0.2f, 0.2f),
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 6,
                paddingBottom = 6
            }
        };

        inspectorPanel.Add(new Label("Selected Action")
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 }
        });

        _inspectorContainer = new IMGUIContainer(DrawSelectedActionInspector)
        {
            style = { flexGrow = 1 }
        };
        inspectorPanel.Add(_inspectorContainer);
        contentRow.Add(inspectorPanel);

        rootVisualElement.Add(contentRow);
    }

    private void TrySetTarget(GameObject gameObject)
    {
        _targetGameObject = gameObject;
        _provider = _targetGameObject == null ? null : _targetGameObject.GetComponent<UnityActionOrchestrationProvider>();
        SetSelectedAction(null);

        if (_targetField != null && _targetField.value != _targetGameObject)
        {
            _targetField.SetValueWithoutNotify(_targetGameObject);
        }

        UpdateStrategyFieldFromProvider();

        RefreshGraph();
    }

    private void RefreshGraph()
    {
        if (_graphView == null)
        {
            return;
        }

        _isRefreshingGraph = true;
        _graphView.BeginPopulate();
        _graphView.ClearGraph();

        if (_targetGameObject == null)
        {
            _orchestrationLabel.text = "Current Orchestration: None";
            _graphView.EndPopulate();
            _isRefreshingGraph = false;
            return;
        }

        var orchestration = GetOrchestration(_targetGameObject);
        _orchestrationLabel.text = $"Current Orchestration: {(orchestration == null ? "None" : orchestration.GetType().Name)}";

        var selectedStrategy = ReadSelectedStrategy();

        var actions = _targetGameObject.GetComponents<BaseUnityAction>().ToList();
        if (actions.Count == 0)
        {
            _graphView.EndPopulate();
            _isRefreshingGraph = false;
            return;
        }

        var nodeByAction = new Dictionary<BaseUnityAction, Node>();
        var outputByAction = new Dictionary<BaseUnityAction, Port>();
        var inputByAction = new Dictionary<BaseUnityAction, Port>();

        CreateActionNodes(actions, nodeByAction, outputByAction, inputByAction);

        if (orchestration is BehaviorGraphOrchestration graphOrchestration)
        {
            RenderBehaviorGraphEdges(graphOrchestration, actions, outputByAction, inputByAction);
        }
        else if (orchestration is PriorityQueueBehaviorOrchestration || selectedStrategy == UnityActionOrchestrationProvider.OrchestrationStrategy.PriorityQueue)
        {
            RenderPriorityQueueEdges(actions, outputByAction, inputByAction);
        }
        else if (orchestration is AllAtOnceBehaviorOrchestration || selectedStrategy == UnityActionOrchestrationProvider.OrchestrationStrategy.AllAtOnce)
        {
            RenderAllAtOnceEdges(actions, outputByAction, inputByAction);
        }

        _graphView.EndPopulate();
        _isRefreshingGraph = false;
    }

    private void OnUnsupportedGraphAction()
    {
        if (_isRefreshingGraph)
        {
            return;
        }

        RefreshGraph();
    }

    private void ShowAddNodePicker()
    {
        if (_targetGameObject == null)
        {
            EditorUtility.DisplayDialog("Add Node", "Select a target GameObject first.", "OK");
            return;
        }

        BaseUnityActionPickerWindow.Show(_targetGameObject, OnActionNodeAdded);
    }

    private void OnActionNodeAdded(BaseUnityAction action)
    {
        RefreshGraph();
        SetSelectedAction(action);
    }

    private void UpdateStrategyFieldFromProvider()
    {
        if (_strategyField == null)
        {
            return;
        }

        if (_provider == null)
        {
            _strategyField.SetEnabled(false);
            _strategyField.SetValueWithoutNotify(UnityActionOrchestrationProvider.OrchestrationStrategy.AllAtOnce);
            return;
        }

        _strategyField.SetEnabled(true);

        var strategyField = typeof(UnityActionOrchestrationProvider).GetField("_strategy", BindingFlags.Instance | BindingFlags.NonPublic);
        var strategy = strategyField?.GetValue(_provider) is UnityActionOrchestrationProvider.OrchestrationStrategy value
            ? value
            : UnityActionOrchestrationProvider.OrchestrationStrategy.AllAtOnce;

        _strategyField.SetValueWithoutNotify(strategy);
    }

    private UnityActionOrchestrationProvider.OrchestrationStrategy ReadSelectedStrategy()
    {
        if (_strategyField != null && _strategyField.value is UnityActionOrchestrationProvider.OrchestrationStrategy selected)
        {
            return selected;
        }

        return UnityActionOrchestrationProvider.OrchestrationStrategy.AllAtOnce;
    }

    private void CreateActionNodes(
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Node> nodeByAction,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            var action = actions[index];
            var node = new Node();

            var countOfSameType = actions.Take(index + 1).Count(a => a.GetType() == action.GetType());
            var suffix = countOfSameType > 1 ? $" #{countOfSameType}" : string.Empty;
            node.title = $"{action.GetType().Name}{suffix}";

            var inputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "In";
            node.inputContainer.Add(inputPort);

            var outputPort = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "Out";
            node.outputContainer.Add(outputPort);

            if (action is IPriority priority)
            {
                node.extensionContainer.Add(new Label($"Priority: {priority.Priority}"));
            }

            node.extensionContainer.Add(new Label(action.name));

            node.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                SetSelectedAction(action);
            });

            var row = index / 4;
            var column = index % 4;
            node.SetPosition(new Rect(column * HorizontalSpacing, row * VerticalSpacing, NodeWidth, NodeHeight));

            node.RefreshExpandedState();
            node.RefreshPorts();

            _graphView.AddElement(node);
            nodeByAction[action] = node;
            outputByAction[action] = outputPort;
            inputByAction[action] = inputPort;
        }
    }

    private void RenderPriorityQueueEdges(
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        var sorted = actions
            .OrderByDescending(a => (a as IPriority)?.Priority ?? 1)
            .ThenBy(a => a.GetType().Name)
            .ToList();

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            Connect(outputByAction[sorted[i]], inputByAction[sorted[i + 1]], "priority order");
        }
    }

    private void RenderAllAtOnceEdges(
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        for (var i = 0; i < actions.Count; i++)
        {
            for (var j = 0; j < actions.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Connect(outputByAction[actions[i]], inputByAction[actions[j]], "parallel");
            }
        }
    }

    private void RenderBehaviorGraphEdges(
        BehaviorGraphOrchestration orchestration,
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        var renderedAny = RenderBehaviorGraphDefinitionEdges(orchestration, actions, outputByAction, inputByAction);
        if (renderedAny)
        {
            return;
        }

        RenderBuiltGraphEdges(orchestration, actions, outputByAction, inputByAction);
    }

    private bool RenderBehaviorGraphDefinitionEdges(
        BehaviorGraphOrchestration orchestration,
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        var field = typeof(BehaviorGraphOrchestration).GetField("_edgeDefinitions", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return false;
        }

        if (!(field.GetValue(orchestration) is IEnumerable edgeDefinitions))
        {
            return false;
        }

        var hasAny = false;
        foreach (var edgeDef in edgeDefinitions)
        {
            var fromType = ReadTypeMember(edgeDef, "FromType");
            var toType = ReadTypeMember(edgeDef, "ToType");
            var priority = ReadIntMember(edgeDef, "Priority");

            if (fromType == null || toType == null)
            {
                continue;
            }

            hasAny = true;
            var fromActions = actions.Where(a => a.GetType() == fromType).ToList();
            var toActions = actions.Where(a => a.GetType() == toType).ToList();

            foreach (var from in fromActions)
            {
                foreach (var to in toActions)
                {
                    Connect(outputByAction[from], inputByAction[to], $"p:{priority}");
                }
            }
        }

        return hasAny;
    }

    private void RenderBuiltGraphEdges(
        BehaviorGraphOrchestration orchestration,
        List<BaseUnityAction> actions,
        Dictionary<BaseUnityAction, Port> outputByAction,
        Dictionary<BaseUnityAction, Port> inputByAction)
    {
        var executorField = typeof(BehaviorGraphOrchestration).GetField("_executor", BindingFlags.Instance | BindingFlags.NonPublic);
        var executor = executorField?.GetValue(orchestration);
        if (executor == null)
        {
            return;
        }

        var graphField = executor.GetType().GetField("_graph", BindingFlags.Instance | BindingFlags.NonPublic);
        if (!(graphField?.GetValue(executor) is BehaviorGraph graph))
        {
            return;
        }

        var byTypeKey = actions.GroupBy(a => a.GetType().FullName)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var edge in graph.GetAllEdges())
        {
            var fromKey = ReadNodeIdKey(edge.From);
            var toKey = ReadNodeIdKey(edge.To);

            if (string.IsNullOrEmpty(fromKey) || string.IsNullOrEmpty(toKey))
            {
                continue;
            }

            if (!byTypeKey.TryGetValue(fromKey, out var fromActions) || !byTypeKey.TryGetValue(toKey, out var toActions))
            {
                continue;
            }

            foreach (var from in fromActions)
            {
                foreach (var to in toActions)
                {
                    Connect(outputByAction[from], inputByAction[to], $"p:{edge.Priority}");
                }
            }
        }
    }

    private static IBehaviorOrchestration GetOrchestration(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        return target.GetComponents<MonoBehaviour>().OfType<IBehaviorOrchestration>().FirstOrDefault();
    }

    private void Connect(Port output, Port input, string label)
    {
        if (output == null || input == null)
        {
            return;
        }

        var edge = output.ConnectTo(input);
        if (!string.IsNullOrEmpty(label))
        {
            edge.viewDataKey = label;
        }
        _graphView.AddElement(edge);
    }

    private void SetSelectedAction(BaseUnityAction action)
    {
        if (_selectedAction == action)
        {
            return;
        }

        _selectedAction = action;
        DestroySelectedActionEditor();

        if (_selectedAction != null)
        {
            _selectedActionEditor = Editor.CreateEditor(_selectedAction);
        }

        _inspectorContainer?.MarkDirtyRepaint();
    }

    private void DrawSelectedActionInspector()
    {
        if (_selectedAction == null)
        {
            EditorGUILayout.HelpBox("Click an action node to edit its serialized fields.", MessageType.Info);
            return;
        }

        if (_selectedActionEditor == null)
        {
            _selectedActionEditor = Editor.CreateEditor(_selectedAction);
        }

        EditorGUILayout.LabelField(_selectedAction.GetType().Name, EditorStyles.boldLabel);
        _selectedActionEditor.OnInspectorGUI();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_selectedAction);
        }
    }

    private void DestroySelectedActionEditor()
    {
        if (_selectedActionEditor == null)
        {
            return;
        }

        DestroyImmediate(_selectedActionEditor);
        _selectedActionEditor = null;
    }

    private static Type ReadTypeMember(object source, string memberName)
    {
        if (source == null)
        {
            return null;
        }

        var property = source.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            return property.GetValue(source) as Type;
        }

        var field = source.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? source.GetType().GetField($"<{memberName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(source) as Type;
    }

    private static int ReadIntMember(object source, string memberName)
    {
        if (source == null)
        {
            return 0;
        }

        var property = source.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.GetValue(source) is int propertyValue)
        {
            return propertyValue;
        }

        var field = source.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? source.GetType().GetField($"<{memberName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(source) as int? ?? 0;
    }

    private static string ReadNodeIdKey(NodeId nodeId)
    {
        var text = nodeId.ToString();
        var start = text.IndexOf('(');
        var end = text.LastIndexOf(')');

        if (start < 0 || end <= start)
        {
            return null;
        }

        return text.Substring(start + 1, end - start - 1);
    }

    private class OrchestrationGraphView : GraphView
    {
        private Action _unsupportedActionCallback;
        private bool _isPopulating;

        public OrchestrationGraphView()
        {
            style.flexGrow = 1;

            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = OnGraphViewChanged;
        }

        public void SetUnsupportedActionCallback(Action callback)
        {
            _unsupportedActionCallback = callback;
        }

        public void BeginPopulate()
        {
            _isPopulating = true;
        }

        public void EndPopulate()
        {
            _isPopulating = false;
        }

        public void ClearGraph()
        {
            DeleteElements(graphElements.ToList());
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isPopulating)
            {
                return change;
            }

            var hasUnsupportedMutation =
                (change.elementsToRemove != null && change.elementsToRemove.Count > 0) ||
                (change.edgesToCreate != null && change.edgesToCreate.Count > 0) ||
                (change.movedElements != null && change.movedElements.Count > 0);

            if (!hasUnsupportedMutation)
            {
                return change;
            }

            change.elementsToRemove?.Clear();
            change.edgesToCreate?.Clear();
            change.movedElements?.Clear();
            _unsupportedActionCallback?.Invoke();
            return change;
        }
    }

}
