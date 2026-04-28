using System;
using UnityEngine;

/// <summary>
/// Input data for GoToTeleportAction.
/// </summary>
[Serializable]
public struct GoToTeleportInput
{
    [Tooltip("Target local position to teleport to (relative to parent)")]
    public Vector3 targetPosition;

    [Tooltip("Delay before teleporting (for visual feedback)")]
    public float delaySeconds;

    public void Validate()
    {
        if (delaySeconds < 0f)
            throw new ArgumentException("GoToTeleportInput.delaySeconds must be >= 0.");
    }
}
