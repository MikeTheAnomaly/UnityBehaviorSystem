using UnityEngine;

/// <summary>
/// Interface: Condition that determines if a graph edge can be traversed.
/// 
/// Used by BehaviorGraph to control state transitions between nodes.
/// Implementations can check game state, distance, health, etc.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Evaluates whether the condition is satisfied.
    /// </summary>
    /// <param name="context">GameObject context for state evaluation.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    bool Evaluate(GameObject context);
}
