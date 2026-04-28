using System.Collections;
using UnityEngine;

/// <summary>
/// Port: Secondary port defining the action contract.
/// Actions are atomic behaviors that can be orchestrated independently.
/// 
/// Each action reads state from the provided context GameObject
/// (Transform, attached components, etc) to determine its execution.
/// 
/// Actions may optionally depend on providers (accessed via BaseAction).
/// 
/// Actions return coroutines that can:
/// - Wait for frames (yield return null)
/// - Wait for time (yield return WaitForSeconds)
/// - Wait for conditions (yield return new WaitUntil(() => condition))
/// - Respond dynamically to game state changes
/// </summary>
public interface IAction
{
    /// <summary>
    /// Executes the action as a coroutine.
    /// 
    /// The context GameObject provides all necessary state information:
    /// - Transform (current position, rotation, scale)
    /// - Attached components (Rigidbody, NavMeshAgent, etc)
    /// - Custom components (health, team, etc)
    /// 
    /// Coroutine can yield to wait on frames, time, or game state conditions.
    /// </summary>
    /// <param name="context">GameObject providing state and components for action execution.</param>
    /// <returns>Coroutine that executes the action.</returns>
    IEnumerator Execute(GameObject context);
}

    



