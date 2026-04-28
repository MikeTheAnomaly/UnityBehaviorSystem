# Behavior Graph Quick Start

## 5-Minute Tutorial

### 1. Create Your Actions

```csharp
// Reuse existing actions or create new ones
var moveAction = new MoveToTargetAction();
var attackAction = new AttackAction();
```

### 2. Build Your Graph

```csharp
var graph = new BehaviorGraphBuilder()
    // Define entry point
    .WithEntry<MoveToTargetAction>("move", moveAction)
    
    // Add other states
    .AddNode<AttackAction>("attack", attackAction)
    
    // Define transitions
    .AddEdge("move", "attack", 
        new DistanceToTargetCondition<ExampleEnemy>(e => e.target, 2f))
    .AddEdge("attack", "move", 
        new NotCondition(new DistanceToTargetCondition<ExampleEnemy>(e => e.target, 3f)))
    
    .Build();
```

### 3. Wire It Up

```csharp
// In your provider's Awake()
var orchestration = gameObject.AddComponent<BehaviorGraphOrchestration>();
orchestration.Initialize(graph);
gameObject.AddComponent<BehaviorLoop>();
```

### 4. Done!

Your enemy now:
1. Moves toward target
2. Attacks when within 2 units
3. Returns to moving when target escapes beyond 3 units

## Common Patterns

### Simple State Machine

```csharp
.WithEntry<IdleAction>("idle", idleAction)
.AddNode<PatrolAction>("patrol", patrolAction)
.AddNode<ChaseAction>("chase", chaseAction)
.AddEdge("idle", "patrol", new AlwaysTrueCondition())
.AddEdge("patrol", "chase", new TargetExistsCondition<Enemy>(e => e.target))
.AddEdge("chase", "patrol", new NotCondition(new TargetExistsCondition<Enemy>(e => e.target)))
```

### Priority-Based Transitions

```csharp
// Higher priority = checked first
.AddEdge("idle", "flee", healthLow, priority: 2)  // Urgent
.AddEdge("idle", "attack", targetNear, priority: 1) // Normal
.AddEdge("idle", "patrol", alwaysTrue, priority: 0) // Default
```

### Hysteresis (Prevent Flickering)

```csharp
// Different thresholds for enter/exit
.AddEdge("move", "attack", withinRange(2f))   // Enter at 2
.AddEdge("attack", "move", outOfRange(3f))     // Exit at 3
```

## Built-in Conditions

```csharp
// Always transitions
new AlwaysTrueCondition()

// Check if target exists
new TargetExistsCondition<ExampleEnemy>(e => e.target)

// Check distance to target
new DistanceToTargetCondition<ExampleEnemy>(e => e.target, 5f)

// Invert any condition
new NotCondition(someCondition)
```

## Custom Conditions

```csharp
public class HealthBelowCondition : ICondition
{
    private readonly float _threshold;
    
    public HealthBelowCondition(float threshold) => _threshold = threshold;
    
    public bool Evaluate(GameObject context)
    {
        var health = context.GetComponent<ExampleHealth>();
        return health != null && health.health < _threshold;
    }
}

// Usage:
.AddEdge("attack", "flee", new HealthBelowCondition(20f))
```

## Debugging

### Visualize Current State

```csharp
var orchestration = GetComponent<BehaviorGraphOrchestration>();
Debug.Log($"Current state: {orchestration.GetCurrentNodeId()}");
```

### Log Transitions

```csharp
public class LoggingCondition : ICondition
{
    private readonly ICondition _inner;
    private readonly string _name;
    
    public LoggingCondition(ICondition inner, string name)
    {
        _inner = inner;
        _name = name;
    }
    
    public bool Evaluate(GameObject context)
    {
        var result = _inner.Evaluate(context);
        Debug.Log($"Condition '{_name}': {result}");
        return result;
    }
}
```

## Testing Your Graph

```csharp
[Test]
public void Graph_TransitionsCorrectly()
{
    // Arrange
    var context = new GameObject();
    var graph = new BehaviorGraphBuilder()
        .WithEntry<MockAction>("start", new MockAction())
        .AddNode<MockAction>("end", new MockAction())
        .AddEdge("start", "end", new AlwaysTrueCondition())
        .Build();
    var executor = new BehaviorGraphExecutor(graph);
    
    // Act
    yield return executor.ExecuteIteration(context);
    yield return executor.ExecuteIteration(context);
    
    // Assert
    Assert.AreEqual(NodeId.Create("end"), executor.GetCurrentNodeId());
}
```

## Common Mistakes

### ❌ Wrong: Using Same Threshold

```csharp
.AddEdge("move", "attack", withinRange(2f))
.AddEdge("attack", "move", outOfRange(2f)) // Flickers!
```

### ✅ Right: Use Hysteresis

```csharp
.AddEdge("move", "attack", withinRange(2f))
.AddEdge("attack", "move", outOfRange(3f)) // Stable
```

### ❌ Wrong: Forgetting Generic Type

```csharp
.WithEntry("move", new MoveAction()) // Missing <TAction>
```

### ✅ Right: Include Type Parameter

```csharp
.WithEntry<MoveAction>("move", new MoveAction())
```

### ❌ Wrong: No Exit Condition

```csharp
.AddEdge("attack", "attack", new AlwaysTrueCondition()) // Stuck!
```

### ✅ Right: Always Have Exit

```csharp
.AddEdge("attack", "move", new NotCondition(targetNear))
```

## Performance Tips

1. **Order conditions by frequency** - Most common first
2. **Cache components** in conditions instead of GetComponent every frame
3. **Use priority** to short-circuit evaluation
4. **Minimize complex calculations** in Evaluate()

## Next Steps

1. Read [README.md](README.md) for complete documentation
2. Check [BehaviorGraphExecutorTests.cs](../../../Tests/BehaviorSystem/Graph/BehaviorGraphExecutorTests.cs) for examples
3. Look at [EnemyBehaviorGraphProvider.cs](../../../Examples/Enemy/EnemyBehaviorGraphProvider.cs) for real usage

Happy graphing! 🎮
