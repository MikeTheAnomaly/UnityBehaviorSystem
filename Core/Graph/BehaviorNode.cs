using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Generic node in a behavior graph with type-safe action reference.
/// 
/// Provides:
/// - Type safety for action access
/// - Compile-time checking of action types
/// - Conversion to non-generic for graph storage
/// 
/// Usage:
/// var node = new BehaviorNode&lt;MoveAction&gt;(moveId, moveAction);
/// </summary>
public class BehaviorNode<TAction> where TAction : IAction
{
    public NodeId Id { get; }
    public TAction Action { get; }

    public BehaviorNode(NodeId id, TAction action)
    {
        Id = id;
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// Converts to non-generic BehaviorNode for graph storage.
    /// </summary>
    public BehaviorNode AsNonGeneric() => new BehaviorNode(Id, Action);

    public IEnumerator Execute(GameObject context) => Action.Execute(context);
}

/// <summary>
/// Non-generic wrapper for BehaviorNode.
/// Allows storage in heterogeneous collections (e.g., graphs with mixed action types).
/// </summary>
public class BehaviorNode
{
    public NodeId Id { get; }
    public IAction Action { get; }

    public BehaviorNode(NodeId id, IAction action)
    {
        Id = id;
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public IEnumerator Execute(GameObject context) => Action.Execute(context);
}
