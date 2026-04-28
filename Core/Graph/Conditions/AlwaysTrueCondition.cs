using UnityEngine;

/// <summary>
/// Condition that always evaluates to true.
/// 
/// Use cases:
/// - Unconditional transitions
/// - Default/fallback edges
/// - Testing and prototyping
/// </summary>
public class AlwaysTrueCondition : ICondition
{
    public bool Evaluate(GameObject context) => true;
}
