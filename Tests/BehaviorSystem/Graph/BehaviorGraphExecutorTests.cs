using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorGraphExecutor traversal and transitions.
    /// </summary>
    public class BehaviorGraphExecutorTests
    {
        private GameObject _context;

        [SetUp]
        public void Setup()
        {
            _context = new GameObject("TestContext");
        }

        [TearDown]
        public void Teardown()
        {
            if (_context != null)
                Object.DestroyImmediate(_context);
        }

        [Test]
        public void Constructor_NullGraph_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new BehaviorGraphExecutor(null));
        }

        [Test]
        public void TryReset_TargetNodeExists_ResetsCurrentNode()
        {
            var graph = new BehaviorGraphBuilder()
                .WithEntry("entry", new MockAction())
                .AddNode("other", new MockAction())
                .AddEdge("entry", "other", new AlwaysTrueCondition())
                .AddEdge("other", "other", new AlwaysTrueCondition())
                .Build();

            var executor = new BehaviorGraphExecutor(graph);
            Assert.AreEqual(NodeId.Create("entry"), executor.GetCurrentNodeId());

            var result = executor.TryReset(NodeId.Create("other"));

            Assert.IsTrue(result);
            Assert.AreEqual(NodeId.Create("other"), executor.GetCurrentNodeId());
        }

        [Test]
        public void TryReset_TargetNodeMissing_DoesNotChangeCurrentNode()
        {
            var graph = new BehaviorGraphBuilder()
                .WithEntry("entry", new MockAction())
                .AddNode("other", new MockAction())
                .AddEdge("entry", "other", new AlwaysTrueCondition())
                .AddEdge("other", "other", new AlwaysTrueCondition())
                .Build();

            var executor = new BehaviorGraphExecutor(graph);
            var before = executor.GetCurrentNodeId();

            var result = executor.TryReset(NodeId.Create("missing"));

            Assert.IsFalse(result);
            Assert.AreEqual(before, executor.GetCurrentNodeId());
        }

        [UnityTest]
        public IEnumerator ExecuteIteration_SingleNode_ExecutesAction()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(entryId, action).AsNonGeneric();
            var graph = new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]);
            var executor = new BehaviorGraphExecutor(graph);

            // Act
            yield return executor.ExecuteIteration(_context);

            // Assert
            Assert.AreEqual(1, action.ExecutionCount, "Entry node action should execute");
        }

        [UnityTest]
        public IEnumerator ExecuteIteration_WithTransition_FollowsEdge()
        {
            // Arrange
            var id1 = NodeId.Create("node1");
            var id2 = NodeId.Create("node2");
            
            var action1 = new MockAction();
            var action2 = new MockAction();
            
            var node1 = new BehaviorNode<MockAction>(id1, action1).AsNonGeneric();
            var node2 = new BehaviorNode<MockAction>(id2, action2).AsNonGeneric();
            
            var edge = new BehaviorEdge(id1, id2, new AlwaysTrueCondition());
            
            var graph = new BehaviorGraph(id1, new[] { node1, node2 }, new[] { edge });
            var executor = new BehaviorGraphExecutor(graph);

            // Act - Execute twice to transition
            yield return executor.ExecuteIteration(_context);
            yield return executor.ExecuteIteration(_context);

            // Assert
            Assert.AreEqual(1, action1.ExecutionCount, "First action should execute once");
            Assert.AreEqual(1, action2.ExecutionCount, "Second action should execute after transition");
        }

        [UnityTest]
        public IEnumerator ExecuteIteration_ConditionalEdge_OnlyFollowsWhenTrue()
        {
            // Arrange
            var id1 = NodeId.Create("node1");
            var id2 = NodeId.Create("node2");
            
            var action1 = new MockAction();
            var action2 = new MockAction();
            
            var node1 = new BehaviorNode<MockAction>(id1, action1).AsNonGeneric();
            var node2 = new BehaviorNode<MockAction>(id2, action2).AsNonGeneric();
            
            var condition = new MockCondition(false); // Starts false
            var edge = new BehaviorEdge(id1, id2, condition);
            
            var graph = new BehaviorGraph(id1, new[] { node1, node2 }, new[] { edge });
            var executor = new BehaviorGraphExecutor(graph);

            // Act - Execute twice, condition is false
            yield return executor.ExecuteIteration(_context);
            yield return executor.ExecuteIteration(_context);

            // Assert - Should stay on node1
            Assert.AreEqual(2, action1.ExecutionCount, "Should repeat node1 when transition fails");
            Assert.AreEqual(0, action2.ExecutionCount, "Should not reach node2 when condition is false");

            // Act - Enable condition and execute
            condition.SetResult(true);
            yield return executor.ExecuteIteration(_context);
            yield return executor.ExecuteIteration(_context);

            // Assert - Should transition to node2
            // Note: node1 executes once more (count=3) before transition, then node2 executes
            Assert.AreEqual(3, action1.ExecutionCount, "Node1 executes one final time before transitioning");
            Assert.AreEqual(1, action2.ExecutionCount, "Should reach node2 after condition becomes true");
        }

        [UnityTest]
        public IEnumerator ExecuteIteration_MultipleEdges_ChoosesHighestPriority()
        {
            // Arrange
            var id1 = NodeId.Create("node1");
            var id2 = NodeId.Create("node2");
            var id3 = NodeId.Create("node3");
            
            var action1 = new MockAction();
            var action2 = new MockAction();
            var action3 = new MockAction();
            
            var node1 = new BehaviorNode<MockAction>(id1, action1).AsNonGeneric();
            var node2 = new BehaviorNode<MockAction>(id2, action2).AsNonGeneric();
            var node3 = new BehaviorNode<MockAction>(id3, action3).AsNonGeneric();
            
            // Both edges are always true, but edge to node3 has higher priority
            var edge1 = new BehaviorEdge(id1, id2, new AlwaysTrueCondition(), priority: 1);
            var edge2 = new BehaviorEdge(id1, id3, new AlwaysTrueCondition(), priority: 2);
            
            var graph = new BehaviorGraph(id1, new[] { node1, node2, node3 }, new[] { edge1, edge2 });
            var executor = new BehaviorGraphExecutor(graph);

            // Act
            yield return executor.ExecuteIteration(_context);
            yield return executor.ExecuteIteration(_context);

            // Assert - Should transition to higher priority node3
            Assert.AreEqual(0, action2.ExecutionCount, "Should not execute lower priority node");
            Assert.AreEqual(1, action3.ExecutionCount, "Should execute higher priority node");
        }

        [Test]
        public void GetCurrentNode_ReturnsEntryNode()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(entryId, action).AsNonGeneric();
            var graph = new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]);
            var executor = new BehaviorGraphExecutor(graph);

            // Act
            var currentNode = executor.GetCurrentNode();

            // Assert
            Assert.AreEqual(node, currentNode, "Should start at entry node");
        }

        /// <summary>
        /// Mock condition for testing conditional transitions.
        /// </summary>
        private class MockCondition : ICondition
        {
            private bool _result;

            public MockCondition(bool result)
            {
                _result = result;
            }

            public void SetResult(bool result) => _result = result;

            public bool Evaluate(GameObject context) => _result;
        }
    }
}
