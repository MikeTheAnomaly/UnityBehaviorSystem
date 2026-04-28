using UnityEngine;

namespace BehaviorSystem.Core.Logging
{
    /// <summary>
    /// Settings for BehaviorSystemLogger.
    /// 
    /// Create this asset via: Assets > Create > Behavior System > Logger Settings
    /// Place in a Resources folder so the logger can find it automatically.
    /// 
    /// Toggle EnableLogging in the Inspector to control BehaviorSystem log output.
    /// Note: LogError always outputs regardless of this setting.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorSystemLoggerSettings", menuName = "Behavior System/Logger Settings", order = 1)]
    public class BehaviorSystemLoggerSettings : ScriptableObject
    {
        [Header("Logging Configuration")]
        [Tooltip("Enable or disable BehaviorSystem Log and LogWarning messages. LogError always outputs.")]
        public bool EnableLogging = false;

        [Header("Info")]
        [TextArea(3, 10)]
        [SerializeField]
        private string _helpText = 
            "Enable logging to see BehaviorSystem debug messages in the console.\n\n" +
            "• Log: General debug information\n" +
            "• LogWarning: Potential issues\n" +
            "• LogError: Critical errors (always shown)\n\n" +
            "All messages are prefixed with [BehaviorSystem] for easy filtering.";
    }
}
