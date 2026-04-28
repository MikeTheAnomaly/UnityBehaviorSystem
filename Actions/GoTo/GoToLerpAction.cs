using System;
using System.Collections;
using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Concrete action: Lerps the GameObject to a target position with offset.
/// 
/// Inherits from BaseUnityAction and auto-subscribes to BehaviorLoop.
/// Smoothly interpolates to target position using Vector3.MoveTowards.
/// </summary>
public class GoToLerpAction : BaseUnityAction, IActionWithInput<GoToLerpInput>
{
    [SerializeField]
    private GoToLerpInput _input = new GoToLerpInput
    {
        targetPosition = Vector3.zero,
        normalOffset = Vector3.zero,
        lerpSpeed = 5f,
        arrivalThreshold = 0.1f,
        timeoutSeconds = 10f
    };

    private bool _isConfigured;

    public void Configure(GoToLerpInput input)
    {
        input.Validate();
        _input = input;
        _isConfigured = true;
    }

    public override IEnumerator Execute(GameObject context)
    {
        if (!_isConfigured)
            throw new InvalidOperationException("GoToLerpAction requires Configure(GoToLerpInput) before Execute().");

        // Calculate final target with normal offset
        var finalTarget = context.transform.position + _input.targetPosition + _input.normalOffset;
        var elapsedTime = 0f;

        while (elapsedTime < _input.timeoutSeconds)
        {
            var currentPosition = context.transform.position;
            var distanceToTarget = Vector3.Distance(currentPosition, finalTarget);

            // Check if arrived
            if (distanceToTarget <= _input.arrivalThreshold)
            {
                // Snap to exact position
                context.transform.position = finalTarget;
                yield break;
            }

            // Calculate lerp step
            var step = _input.lerpSpeed * Time.deltaTime;
            var newPosition = Vector3.MoveTowards(currentPosition, finalTarget, step);
            
            // Update position
            context.transform.position = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Timeout warning
        BehaviorSystemLogger.LogWarning(
            $"GoToLerpAction on {context.name} timed out after {_input.timeoutSeconds}s. " +
            $"Distance remaining: {Vector3.Distance(context.transform.localPosition, finalTarget):F2}",
            context);
    }
}
