# Behavior System Logging

## Overview
The Behavior System includes a custom logging system that provides filtered debug output with Inspector-based controls.

## Features
- **Toggleable Logging**: Enable/disable logs via Inspector without code changes
- **Separate Log Stream**: All messages prefixed with `[BehaviorSystem]` for easy Console filtering
- **Three Log Levels**:
  - `Log`: General debug information
  - `LogWarning`: Potential issues
  - `LogError`: Critical errors (always visible)
- **Error Surfacing**: `LogError` always outputs to Unity's standard logging

## Setup

### 1. Create Logger Settings Asset
1. In Unity, go to: **Assets > Create > Behavior System > Logger Settings**
2. Name it `BehaviorSystemLoggerSettings`
3. Move it to a **Resources** folder (create one if needed)
   - Path should be: `Assets/Resources/BehaviorSystemLoggerSettings.asset`

### 2. Configure Logging
1. Select the `BehaviorSystemLoggerSettings` asset in the Project window
2. In the Inspector, check/uncheck **Enable Logging** to control output
3. `LogError` messages always appear regardless of this setting

## Usage in Code

```csharp
using BehaviorSystem.Core.Logging;

public class MyBehaviorAction : BaseUnityAction
{
    public override IEnumerator Execute(GameObject context)
    {
        // General debug info (only shows if logging enabled)
        BehaviorSystemLogger.Log("Executing action");
        
        // Warning message (only shows if logging enabled)
        BehaviorSystemLogger.LogWarning("Potential issue detected");
        
        // Error message (ALWAYS shows)
        BehaviorSystemLogger.LogError("Critical failure!");
        
        // All methods support context parameter for Console highlighting
        BehaviorSystemLogger.Log("Message", gameObject);
        
        yield return null;
    }
}
```

## Console Filtering
To view only BehaviorSystem logs in the Unity Console:
1. Open the Console window
2. Click the search/filter box
3. Type: `[BehaviorSystem`
4. Only logs from the Behavior System will be displayed

## Architecture
- **BehaviorSystemLogger**: Static class with Log, LogWarning, LogError methods
- **BehaviorSystemLoggerSettings**: ScriptableObject asset for Inspector configuration
- Settings are loaded automatically from Resources folder on first use
