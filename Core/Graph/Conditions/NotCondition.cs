using UnityEngine;
using System;

/// <summary>
/// Condition that inverts another condition.
/// Useful for creating complementary transitions (e.g., "not in range").
/// 
/// Example:
/// // Transition when OUT of range
/// new NotCondition(new DistanceToTargetCondition&lt;Enemy&gt;(e => e.target, 5f))
/// </summary>
public class NotCondition : ICondition
{
    private readonly ICondition _inner;

    public NotCondition(ICondition inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool Evaluate(GameObject context) => !_inner.Evaluate(context);
}
