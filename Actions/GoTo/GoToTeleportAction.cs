using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Concrete action: Instantly teleports the GameObject to a target position.
/// 
/// Inherits from BaseUnityAction and auto-subscribes to BehaviorLoop.
/// Useful for testing without needing to wait for movement.
/// </summary>
public class GoToTeleportAction : BaseUnityAction, IActionWithInput<GoToTeleportInput>
{
    [SerializeField]
    private GoToTeleportInput _input = new GoToTeleportInput
    {
        targetPosition = Vector3.zero,
        delaySeconds = 0.5f
    };

    private bool _isConfigured;

    public void Configure(GoToTeleportInput input)
    {
        input.Validate();
        _input = input;
        _isConfigured = true;
    }

    public override IEnumerator Execute(GameObject context)
    {
        if (!_isConfigured)
            throw new InvalidOperationException("GoToTeleportAction requires Configure(GoToTeleportInput) before Execute().");

        // Wait for delay (allows visual feedback before teleport)
        if (_input.delaySeconds > 0)
            yield return new WaitForSeconds(_input.delaySeconds);

        // Teleport
        context.transform.localPosition += _input.targetPosition;
    }
}
