using UnityEngine;
using System;

/// <summary>
/// Generic condition that checks if a component has a valid target field.
/// 
/// Use cases:
/// - Transition from idle to pursue when target acquired
/// - Guard against attacking without a target
/// - Return to patrol when target lost
/// 
/// Example:
/// new TargetExistsCondition&lt;ExampleEnemy&gt;(e => e.target)
/// </summary>
public class TargetExistsCondition<TComponent> : ICondition where TComponent : Component
{
    private readonly Func<TComponent, Transform> _targetSelector;

    public TargetExistsCondition(Func<TComponent, Transform> targetSelector)
    {
        _targetSelector = targetSelector ?? throw new ArgumentNullException(nameof(targetSelector));
    }

    public bool Evaluate(GameObject context)
    {
        if (context == null)
            return false;

        var component = context.GetComponent<TComponent>();
        if (component == null)
            return false;

        var target = _targetSelector(component);
        return target != null;
    }
}
