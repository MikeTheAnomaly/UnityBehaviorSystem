using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Base class for actions that are Unity MonoBehaviours.
/// 
/// Auto-subscribes to IBehaviorOrchestration on the same GameObject.
/// BehaviorLoop tells the orchestrator when to execute.
/// 
/// Usage:
/// 1. Create concrete action inheriting from BaseUnityAction
/// 2. Attach to GameObject
/// 3. Attach an IBehaviorOrchestration implementation (e.g., AllAtOnceBehaviorOrchestration)
/// 4. Attach BehaviorLoop to trigger execution
/// 5. Actions auto-subscribe to the orchestrator in Awake
/// </summary>
public abstract class BaseUnityAction : MonoBehaviour, IAction
{
    protected virtual void Start()
    {
        // Find IBehaviorOrchestration on this GameObject
        var orchestration = GetComponent<IBehaviorOrchestration>();
        
        if (orchestration == null)
        {
            BehaviorSystemLogger.LogError(
                $"BaseUnityAction on {gameObject.name} could not find IBehaviorOrchestration component. " +
                "Attach an orchestration strategy (e.g., AllAtOnceBehaviorOrchestration) to this GameObject.",
                gameObject);
            return;
        }

        // Subscribe this action to the orchestrator
        orchestration.SubscribeAction(this);
    }

    /// <summary>
    /// Executes the action. Implemented by concrete subclasses.
    /// </summary>
    public abstract System.Collections.IEnumerator Execute(GameObject context);
}
