using UnityEngine;

/// <summary>
/// Adapter: MonoBehaviour that provides orchestration instance selection via inspector.
/// 
/// Designers can choose execution strategy (AllAtOnce or PriorityQueue) from a dropdown.
/// Automatically attaches the selected orchestration component to the GameObject on Awake.
/// </summary>
[Icon("Assets/_Scripts/BehaviorSystem/Editor/Icons/orchestration_provider_icon.png")]
public class UnityActionOrchestrationProvider : MonoBehaviour, IUnityActionOrchestrationProvider
{
    /// <summary>
    /// Strategy selection for action orchestration.
    /// </summary>
    public enum OrchestrationStrategy
    {
        /// <summary>All actions execute concurrently via coroutines.</summary>
        AllAtOnce = 0,
        
        /// <summary>Actions execute sequentially in priority order.</summary>
        PriorityQueue = 1
    }

    [SerializeField]
    private OrchestrationStrategy _strategy = OrchestrationStrategy.AllAtOnce;

    private IBehaviorOrchestration _orchestration;

    private void Awake()
    {
        // Attach the orchestration component based on selected strategy
        _orchestration = gameObject.GetComponent<IBehaviorOrchestration>();
        if (_orchestration == null){
            CreateOrchestrationInstance();
        }
    }

    /// <summary>
    /// Gets the orchestration instance.
    /// </summary>
    public IBehaviorOrchestration GetOrchestration()
    {
        if(_orchestration == null)
        {
            return CreateOrchestrationInstance();
        }
        return _orchestration;
    }

    public IBehaviorOrchestration CreateOrchestrationInstance()
    {
        _orchestration = _strategy switch
        {
            OrchestrationStrategy.AllAtOnce => gameObject.AddComponent<AllAtOnceBehaviorOrchestration>(),
            OrchestrationStrategy.PriorityQueue => gameObject.AddComponent<PriorityQueueBehaviorOrchestration>(),
            _ => gameObject.AddComponent<AllAtOnceBehaviorOrchestration>() // Safe default
        };
        return _orchestration;
    }
}



