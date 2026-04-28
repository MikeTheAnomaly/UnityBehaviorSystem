using System;
using System.Collections.Generic;

/// <summary>
/// Fluent API for building behavior graphs with type safety.
/// 
/// Features:
/// - Type-safe node creation via generics
/// - Fluent edge definition with conditions
/// - Automatic node registration
/// - Validation on build
/// 
/// Example usage:
/// <code>
/// var graph = new BehaviorGraphBuilder()
///     .WithEntry&lt;MoveAction&gt;("move", moveAction)
///     .AddNode&lt;AttackAction&gt;("attack", attackAction)
///     .AddEdge("move", "attack", new DistanceToTargetCondition(2f))
///     .AddEdge("attack", "move", new DistanceToTargetCondition(3f))
///     .Build();
/// </code>
/// </summary>
public class BehaviorGraphBuilder
{
    private NodeId _entryNodeId;
    private readonly List<BehaviorNode> _nodes = new List<BehaviorNode>();
    private readonly List<BehaviorEdge> _edges = new List<BehaviorEdge>();
    private readonly Dictionary<string, NodeId> _nodeIdsByKey = new Dictionary<string, NodeId>();

    /// <summary>
    /// Defines the entry node for the graph.
    /// Must be called before Build().
    /// </summary>
    public BehaviorGraphBuilder WithEntry<TAction>(string key, TAction action) where TAction : IAction
    {
        var id = NodeId.Create(key);
        _entryNodeId = id;
        _nodeIdsByKey[key] = id;
        _nodes.Add(new BehaviorNode<TAction>(id, action).AsNonGeneric());
        return this;
    }

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    public BehaviorGraphBuilder AddNode<TAction>(string key, TAction action) where TAction : IAction
    {
        if (_nodeIdsByKey.ContainsKey(key))
            throw new ArgumentException($"Node with key '{key}' already exists", nameof(key));

        if(string.IsNullOrEmpty(key))
            throw new ArgumentException("Node key cannot be null or empty", nameof(key));
        
        if (action == null)
            throw new ArgumentNullException(nameof(action), "Action cannot be null");

        var id = NodeId.Create(key);
        _nodeIdsByKey[key] = id;
        _nodes.Add(new BehaviorNode<TAction>(id, action).AsNonGeneric());
        return this;
    }

    /// <summary>
    /// Adds a conditional edge between two nodes.
    /// Nodes must be added before creating edges.
    /// </summary>
    public BehaviorGraphBuilder AddEdge(string fromKey, string toKey, ICondition condition, int priority = 0)
    {
        if (!_nodeIdsByKey.TryGetValue(fromKey, out var fromId))
            throw new ArgumentException($"Node with key '{fromKey}' not found", nameof(fromKey));
        
        if (!_nodeIdsByKey.TryGetValue(toKey, out var toId))
            throw new ArgumentException($"Node with key '{toKey}' not found", nameof(toKey));

        _edges.Add(new BehaviorEdge(fromId, toId, condition, priority));
        return this;
    }

    /// <summary>
    /// Adds multiple edges from one node to others with same priority.
    /// </summary>
    public BehaviorGraphBuilder AddEdges(string fromKey, params (string toKey, ICondition condition)[] edges)
    {
        foreach (var (toKey, condition) in edges)
        {
            AddEdge(fromKey, toKey, condition);
        }
        return this;
    }

    /// <summary>
    /// Builds and validates the behavior graph.
    /// </summary>
    public BehaviorGraph Build()
    {
        if (_entryNodeId == default)
            throw new InvalidOperationException("Entry node not set. Call WithEntry() before Build().");

        return new BehaviorGraph(_entryNodeId, _nodes, _edges);
    }

    /// <summary>
    /// Resets the builder to start fresh.
    /// </summary>
    public BehaviorGraphBuilder Clear()
    {
        _entryNodeId = default;
        _nodes.Clear();
        _edges.Clear();
        _nodeIdsByKey.Clear();
        return this;
    }
}
