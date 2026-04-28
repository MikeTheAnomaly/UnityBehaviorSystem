# Behavior Graph System

Type-safe behavior graphs with conditional state transitions.

## Overview

The Behavior Graph system extends the behavior system with stateful, graph-based execution. Instead of sequential or concurrent action execution, behaviors are organized as nodes in a directed graph with conditional edges.

**Key Features:**
- Type-safe node references via `NodeId` struct
- Conditional transitions via `ICondition` interface
- Priority-based edge selection
- Compatible with existing `IBehaviorOrchestration` interface
- Testable domain logic (no Unity dependencies in core)

## Architecture

### Core Components

#### NodeId
Type-safe identifier for graph nodes, preventing string typo errors:
```csharp
var moveId = NodeId.Create("move");            // Explicit key
var attackId = NodeId.FromType<AttackAction>(); // Type-based key
```

#### BehaviorNode
Wraps an `IAction` with a unique ID:
```csharp
var moveNode = new BehaviorNode<MoveAction>(moveId, moveAction);
var node = moveNode.AsNonGeneric(); // For heterogeneous storage
```

#### BehaviorEdge
Directed edge with conditional transitions:
```csharp
var edge = new BehaviorEdge(
    from: moveId,
    to: attackId,
    condition: new DistanceToTargetCondition(2f),
    priority: 1
);
```

#### BehaviorGraph
Immutable graph with validation:
```csharp
var graph = new BehaviorGraph(
    entryNodeId: moveId,
    nodes: new[] { moveNode, attackNode },
    edges: new[] { edge }
);
```

#### BehaviorGraphExecutor
Executes graph iterations with state transitions:
```csharp
var executor = new BehaviorGraphExecutor(graph);
yield return executor.ExecuteIteration(context);
```

### Unity Integration

#### BehaviorGraphOrchestration
MonoBehaviour implementing `IBehaviorOrchestration`:
```csharp
var orchestration = gameObject.AddComponent<BehaviorGraphOrchestration>();
orchestration.Initialize(graph);
// BehaviorLoop automatically executes via ExecuteActions()
```

#### BehaviorGraphBuilder
Fluent API for graph construction:
```csharp
var graph = new BehaviorGraphBuilder()
    .WithEntry<MoveAction>("move", moveAction)
    .AddNode<AttackAction>("attack", attackAction)
    .AddEdge("move", "attack", new DistanceToTargetCondition(2f))
    .AddEdge("attack", "move", new NotCondition(new DistanceToTargetCondition(3f)))
    .Build();
```

## Conditions

### Built-in Conditions

#### AlwaysTrueCondition
Unconditional transitions (always passes):
```csharp
new AlwaysTrueCondition()
```

#### TargetExistsCondition
Checks if `ExampleEnemy.target` is not null:
```csharp
new TargetExistsCondition()
```

#### DistanceToTargetCondition
Checks if target within range:
```csharp
new DistanceToTargetCondition(2.0f) // True if distance <= 2.0
```

#### NotCondition
Inverts another condition:
```csharp
new NotCondition(new DistanceToTargetCondition(3f)) // True if distance > 3.0
```

### Custom Conditions

Implement `ICondition`:
```csharp
public class HealthBelowCondition : ICondition
{
    private readonly float _threshold;
    
    public HealthBelowCondition(float threshold)
    {
        _threshold = threshold;
    }
    
    public bool Evaluate(GameObject context)
    {
        var health = context.GetComponent<ExampleHealth>();
        return health != null && health.health < _threshold;
    }
}
```

## Usage Example: Enemy AI

### Graph-Based Enemy Behavior

```csharp
[RequireComponent(typeof(BehaviorGraphOrchestration))]
[RequireComponent(typeof(BehaviorLoop))]
public class EnemyBehaviorGraphProvider : MonoBehaviour
{
    [SerializeField] private float _attackRange = 2.0f;
    [SerializeField] private float _retreatRange = 3.0f;

    private void Awake()
    {
        // Create actions
        var moveAction = new MoveToTargetAction();
        var attackAction = new AttackAction();

        // Build graph
        var graph = new BehaviorGraphBuilder()
            .WithEntry<MoveToTargetAction>("move", moveAction)
            .AddNode<AttackAction>("attack", attackAction)
            
            // Move → Attack when close
            .AddEdge("move", "attack", new DistanceToTargetCondition(_attackRange))
            
            // Attack → Move when far
            .AddEdge("attack", "move", new NotCondition(new DistanceToTargetCondition(_retreatRange)))
            
            .Build();

        // Initialize
        GetComponent<BehaviorGraphOrchestration>().Initialize(graph);
    }
}
```

### Execution Flow

1. **Start:** Executor begins at entry node ("move")
2. **Iteration 1:** Execute MoveToTargetAction
3. **Transition:** If distance <= 2.0, transition to "attack"
4. **Iteration 2:** Execute AttackAction
5. **Transition:** If distance > 3.0, transition back to "move"
6. **Repeat:** Continue until behavior stops

## Comparison: Graph vs. Sequential

### Sequential (PriorityQueueBehaviorOrchestration)
- Actions execute in fixed order
- Each action completes before next starts
- No state persistence between iterations
- Example: Move → Attack (always in order)

### Graph (BehaviorGraphOrchestration)
- Actions execute based on current state
- Transitions conditional on game state
- State persists (current node)
- Example: Move ↔ Attack (dynamic transitions)

## Testing

Tests are in `Assets/Tests/BehaviorSystem/Graph/`:

- **NodeIdTests.cs** - Type-safe ID equality and creation
- **BehaviorNodeTests.cs** - Node construction and execution
- **BehaviorGraphTests.cs** - Graph validation and queries
- **BehaviorGraphExecutorTests.cs** - Traversal and transitions
- **BehaviorGraphBuilderTests.cs** - Fluent API construction
- **ConditionTests.cs** - Condition evaluation

Run via Unity Test Runner (Window > General > Test Runner).

## Best Practices

### Design Principles

1. **Single Responsibility:** Each node executes one behavior
2. **Clear Transitions:** Use descriptive condition names
3. **Hysteresis:** Use different thresholds for opposing transitions
   ```csharp
   // Prevents flickering between states
   .AddEdge("move", "attack", new DistanceToTargetCondition(2f))
   .AddEdge("attack", "move", new NotCondition(new DistanceToTargetCondition(3f)))
   ```

### Priority Usage

Use priority for:
- **Fallback edges:** High priority for specific cases, low for defaults
- **Interrupt behaviors:** High priority for urgent transitions
- **Ambiguity resolution:** When multiple edges could be valid

Example:
```csharp
.AddEdge("idle", "flee", new HealthBelowCondition(20f), priority: 2)  // Urgent
.AddEdge("idle", "attack", new TargetExistsCondition(), priority: 1)   // Normal
.AddEdge("idle", "patrol", new AlwaysTrueCondition(), priority: 0)     // Default
```

### State Management

**Actions are stateless** - state lives in components:
```csharp
// ✅ Good: State in component
var enemy = context.GetComponent<ExampleEnemy>();
var target = enemy.target;

// ❌ Bad: State in action
public class MoveAction : IAction {
    private Transform _target; // Persists across contexts!
}
```

## Performance Considerations

- **Graph Construction:** One-time cost in `Awake()`
- **Condition Evaluation:** Every iteration per outgoing edge
- **Edge Priority:** Higher priority = evaluated first (early exit on match)

Optimize conditions:
```csharp
// ✅ Efficient: Early null checks
public bool Evaluate(GameObject context)
{
    if (context == null) return false;
    var enemy = context.GetComponent<ExampleEnemy>();
    if (enemy == null || enemy.target == null) return false;
    
    // Expensive calculation only if needed
    return Vector3.Distance(...) <= _range;
}
```

## Extension Points

### Custom Orchestration

Extend `BehaviorGraphOrchestration` for:
- **Logging:** Track state transitions
- **Debugging:** Visualize current node
- **Analytics:** Record behavior patterns

### Custom Conditions

Implement `ICondition` for:
- Team-based logic (ally vs. enemy)
- Resource checks (ammo, stamina)
- Time-based transitions (day/night cycles)
- Animation state queries

### Builder Extensions

Add helper methods to `BehaviorGraphBuilder`:
```csharp
public static class BehaviorGraphBuilderExtensions
{
    public static BehaviorGraphBuilder AddBidirectionalEdge(
        this BehaviorGraphBuilder builder,
        string node1, string node2,
        ICondition forward, ICondition backward)
    {
        return builder
            .AddEdge(node1, node2, forward)
            .AddEdge(node2, node1, backward);
    }
}
```

## Troubleshooting

### Common Issues

**Issue:** Graph executes but doesn't transition
- **Cause:** Condition always returns false
- **Fix:** Debug condition with logging:
  ```csharp
  public bool Evaluate(GameObject context)
  {
      var result = /* condition logic */;
      Debug.Log($"Condition evaluated: {result}");
      return result;
  }
  ```

**Issue:** NullReferenceException in condition
- **Cause:** Missing component on context
- **Fix:** Add null checks and validation:
  ```csharp
  if (context == null) return false;
  var component = context.GetComponent<T>();
  if (component == null) {
      Debug.LogWarning($"Missing component {typeof(T)} on {context.name}");
      return false;
  }
  ```

**Issue:** Flickering between states
- **Cause:** Same threshold for opposing transitions
- **Fix:** Use hysteresis (different thresholds)

## Integration with Existing System

### Compatible Components

✅ Works with:
- `BehaviorLoop` - Provides iteration control
- `IAction` interface - All existing actions
- `IBehaviorOrchestration` - Standard interface

❌ Not compatible with:
- `BaseUnityAction` auto-subscription - Use builder instead
- Runtime action subscription - Graph is immutable

### Migration Path

From `PriorityQueueBehaviorOrchestration`:
1. Identify state-dependent behaviors
2. Create graph nodes for each action
3. Define conditional edges for transitions
4. Replace provider with `EnemyBehaviorGraphProvider`

Example:
```csharp
// Before: Sequential
var orchestration = new PriorityQueueBehaviorOrchestration();
orchestration.SubscribeAction(moveAction);  // Always runs first
orchestration.SubscribeAction(attackAction); // Always runs second

// After: Conditional graph
var graph = new BehaviorGraphBuilder()
    .WithEntry("move", moveAction)
    .AddNode("attack", attackAction)
    .AddEdge("move", "attack", new DistanceToTargetCondition(2f))
    .Build();
```

## Future Enhancements

Potential additions:
- **Parallel nodes:** Execute multiple actions simultaneously
- **Hierarchical graphs:** Sub-graphs for complex behaviors
- **Visual editor:** Unity editor window for graph authoring
- **Debug visualization:** Gizmos showing current node and valid transitions
- **Serialization:** Save/load graphs from JSON/ScriptableObjects
