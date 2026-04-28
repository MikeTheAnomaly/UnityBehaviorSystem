using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorGraphExecutor's interruption support for ICancellableAction.
    /// </summary>
    public class BehaviorGraphExecutorInterruptionTests
    {
    private GameObject _testContext;

    [SetUp]
    public void Setup()
    {
        _testContext = new GameObject("TestContext");
    }

    [TearDown]
    public void Teardown()
    {
        if (_testContext != null)
        {
            Object.DestroyImmediate(_testContext);
        }
    }

    /// <summary>
    /// Tests that a cancellable action can be interrupted mid-execution when a transition becomes valid.
    /// </summary>
    [UnityTest]
    public IEnumerator InterruptibleAction_InterruptedMidExecution_WhenTransitionBecomesValid()
    {
        // Arrange
        var moveAction = new TestCancellableAction(frameCount: 5);
        var attackAction = new TestAction();
        var toggleCondition = new ToggleCondition(initialValue: false);
        
        var graph = new BehaviorGraphBuilder()
            .WithEntry("move", moveAction)
            .AddNode("attack", attackAction)
            .AddEdge("move", "attack", toggleCondition)
            .Build();
        
        var executor = new BehaviorGraphExecutor(graph);

        // Act - Start executing move action (runs for 5 frames)
        var enumerator = executor.ExecuteIteration(_testContext);
        
        // Execute 2 frames
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, moveAction.ExecutionCount);
        yield return null;
        
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, moveAction.ExecutionCount);
        yield return null;
        
        // Enable transition condition mid-execution
        toggleCondition.SetValue(true);
        
        // Next frame should interrupt
        Assert.IsTrue(enumerator.MoveNext());
        
        // Assert - Action was cancelled
        Assert.IsTrue(moveAction.WasCancelled, "Action should have been cancelled");
        Assert.AreEqual(3, moveAction.ExecutionCount, "Should execute 3 frames before interruption");
        
        // Execute next iteration - should be on attack action
        enumerator = executor.ExecuteIteration(_testContext);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(attackAction.WasExecuted, "Should transition to attack action");
    }

    /// <summary>
    /// Tests that a non-cancellable action completes fully before checking transitions.
    /// </summary>
    [UnityTest]
    public IEnumerator NonCancellableAction_CompletesBeforeTransition()
    {
        // Arrange
        var moveAction = new TestLongAction(frameCount: 5);
        var attackAction = new TestAction();
        var toggleCondition = new ToggleCondition(initialValue: false);
        
        var graph = new BehaviorGraphBuilder()
            .WithEntry("move", moveAction)
            .AddNode("attack", attackAction)
            .AddEdge("move", "attack", toggleCondition)
            .Build();
        
        var executor = new BehaviorGraphExecutor(graph);

        // Act - Start executing move action
        var enumerator = executor.ExecuteIteration(_testContext);
        
        // Execute 2 frames
        Assert.IsTrue(enumerator.MoveNext());
        yield return null;
        Assert.IsTrue(enumerator.MoveNext());
        yield return null;
        
        // Enable transition condition mid-execution
        toggleCondition.SetValue(true);
        
        // Continue executing - should NOT interrupt
        Assert.IsTrue(enumerator.MoveNext());
        yield return null;
        Assert.IsTrue(enumerator.MoveNext());
        yield return null;
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext()); // Completes
        
        // Assert - Action completed fully (5 frames)
        Assert.AreEqual(5, moveAction.ExecutionCount, "Non-cancellable action should complete all frames");
        
        // Execute next iteration - should NOW transition to attack
        enumerator = executor.ExecuteIteration(_testContext);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(attackAction.WasExecuted, "Should transition after action completes");
    }

    /// <summary>
    /// Tests that cancellation flag is reset when action executes again.
    /// </summary>
    [UnityTest]
    public IEnumerator CancellableAction_ResetsFlag_OnNextExecution()
    {
        // Arrange - Simple graph with just move action, no transitions
        var moveAction = new TestCancellableAction(frameCount: 3);
        
        var graph = new BehaviorGraphBuilder()
            .WithEntry("move", moveAction)
            .Build();
        
        var executor = new BehaviorGraphExecutor(graph);

        // Act - Execute move normally first time
        Debug.Log("=== FIRST EXECUTION - Full 3 frames ===");
        var enumerator = executor.ExecuteIteration(_testContext);
        
        int firstRunFrames = 0;
        while (enumerator.MoveNext())
        {
            firstRunFrames++;
            yield return null;
        }
        
        Assert.AreEqual(3, firstRunFrames, "First run should complete 3 frames");
        
        // Execute again - should also complete 3 frames
        Debug.Log("=== SECOND EXECUTION - Should also do full 3 frames ===");
        enumerator = executor.ExecuteIteration(_testContext);
        
        int secondRunFrames = 0;
        while (enumerator.MoveNext())
        {
            secondRunFrames++;
            yield return null;
        }
        
        Assert.AreEqual(3, secondRunFrames, "Second run should complete 3 frames");
    }

    /// <summary>
    /// Tests that an interrupted action can be re-executed after transitioning back.
    /// </summary>
    [UnityTest]
    public IEnumerator InterruptedAction_CanReExecute_AfterTransitionBack()
    {
        // Arrange - Graph: move <-> idle
        var moveAction = new TestCancellableAction(frameCount: 3);
        var idleAction = new TestAction();
        var toggleCondition = new ToggleCondition(initialValue: false);
        
        var graph = new BehaviorGraphBuilder()
            .WithEntry("move", moveAction)
            .AddNode("idle", idleAction)
            .AddEdge("move", "idle", toggleCondition)
            .AddEdge("idle", "move", new AlwaysTrueCondition())
            .Build();
        
        var executor = new BehaviorGraphExecutor(graph);

        // First execution - interrupt move after 1 frame
        Debug.Log("=== INTERRUPT MOVE ===");
        var enumerator = executor.ExecuteIteration(_testContext);
        
        // Execute first frame
        Assert.IsTrue(enumerator.MoveNext()); // Frame 1 executes, yields
        Assert.AreEqual(1, moveAction.ExecutionCount);
        
        // Set condition to trigger interruption on next loop iteration
        toggleCondition.SetValue(true);
        yield return null;
        
        // Next MoveNext will resume executor, which checks condition and interrupts
        // The action doesn't execute another frame - it's interrupted immediately
        Assert.IsFalse(enumerator.MoveNext()); // Should return false (yield break was called)
        
        // Now check that interruption happened
        Assert.IsTrue(moveAction.WasCancelled, "Move should have been interrupted");
        Assert.AreEqual(1, moveAction.ExecutionCount, "Should only execute 1 frame before interruption");
        
        // Execute idle - should transition back to move automatically
        Debug.Log("=== EXECUTE IDLE ===");
        enumerator = executor.ExecuteIteration(_testContext);
        while (enumerator.MoveNext()) yield return null;
        
        // Disable interrupt condition
        toggleCondition.SetValue(false);
        
        // Execute move again - should complete all 3 frames
        Debug.Log("=== RE-EXECUTE MOVE ===");
        enumerator = executor.ExecuteIteration(_testContext);
        
        int frames = 0;
        while (enumerator.MoveNext())
        {
            frames++;
            Debug.Log($"Re-execution frame {frames}: ExecutionCount={moveAction.ExecutionCount}, IsCancelled={moveAction.IsCancelled}");
            yield return null;
        }
        
        Debug.Log($"=== FINAL: frames={frames} ===");
        Assert.AreEqual(3, frames, "Should complete all 3 frames on re-execution");
    }

    /// <summary>
    /// Tests that cancellable action transitions immediately on cancellation.
    /// </summary>
    [UnityTest]
    public IEnumerator CancellableAction_TransitionsImmediately_OnCancellation()
    {
        // Arrange
        var moveAction = new TestCancellableAction(frameCount: 10);
        var attackAction = new TestAction();
        var condition = new AlwaysTrueCondition(); // Always valid
        
        var graph = new BehaviorGraphBuilder()
            .WithEntry("move", moveAction)
            .AddNode("attack", attackAction)
            .AddEdge("move", "attack", condition)
            .Build();
        
        var executor = new BehaviorGraphExecutor(graph);

        // Act - Execute one frame
        var enumerator = executor.ExecuteIteration(_testContext);
        Assert.IsTrue(enumerator.MoveNext());
        yield return null;
        
        // Assert - Should interrupt after just 1 frame (condition always true)
        Assert.IsTrue(moveAction.WasCancelled, "Should cancel immediately when condition is valid");
        Assert.AreEqual(1, moveAction.ExecutionCount, "Should only execute 1 frame before interruption");
        
        // Verify transition happened
        enumerator = executor.ExecuteIteration(_testContext);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(attackAction.WasExecuted, "Should transition immediately after cancellation");
    }

    #region Test Actions

    /// <summary>
    /// Test action that implements ICancellableAction and runs for N frames.
    /// </summary>
    private class TestCancellableAction : ICancellableAction
    {
        private readonly int _frameCount;
        private bool _cancelled;

        public int ExecutionCount { get; private set; }
        public bool WasCancelled { get; private set; }
        public bool IsCancelled => _cancelled;

        public TestCancellableAction(int frameCount)
        {
            _frameCount = frameCount;
        }

        public void Reset()
        {
            Debug.Log($"[TestCancellableAction] Reset() called - clearing _cancelled flag");
            _cancelled = false;
        }

        public void Cancel()
        {
            Debug.Log($"[TestCancellableAction] Cancel() called - setting _cancelled=true");
            _cancelled = true;
            WasCancelled = true;
        }

        public void ResetState()
        {
            ExecutionCount = 0;
            WasCancelled = false;
        }

        public IEnumerator Execute(GameObject context)
        {
            ExecutionCount = 0;
            Debug.Log($"[TestCancellableAction] Execute() starting - ExecutionCount=0, _cancelled={_cancelled}, frameCount={_frameCount}");
            
            for (int i = 0; i < _frameCount && !_cancelled; i++)
            {
                ExecutionCount++;
                Debug.Log($"[TestCancellableAction] Frame {i}: ExecutionCount={ExecutionCount}, _cancelled={_cancelled}");
                yield return null;
            }
            
            Debug.Log($"[TestCancellableAction] Execute() completed - ExecutionCount={ExecutionCount}, _cancelled={_cancelled}");
        }
    }

    /// <summary>
    /// Test action that runs for N frames but is NOT cancellable.
    /// </summary>
    private class TestLongAction : IAction
    {
        private readonly int _frameCount;

        public int ExecutionCount { get; private set; }

        public TestLongAction(int frameCount)
        {
            _frameCount = frameCount;
        }

        public IEnumerator Execute(GameObject context)
        {
            ExecutionCount = 0;
            
            for (int i = 0; i < _frameCount; i++)
            {
                ExecutionCount++;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Simple test action that executes in one frame.
    /// </summary>
    private class TestAction : IAction
    {
        public bool WasExecuted { get; private set; }

        public IEnumerator Execute(GameObject context)
        {
            WasExecuted = true;
            yield return null;
        }
    }

    /// <summary>
    /// Condition that can be toggled for testing.
    /// </summary>
    private class ToggleCondition : ICondition
    {
        private bool _value;

        public ToggleCondition(bool initialValue)
        {
            _value = initialValue;
        }

        public void SetValue(bool value)
        {
            _value = value;
        }

        public bool Evaluate(GameObject context)
        {
            return _value;
        }
    }

    #endregion
    }
}
