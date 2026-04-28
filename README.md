# Behavior System Guide

A flexible, modular system for managing and executing character behaviors in Unity using coroutines and action orchestration.

## Overview

The Behavior System allows you to:
- Define reusable actions (move, attack, etc.)
- Compose behaviors by combining multiple actions
- Execute actions sequentially or in parallel
- Auto-subscribe actions to the orchestration system
- Support both gameplay and testing with minimal boilerplate

## Core Components

### 1. **BehaviorLoop** (`Core/Loop/BehaviorLoop.cs`)
The orchestrator that manages action execution.

**Features:**
- Execute actions once, in a loop, or N times
- Pause/Resume execution
- Auto-start on Scene Start
- Supports different orchestration strategies

**Usage:**
```csharp
var behaviorLoop = gameObject.AddComponent<BehaviorLoop>();
behaviorLoop.StartBehaviorLoop();
```

### 2. **Actions** (`Core/Actions/`)
Concrete implementations of `IAction` that execute behavior.

**Key Classes:**
- `IAction` - Interface defining action contract (execute coroutine)
- `BaseUnityAction` - Auto-subscribes to BehaviorLoop on Awake

### 3. **Action Providers** (`Actions/*/ActionProvider.cs`)
MonoBehaviour components that create and manage actions.

---

## Creating a New Action

### Step 1: Create the Concrete Action Class

Create a class inheriting from `BaseUnityAction`:

```csharp
// Assets/Scripts/BehaviorSystem/Actions/Attack/AttackAction.cs
using System.Collections;
using UnityEngine;

public class AttackAction : BaseUnityAction
{
    [SerializeField]
    [Tooltip("Damage to deal")]
    private float _damage = 10f;

    [SerializeField]
    [Tooltip("Attack range")]
    private float _range = 5f;

    public void SetDamage(float damage)
    {
        _damage = damage;
    }

    public override IEnumerator Execute(GameObject context)
    {
        // Get target in range
        var colliders = Physics.OverlapSphere(context.transform.position, _range);
        
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                // Deal damage
                if (collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(_damage);
                }
            }
        }
        
        yield return null;
    }
}
```

### Step 2: Create the Action Provider

Create a provider that instantiates your action:

```csharp
// Assets/Scripts/BehaviorSystem/Actions/Attack/AttackActionProvider.cs
using UnityEngine;

public class AttackActionProvider : MonoBehaviour
{
    [SerializeField]
    private float _damage = 10f;

    [SerializeField]
    private float _range = 5f;

    private AttackAction _currentAction;

    private void Awake()
    {
        // Create the action component
        var action = gameObject.AddComponent<AttackAction>();
        action.SetDamage(_damage);
        _currentAction = action;
    }

    public void SetDamage(float damage)
    {
        _damage = damage;
        if (_currentAction != null)
            _currentAction.SetDamage(damage);
    }

    public float GetDamage() => _damage;
}
```

### Step 3: Use in Scene

1. **Attach to GameObject:**
   ```
   YourCharacter
   ├─ BehaviorLoop (orchestrator)
   └─ AttackActionProvider (creates action)
   ```

2. **Configure in Inspector:**
   - Set Damage: 15
   - Set Range: 7

3. **Execute:**
   ```csharp
   BehaviorLoop.StartBehaviorLoop();
   ```

---

## Built-in Actions

### GoTo Actions

**GoToLerpAction** - Smoothly moves to target with offset
- Uses `localPosition` (relative to parent)
- Configurable speed and arrival threshold
- Example:
  ```csharp
  var provider = gameObject.AddComponent<GoToActionProvider>();
  provider.SetTarget(new Vector3(5, 0, 10));
  ```

**GoToTeleportAction** - Instantly teleports to target
- Useful for testing
- Optional delay for visual feedback
- Example:
  ```csharp
  var provider = gameObject.AddComponent<GoToActionProvider>();
  // Set mode to Test in Inspector
  provider.SetTarget(new Vector3(5, 0, 10));
  ```

## Orchestration Strategies

Define how actions execute:

### AllAtOnceBehaviorOrchestration
Executes all actions concurrently:
```
Action1 ──────────────┐
Action2 ──────────────┤
Action3 ──────────────┘
```

### PriorityQueueBehaviorOrchestration
Executes higher priority actions first (sequential):
```
Action1 (priority 2) ──────────
Action2 (priority 1) ──────────
Action3 (priority 3) ──────────
```

---

## Architecture Pattern

The system uses a clean separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│ Scene (Inspector Configuration)                             │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ GameObject: Enemy                                      │  │
│ │ ├─ BehaviorLoop (Orchestrator)                        │  │
│ │ │  └─ Settings: Once/Loop, Orchestration Strategy   │  │
│ │ ├─ GoToActionProvider (Create + Configure)          │  │
│ │ │  └─ Auto-creates: GoToLerpAction               │  │
│ └────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
        ↓
┌─────────────────────────────────────────────────────────────┐
│ Behavior Loop Execution                                     │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ 1. OnAwake: Providers create actions                  │  │
│ │ 2. Actions auto-subscribe to BehaviorLoop            │  │
│ │ 3. OnStart: BehaviorLoop.StartBehaviorLoop() called  │  │
│ │ 4. Orchestrator executes actions (strategy dependent)│  │
│ │ 5. Actions yield to wait for completion             │  │
│ │ 6. Loop continues based on mode (Once/Loop/LoopN)   │  │
│ └────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Best Practices

### ✅ Do's

1. **Use Local Position for GoTo**
   - Enables parenting and relative movement
   ```csharp
   context.transform.localPosition = targetPosition;
   ```

2. **Inherit from BaseUnityAction**
   - Automatic subscription to orchestrator
   - No provider boilerplate

3. **Use Serialized Fields**
   - Makes actions configurable in Inspector
   - Easy designer iteration

4. **Provide Public Setters**
   - Allow runtime parameter updates
   ```csharp
   public void SetJumpHeight(float height) { _jumpHeight = height; }
   ```

5. **Log Warnings for Timeout/Errors**
   - Debug issues easily
   ```csharp
   Debug.LogWarning($"Action {name} timed out", context);
   ```

### ❌ Don'ts

1. **Don't use World Position for movements**
   - Breaks parenting and relative transformations

2. **Don't require external providers**
   - Actions should be self-contained

3. **Don't ignore error states**
   - Validate components exist before use

4. **Don't block indefinitely**
   - Always have timeouts for physics/wait operations

5. **Don't create new CosmosClient-like patterns**
   - Keep architecture simple and flat

---

## Testing Actions

```csharp
[UnityTest]
public IEnumerator AttackAction_InRange_DamagesEnemy()
{
    // Arrange
    var actor = new GameObject("Attacker");
    var enemy = new GameObject("Enemy");
    var damageable = enemy.AddComponent<MockDamageable>();
    
    actor.AddComponent<BehaviorLoop>();
    var provider = actor.AddComponent<AttackActionProvider>();
    provider.SetDamage(15f);
    
    // Act
    var behaviorLoop = actor.GetComponent<BehaviorLoop>();
    behaviorLoop.StartBehaviorLoop();
    
    yield return null; // One frame for action execution
    
    // Assert
    Assert.AreEqual(15f, damageable.DamageTaken);
    
    Object.DestroyImmediate(actor);
    Object.DestroyImmediate(enemy);
}
```

---

## Common Patterns

### Sequential Movement + Attack
```csharp
// GameObject has:
// - BehaviorLoop (Once mode)
// - GoToActionProvider (target: enemy position)
// - AttackActionProvider (damage: 20)

// Executes: Move → Attack
```

### Continuous Patrol + Jump
```csharp
// GameObject has:
// - BehaviorLoop (Loop mode)
// - GoToActionProvider (waypoint 1)
// - GoToActionProvider (waypoint 2)
// - JumpActionProvider (height: 2)

// Executes repeatedly: Move → Move → Jump → Loop
```

### Priority-Based Behavior
```csharp
// Set Orchestration Strategy to PriorityQueueBehaviorOrchestration
// 
// AttackActionProvider: Priority 2 (urgent)
// MoveActionProvider: Priority 1 (normal)
// 
// Executes: Attack (high priority) → Move → Loop
```

---

## File Structure

```
Assets/Scripts/BehaviorSystem/
├── Core/
│   ├── Actions/
│   │   ├── IAction.cs
│   │   ├── BaseAction.cs
│   │   └── BaseUnityAction.cs
│   ├── Loop/
│   │   └── BehaviorLoop.cs
│   └── Orchestration/
│       ├── IBehaviorOrchestration.cs
│       ├── IPriority.cs
│       └── Implementations/
│           ├── AllAtOnceBehaviorOrchestration.cs
│           └── PriorityQueueBehaviorOrchestration.cs
│
└── Actions/
    ├── GoTo/
    │   ├── GoToLerpAction.cs
    │   ├── GoToTeleportAction.cs
    │   └── GoToActionProvider.cs
    └── [YourAction]/
        ├── [YourAction].cs
        └── [YourAction]Provider.cs
```

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Action not executing | BehaviorLoop not started | Call `behaviorLoop.StartBehaviorLoop()` |
| Actions not subscribing | BehaviorLoop added after actions | Add BehaviorLoop first, then action providers |
| Infinite wait | Action yields without timeout | Add timeout with `WaitForSeconds` or `elapsedTime` check |
| Position snapping | Using `transform.position` instead of `localPosition` | Use `transform.localPosition` for parent-relative movement |
| Jump not working | Rigidbody is kinematic | Set Rigidbody to dynamic (Is Kinematic = false) |

---

## References

- `BehaviorLoop.cs` - Orchestration entry point
- `IAction.cs` - Action contract
- `BaseUnityAction.cs` - Base class for all actions
- `GoTo/GoToActionProvider.cs` - Example implementation
