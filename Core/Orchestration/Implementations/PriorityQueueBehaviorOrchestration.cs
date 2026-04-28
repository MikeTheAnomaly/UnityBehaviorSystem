using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implementation: Executes subscribed actions sequentially in priority order.
/// 
/// Best for:
/// - Actions with dependencies or sequencing requirements
/// - Resource constraints (e.g., only one movement action at a time)
/// - Turn-based or ordered behavior patterns
/// </summary>
public class PriorityQueueBehaviorOrchestration : MonoBehaviour, IBehaviorOrchestration
{
    private const int DefaultPriority = 1;
    private List<IAction> _subscribedActions = new List<IAction>();

    public void SubscribeAction(IAction action)
    {
        if (action != null && !_subscribedActions.Contains(action))
        {
            _subscribedActions.Add(action);
        }
    }

    public void UnsubscribeAction(IAction action)
    {
        if (action != null)
        {
            _subscribedActions.Remove(action);
        }
    }

    public IEnumerator ExecuteActions(GameObject context)
    {
        if (context == null)
            throw new System.ArgumentNullException(nameof(context), "Context GameObject cannot be null");

        if (_subscribedActions.Count == 0)
            yield break;

        // Sort actions by priority (highest first)
        var sortedActions = _subscribedActions
            .Select(action => new
            {
                Action = action,
                Priority = (action as IPriority)?.Priority ?? DefaultPriority
            })
            .OrderByDescending(x => x.Priority)
            .Select(x => x.Action)
            .ToList();

        // Execute actions sequentially in priority order
        foreach (var action in sortedActions)
        {
            yield return ExecuteActionCoroutine(context, action);
        }
    }

    private IEnumerator ExecuteActionCoroutine(GameObject context, IAction action)
    {
        Coroutine actionCoroutine = null;
        try
        {
            actionCoroutine = StartCoroutine(action.Execute(context));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error executing action: {ex.Message}");
            throw;
        }

        if (actionCoroutine != null)
            yield return actionCoroutine;
    }
}
