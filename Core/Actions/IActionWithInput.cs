using UnityEngine;

/// <summary>
/// Optional contract for actions that require a strongly-typed input struct.
/// Keeps IAction unchanged while enabling strict input configuration.
/// </summary>
/// <typeparam name="TInput">Per-action input struct type.</typeparam>
public interface IActionWithInput<TInput> : IAction
{
    /// <summary>
    /// Configures the action with required input data.
    /// Implementations should validate and throw on invalid input.
    /// </summary>
    /// <param name="input">Required input data for the action.</param>
    void Configure(TInput input);
}
