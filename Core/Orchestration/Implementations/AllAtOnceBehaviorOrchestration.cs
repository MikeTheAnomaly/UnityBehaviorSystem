using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation: Executes all subscribed actions concurrently.
/// 
/// Best for:
/// - Actions that can run independently without ordering constraints
/// - Parallel movement, animations, state changes
/// - Maximum throughput when actions don't conflict
/// </summary>
public class AllAtOnceBehaviorOrchestration : MonoBehaviour, IBehaviorOrchestration
{
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

        // Start all action coroutines concurrently
        var runningCoroutines = new List<Coroutine>();
        
        foreach (var action in _subscribedActions)
        {
            try
            {
                var coroutine = StartCoroutine(action.Execute(context));
                runningCoroutines.Add(coroutine);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error starting action coroutine: {ex.Message}");
                throw;
            }
        }

        // Wait for all coroutines to complete
        foreach (var coroutine in runningCoroutines)
        {
            yield return coroutine;
        }
    }
}


