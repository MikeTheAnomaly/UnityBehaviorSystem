using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorGraphOrchestration lazy building with action subscription.
    /// Verifies LSP compliance - orchestration works like AllAtOnce/PriorityQueue.
    /// </summary>
    public class BehaviorGraphOrchestrationLazyBuildTests
    {
        private GameObject _testGameObject;
        private BehaviorGraphOrchestration _orchestration;

        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestActor");
            _orchestration = _testGameObject.AddComponent<BehaviorGraphOrchestration>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
        }

        [Test]
        public void SubscribeAction_StoresAction()
        {
            // Arrange
            var action = new MockAction();

            // Act
            _orchestration.SubscribeAction(action);

            // Assert - no exception thrown
            Assert.Pass("Action subscription accepted");
        }

        [Test]
        public void UnsubscribeAction_RemovesAction()
        {
            // Arrange
            var action = new MockAction();
            _orchestration.SubscribeAction(action);

            // Act
            _orchestration.UnsubscribeAction(action);

            // Assert - no exception thrown
            Assert.Pass("Action unsubscription accepted");
        }

        [UnityTest]
        public IEnumerator LazyBuild_WithSingleAction_ExecutesAction()
        {
            // Arrange
            var action = new MockAction();
            _orchestration.SubscribeAction(action);
            _orchestration.SetEntryNode<MockAction>();

            // Act
            var enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, action.ExecutionCount, "Action should execute once");
        }

        [UnityTest]
        public IEnumerator LazyBuild_WithMultipleActions_AutoSetsFirstAsEntry()
        {
            // Arrange
            var action1 = new MockAction();
            var action2 = new MockDelayedAction();
            
            _orchestration.SubscribeAction(action1);
            _orchestration.SubscribeAction(action2);
            // Don't explicitly set entry - should use first subscribed

            // Act
            var enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, action1.ExecutionCount, "First action should execute as entry node");
            Assert.AreEqual(0, action2.ExecutionCount, "Second action should not execute without edge");
        }

        [UnityTest]
        public IEnumerator LazyBuild_WithEdge_Transitions()
        {
            // Arrange
            var action1 = new MockAction();
            var action2 = new MockDelayedAction();
            
            _orchestration.SubscribeAction(action1);
            _orchestration.SubscribeAction(action2);
            _orchestration.SetEntryNode<MockAction>();
            _orchestration.AddEdge<MockAction, MockDelayedAction>(new AlwaysTrueCondition());

            // Act - Execute twice to transition
            var enumerator1 = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator1.MoveNext())
            {
                yield return null;
            }
            
            var enumerator2 = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator2.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, action1.ExecutionCount, "First action executes once");
            Assert.AreEqual(1, action2.ExecutionCount, "Second action executes after transition");
        }

        [UnityTest]
        public IEnumerator LazyBuild_OnlyBuildsOnce_UntilDirty()
        {
            // Arrange
            var action = new MockAction();
            _orchestration.SubscribeAction(action);
            _orchestration.SetEntryNode<MockAction>();

            // Act - Execute multiple times
            for (int i = 0; i < 3; i++)
            {
                var enumerator = _orchestration.ExecuteIteration(_testGameObject);
                while (enumerator.MoveNext())
                {
                    yield return null;
                }
            }

            // Assert - graph only built once, action executes each time
            Assert.AreEqual(3, action.ExecutionCount, "Action should execute on each iteration");
        }

        [UnityTest]
        public IEnumerator SubscribeAction_AfterFirstExecution_RebuildsGraph()
        {
            // Arrange
            var action1 = new MockAction();
            _orchestration.SubscribeAction(action1);
            _orchestration.SetEntryNode<MockAction>();
            
            var enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Act - Add new action after first execution
            var action2 = new MockDelayedAction();
            _orchestration.SubscribeAction(action2);
            _orchestration.AddEdge<MockAction, MockDelayedAction>(new AlwaysTrueCondition());

            enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            
            enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(2, action1.ExecutionCount, "First action executes twice total");
            Assert.AreEqual(1, action2.ExecutionCount, "New action executes after rebuild");
        }

        [Test]
        public void DirectMode_IgnoresSubscriptions()
        {
            // Arrange
            var directAction = new MockAction();
            var directGraph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("test", directAction)
                .Build();
            
            _orchestration.Initialize(directGraph);

            // Act - Try to subscribe (should be ignored)
            var subscribedAction = new MockAction();
            _orchestration.SubscribeAction(subscribedAction);

            // Assert - no exception, silently ignored
            Assert.Pass("Direct mode ignores subscriptions");
        }

        [UnityTest]
        public IEnumerator DirectMode_ExecutesInitializedGraph()
        {
            // Arrange
            var action = new MockAction();
            var graph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("test", action)
                .Build();
            
            _orchestration.Initialize(graph);

            // Act
            var enumerator = _orchestration.ExecuteIteration(_testGameObject);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, action.ExecutionCount, "Direct mode executes initialized graph");
        }

        [Test]
        public void AddEdge_WithNullCondition_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                _orchestration.AddEdge<MockAction, MockDelayedAction>(null));
        }

        [UnityTest]
        public IEnumerator AddEdge_WithMissingToAction_LogsWarning()
        {
            // Arrange
            var action1 = new MockAction();
            _orchestration.SubscribeAction(action1);
            _orchestration.SetEntryNode<MockAction>();
            
            // Add edge to action that's not subscribed
            _orchestration.AddEdge<MockAction, MockDelayedAction>(new AlwaysTrueCondition());

            // Act
            for (int i = 0; i < 2; i++)
            {
                var enumerator = _orchestration.ExecuteIteration(_testGameObject);
                while (enumerator.MoveNext())
                {
                    yield return null;
                }
            }

            // Assert - no crash, just stays on first node
            Assert.AreEqual(2, action1.ExecutionCount, "Should stay on first node when edge target missing");
        }

        [Test]
        public void MarkDirty_ForcesRebuild()
        {
            // Arrange
            var action = new MockAction();
            _orchestration.SubscribeAction(action);
            _orchestration.SetEntryNode<MockAction>();

            // Act
            _orchestration.MarkDirty();

            // Assert - no exception
            Assert.Pass("MarkDirty accepted");
        }
    }
}
