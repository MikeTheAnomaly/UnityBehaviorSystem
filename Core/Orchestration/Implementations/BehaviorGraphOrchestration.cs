using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Implementation: Executes behavior graphs via IBehaviorOrchestration.
/// 
/// Supports two modes:
/// 1. Lazy building from subscribed actions + edges (LSP compliant)
/// 2. Direct initialization with BehaviorGraph (backwards compatible)
/// 
/// Lazy Building Mode:
/// - Actions subscribe via BaseUnityAction (standard pattern)
/// - Edges defined via AddEdge&lt;TFrom, TTo&gt;(condition, priority)
/// - Entry node set via SetEntryNode&lt;T&gt;()
/// - Graph auto-builds on first ExecuteActions() call
/// - Rebuilds only when actions/edges change (dirty flag)
/// 
/// Direct Mode:
/// - Call Initialize(BehaviorGraph) with pre-built graph
/// - Ignores subscriptions and edge definitions
/// 
/// Usage (Lazy):
/// 1. Actions auto-subscribe via BaseUnityAction
/// 2. Provider adds edges: AddEdge&lt;MoveAction, AttackAction&gt;(condition)
/// 3. Provider sets entry: SetEntryNode&lt;MoveAction&gt;()
/// 4. BehaviorLoop executes, graph builds automatically
/// </summary>
public class BehaviorGraphOrchestration : MonoBehaviour, IBehaviorOrchestration
{
    private BehaviorGraphExecutor _executor;
    private Coroutine _executionCoroutine;
    
    // Lazy building state
    private readonly Dictionary<Type, IAction> _subscribedActions = new Dictionary<Type, IAction>();
    private readonly List<EdgeDefinition> _edgeDefinitions = new List<EdgeDefinition>();
    private Type _entryNodeType;
    private bool _isDirty = true;
    private bool _isExecuting = false;

    /// <summary>
    /// Edge definition for lazy graph building.
    /// </summary>
    private class EdgeDefinition
    {
        public Type FromType { get; }
        public Type ToType { get; }
        public ICondition Condition { get; }
        public int Priority { get; }

        public EdgeDefinition(Type fromType, Type toType, ICondition condition, int priority)
        {
            FromType = fromType;
            ToType = toType;
            Condition = condition;
            Priority = priority;
        }
    }

    /// <summary>
    /// Initializes the orchestration with a pre-built graph.
    /// Switches to direct mode - subscriptions and edge definitions are ignored.
    /// </summary>
    public void Initialize(BehaviorGraph graph)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));

        _executor = new BehaviorGraphExecutor(graph);
        _isDirty = false;
    }

    /// <summary>
    /// Sets the entry node type for lazy graph building.
    /// </summary>
    public void SetEntryNode<TAction>() where TAction : IAction
    {
        _entryNodeType = typeof(TAction);
        _isDirty = true;
    }

    /// <summary>
    /// Adds an edge definition for lazy graph building.
    /// </summary>
    public void AddEdge<TFrom, TTo>(ICondition condition, int priority = 0) 
        where TFrom : IAction 
        where TTo : IAction
    {
        if (condition == null)
            throw new ArgumentNullException(nameof(condition));

        _edgeDefinitions.Add(new EdgeDefinition(typeof(TFrom), typeof(TTo), condition, priority));
        _isDirty = true;
    }

    /// <summary>
    /// Subscribes an action. In lazy mode, stores action by type for graph building.
    /// In direct mode, subscription is ignored.
    /// </summary>
    public void SubscribeAction(IAction action)
    {
        if (action == null)
            return;


        var actionType = action.GetType();
        if (!_subscribedActions.ContainsKey(actionType))
        {
            _subscribedActions[actionType] = action;
            _isDirty = true;

            // Auto-set entry node if this is the first action
            if (_entryNodeType == null)
            {
                _entryNodeType = actionType;
            }
        }
    }

    /// <summary>
    /// Unsubscribes an action. In lazy mode, removes action and marks dirty.
    /// In direct mode, unsubscription is ignored.
    /// </summary>
    public void UnsubscribeAction(IAction action)
    {
        if (action == null)
            return;

        var actionType = action.GetType();
        if (_subscribedActions.Remove(actionType))
        {
            _isDirty = true;

            // Clear entry if it was removed
            if (_entryNodeType == actionType)
            {
                _entryNodeType = _subscribedActions.Keys.FirstOrDefault();
            }
        }
    }

    /// <summary>
    /// Executes the behavior graph continuously.
    /// Each iteration executes the current node and checks for transitions.
    /// In lazy mode, builds graph if dirty before execution.
    /// </summary>
    public IEnumerator ExecuteActions(GameObject context)
    {
        _isExecuting = true;

        // Lazy build graph if needed
        if (_isDirty)
        {
            BehaviorSystemLogger.Log("Building graph from subscriptions...");
            BuildGraph();
        }

        if (_executor == null)
        {
            BehaviorSystemLogger.LogError("BehaviorGraphOrchestration has no graph. " +
                          "Either call Initialize(graph) or subscribe actions and add edges.");
            yield break;
        }

        // Continuously execute iterations every frame
        //TODO: not huge fan of this loop
        while (_isExecuting)
        {
            BehaviorSystemLogger.Log($"Executing iteration on {GetCurrentNodeId()}");
            yield return _executor.ExecuteIteration(context);
        }

        _isExecuting = false;
    }

    /// <summary>
    /// Executes a single iteration of the graph (for testing purposes).
    /// Builds the graph if needed before execution.
    /// </summary>
    public IEnumerator ExecuteIteration(GameObject context)
    {
        // Lazy build graph if needed
        if (_isDirty)
        {
            BuildGraph();
        }

        if (_executor == null)
        {
            BehaviorSystemLogger.LogError("BehaviorGraphOrchestration has no graph. " +
                          "Either call Initialize(graph) or subscribe actions and add edges.");
            yield break;
        }

        // Drive the executor's iteration to completion
        var enumerator = _executor.ExecuteIteration(context);
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    /// <summary>
    /// Builds the graph from subscribed actions and edge definitions.
    /// </summary>
    private void BuildGraph()
    {
        BehaviorSystemLogger.Log($"BuildGraph called - {_subscribedActions.Count} actions, {_edgeDefinitions.Count} edges");
        
        if (_subscribedActions.Count == 0)
        {
            BehaviorSystemLogger.LogWarning("No actions subscribed to BehaviorGraphOrchestration. Cannot build graph.");
            return;
        }

        if (_entryNodeType == null)
        {
            BehaviorSystemLogger.LogError("Entry node type not set. Call SetEntryNode<T>() or let first subscribed action be entry.");
            return;
        }

        if (!_subscribedActions.ContainsKey(_entryNodeType))
        {
            BehaviorSystemLogger.LogError($"Entry node type {_entryNodeType.Name} not found in subscribed actions.");
            return;
        }

        BehaviorSystemLogger.Log($"Entry node: {_entryNodeType.Name}");
        
        // Build nodes
        var nodes = new List<BehaviorNode>();
        foreach (var kvp in _subscribedActions)
        {
            var nodeId = NodeId.FromType(kvp.Key);
            var node = new BehaviorNode(nodeId, kvp.Value);
            nodes.Add(node);
            BehaviorSystemLogger.Log($"Added node: {kvp.Key.Name}");
        }

        // Build edges
        var edges = new List<BehaviorEdge>();
        foreach (var edgeDef in _edgeDefinitions)
        {
            // Validate both action types are subscribed
            if (!_subscribedActions.ContainsKey(edgeDef.FromType))
            {
                BehaviorSystemLogger.LogWarning($"Edge from {edgeDef.FromType.Name} ignored - action not subscribed.");
                continue;
            }
            if (!_subscribedActions.ContainsKey(edgeDef.ToType))
            {
                BehaviorSystemLogger.LogWarning($"Edge to {edgeDef.ToType.Name} ignored - action not subscribed.");
                continue;
            }

            var fromId = NodeId.FromType(edgeDef.FromType);
            var toId = NodeId.FromType(edgeDef.ToType);
            var edge = new BehaviorEdge(fromId, toId, edgeDef.Condition, edgeDef.Priority);
            edges.Add(edge);
            BehaviorSystemLogger.Log($"Added edge: {edgeDef.FromType.Name} -> {edgeDef.ToType.Name}");
        }

        // Create graph
        var entryId = NodeId.FromType(_entryNodeType);
        var graph = new BehaviorGraph(entryId, nodes, edges);
        _executor = new BehaviorGraphExecutor(graph);
        _isDirty = false;
        
        BehaviorSystemLogger.Log($"Graph built successfully with {nodes.Count} nodes and {edges.Count} edges");
    }

    /// <summary>
    /// Gets the current node being executed.
    /// Useful for debugging and visualization.
    /// </summary>
    public BehaviorNode GetCurrentNode() => _executor?.GetCurrentNode();

    /// <summary>
    /// Gets the current node ID.
    /// </summary>
    public NodeId GetCurrentNodeId() => _executor?.GetCurrentNodeId() ?? default;

    /// <summary>
    /// Resets graph execution to the entry node.
    /// </summary>
    public void ResetGraph() => _executor?.Reset();

    /// <summary>
    /// Forces a graph rebuild on next execution.
    /// Useful when you want to ensure the graph is fresh.
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Cleanup when the component is destroyed.
    /// Stops any running execution coroutine to prevent hanging enumerators.
    /// </summary>
    private void OnDestroy()
    {
        if (_executionCoroutine != null)
        {
            StopCoroutine(_executionCoroutine);
            _executionCoroutine = null;
        }
        
        _executor = null;
        _subscribedActions.Clear();
        _edgeDefinitions.Clear();
        _isExecuting = false;
    }


}
