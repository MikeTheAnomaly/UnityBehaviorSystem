using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Port: Primary orchestration interface for managing and executing actions.
/// Defines the contract for coroutine-based action lifecycle and context passing between behaviors.
/// 
/// Implementations determine execution strategy (concurrent, sequential, priority-based, etc).
/// Actions read state from the provided GameObject context (Transform, components, etc).
/// 
/// Orchestrators manage their own action subscriptions.
/// BehaviorLoop only tells the orchestrator when to execute.
/// </summary>
public interface IBehaviorOrchestration
{
    /// <summary>
    /// Subscribes an action to this orchestrator.
    /// Called by BaseUnityAction during Awake.
    /// </summary>
    void SubscribeAction(IAction action);

    /// <summary>
    /// Unsubscribes an action from this orchestrator.
    /// </summary>
    void UnsubscribeAction(IAction action);

    /// <summary>
    /// Executes all subscribed actions as coroutines according to the implementation's strategy.
    /// 
    /// Each action receives the context GameObject and can query its current state
    /// (position, components, etc) to determine behavior. Actions can yield to wait on frames
    /// and game state changes.
    /// </summary>
    /// <param name="context">GameObject providing state context for actions to read from.</param>
    /// <returns>Coroutine that executes according to the orchestration strategy.</returns>
    IEnumerator ExecuteActions(GameObject context);
}




