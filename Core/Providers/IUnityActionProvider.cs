using UnityEngine;
    /// <summary>
    /// Adapter: MonoBehaviour provider that acts as the entry point for behavior orchestration.
    /// 
    /// Allows designer to select orchestration strategy via inspector dropdown.
    /// Provides convenient access to orchestration instance for action execution.
    /// 
    /// Typical usage:
    /// 1. Attach to GameObject with character/entity
    /// 2. Select orchestration strategy in inspector
    /// 3. Call GetOrchestration() to execute actions
    /// </summary>
    public interface IUnityActionOrchestrationProvider
    {
        /// <summary>
        /// Gets the currently configured orchestration implementation.
        /// </summary>
        IBehaviorOrchestration GetOrchestration();
    }




