    /// <summary>
    /// Port: Optional interface for actions that need to specify execution priority.
    /// Used by priority-based orchestration implementations to determine execution order.
    /// 
    /// Actions that do not implement this interface default to priority 1.
    /// Higher values = higher priority = earlier execution.
    /// </summary>
    public interface IPriority
    {
        /// <summary>
        /// Gets the priority level for this action.
        /// Default implementation should return 1 if action doesn't explicitly set priority.
        /// </summary>
        int Priority { get; }
    }




