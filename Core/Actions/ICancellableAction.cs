using UnityEngine;

/// <summary>
/// Interface for actions that can be interrupted mid-execution.
/// 
/// Enables reactive behavior graphs where state transitions can occur
/// without waiting for current action to complete.
/// 
/// The executor manages the cancellation lifecycle:
/// 1. Calls Reset() before execution to clear cancellation state
/// 2. Calls Cancel() mid-execution if a transition becomes valid
/// 3. Action should check IsCancelled in its execution loop
/// 
/// Usage:
/// - Implement this interface on actions that should respond to interrupts
/// - Implement Reset() to clear the cancelled flag
/// - Set IsCancelled flag when Cancel() is called
/// - Check IsCancelled in your Execute loop and exit early
/// 
/// Example:
/// public override IEnumerator Execute(GameObject context)
/// {
///     // No need to reset - executor calls Reset() automatically
///     while (!complete && !IsCancelled)
///     {
///         DoWork();
///         yield return null;
///     }
/// }
/// </summary>
public interface ICancellableAction : IAction
{
    /// <summary>
    /// Resets the cancellation state before execution.
    /// Called by executor before starting action execution.
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Requests the action to cancel execution.
    /// Action should clean up and exit its Execute coroutine.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Gets whether the action has been cancelled.
    /// Actions should check this flag during execution.
    /// </summary>
    bool IsCancelled { get; }
}
