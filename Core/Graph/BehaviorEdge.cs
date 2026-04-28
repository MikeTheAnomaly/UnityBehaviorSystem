using System;

/// <summary>
/// Directed edge in a behavior graph with conditional transitions.
/// 
/// Features:
/// - Conditional transitions via ICondition
/// - Priority ordering for multiple outgoing edges
/// - Type-safe node references via NodeId
/// 
/// Higher priority edges are evaluated first when multiple edges are valid.
/// </summary>
public class BehaviorEdge
{
    public NodeId From { get; }
    public NodeId To { get; }
    public ICondition Condition { get; }
    public int Priority { get; }

    public BehaviorEdge(NodeId from, NodeId to, ICondition condition, int priority = 0)
    {
        From = from;
        To = to;
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        Priority = priority;
    }

    /// <summary>
    /// Evaluates if this edge can be traversed.
    /// </summary>
    public bool CanTraverse(UnityEngine.GameObject context) => Condition.Evaluate(context);
}
