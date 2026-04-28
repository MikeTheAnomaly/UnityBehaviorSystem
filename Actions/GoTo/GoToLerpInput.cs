using System;
using UnityEngine;

/// <summary>
/// Input data for GoToLerpAction.
/// </summary>
[Serializable]
public struct GoToLerpInput
{
    [Tooltip("Target local position to move to (relative to parent)")]
    public Vector3 targetPosition;

    [Tooltip("Normal offset from target position")]
    public Vector3 normalOffset;

    [Tooltip("Speed of lerp movement (units per second)")]
    public float lerpSpeed;

    [Tooltip("Distance threshold to consider arrived")]
    public float arrivalThreshold;

    [Tooltip("Maximum time to wait for arrival")]
    public float timeoutSeconds;

    public void Validate()
    {
        if (lerpSpeed <= 0f)
            throw new ArgumentException("GoToLerpInput.lerpSpeed must be > 0.");

        if (arrivalThreshold <= 0f)
            throw new ArgumentException("GoToLerpInput.arrivalThreshold must be > 0.");

        if (timeoutSeconds <= 0f)
            throw new ArgumentException("GoToLerpInput.timeoutSeconds must be > 0.");
    }
}
