using UnityEngine;
using System;

/// <summary>
/// Generic condition that checks if target is within a specified range.
/// 
/// Use cases:
/// - Transition from pursue to attack when in range
/// - Return to pursue when target moves out of range
/// - Multi-range behaviors (close, medium, far)
/// 
/// Example:
/// - Pursue → Attack when distance &lt;= 2.0
/// - Attack → Pursue when distance &gt; 2.5 (hysteresis to prevent flickering)
/// 
/// Usage:
/// new IsWithinDistanceToTargetCondition&lt;ExampleEnemy&gt;(e => e.target, 2.0f)
/// </summary>
public class IsWithinDistanceToTargetCondition<TComponent> : ICondition where TComponent : Component
{
    private readonly Func<TComponent, Transform> _targetSelector;
    private readonly float _range;

    public IsWithinDistanceToTargetCondition(Func<TComponent, Transform> targetSelector, float range)
    {
        _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
        _range = range;
    }

    public bool Evaluate(GameObject context)
    {
        if (context == null)
            return false;

        var component = context.GetComponent<TComponent>();
        if (component == null)
            return false;

        var target = _targetSelector(component);
        if (target == null)
            return false;

        float distance = Vector3.Distance(context.transform.position, target.position);
        bool value = distance <= _range;
        Debug.Log($"IsWithinDistanceToTargetCondition: Distance to target is {distance}, range is {_range}, condition is {value}");
        return value;
    }
}
