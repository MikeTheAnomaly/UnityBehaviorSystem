using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Provider: Creates and manages GoTo movement actions.
/// 
/// Single component that provides two concrete action implementations:
/// - Lerp: Smoothly interpolates to target with configurable offset
/// - Test: Instantly teleports to target for quick testing
/// 
/// The concrete actions inherit from BaseUnityAction and auto-subscribe
/// to the BehaviorLoop orchestration system.
/// 
/// Usage:
/// 1. Attach GoToActionProvider to GameObject
/// 2. Attach BehaviorLoop to same or parent GameObject
/// 3. Select movement mode (Lerp or Test) in Inspector
/// 4. Configure parameters and target position
/// 5. On Awake: Provider creates appropriate action component
/// 6. Action auto-subscribes to BehaviorLoop via BaseUnityAction
/// </summary>
public class GoToActionProvider : MonoBehaviour
{
    /// <summary>
    /// Movement mode selection.
    /// </summary>
    public enum MovementMode
    {
        /// <summary>Smoothly lerps to target with normal offset.</summary>
        Lerp = 0,
        
        /// <summary>Instantly teleports to target for testing.</summary>
        Test = 1
    }

    [Header("Movement Mode")]
    [SerializeField]
    private MovementMode _mode = MovementMode.Lerp;

    [Header("Lerp Mode Input")]
    [SerializeField]
    private GoToLerpInput _lerpInput = new GoToLerpInput
    {
        targetPosition = Vector3.zero,
        normalOffset = Vector3.zero,
        lerpSpeed = 5f,
        arrivalThreshold = 0.1f,
        timeoutSeconds = 10f
    };

    [Header("Test Mode Input")]
    [SerializeField]
    private GoToTeleportInput _teleportInput = new GoToTeleportInput
    {
        targetPosition = Vector3.zero,
        delaySeconds = 0.5f
    };

    private BaseUnityAction _currentAction;

    private void Awake()
    {
        // Create the appropriate action based on mode
        switch (_mode)
        {
            case MovementMode.Lerp:
                var lerpAction = gameObject.AddComponent<GoToLerpAction>();
                lerpAction.Configure(_lerpInput);
                _currentAction = lerpAction;
                break;

            case MovementMode.Test:
                var teleportAction = gameObject.AddComponent<GoToTeleportAction>();
                teleportAction.Configure(_teleportInput);
                _currentAction = teleportAction;
                break;

            default:
                BehaviorSystemLogger.LogError($"Unknown MovementMode: {_mode}", this);
                break;
        }
    }

    /// <summary>
    /// Sets the target position and updates the action.
    /// </summary>
    public void SetTarget(Vector3 targetPosition)
    {
        _lerpInput.targetPosition = targetPosition;
        _teleportInput.targetPosition = targetPosition;

        // Update existing action if it exists
        if (_currentAction != null)
        {
            if (_currentAction is GoToLerpAction lerpAction)
                lerpAction.Configure(_lerpInput);
            else if (_currentAction is GoToTeleportAction teleportAction)
                teleportAction.Configure(_teleportInput);
        }
    }

    /// <summary>
    /// Gets the current target position.
    /// </summary>
    public Vector3 GetTarget() => _lerpInput.targetPosition;
}

