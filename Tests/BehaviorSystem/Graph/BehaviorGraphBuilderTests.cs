using NUnit.Framework;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorGraphBuilder fluent API.
    /// </summary>
    public class BehaviorGraphBuilderTests
    {
        [Test]
        public void Build_WithoutEntry_ThrowsException()
        {
            // Arrange
            var builder = new BehaviorGraphBuilder();

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void Build_WithEntryOnly_Succeeds()
        {
            // Arrange & Act
            var graph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction())
                .Build();

            // Assert
            Assert.IsNotNull(graph, "Graph should be created");
        }

        [Test]
        public void Build_WithNodesAndEdges_Succeeds()
        {
            // Arrange & Act
            var graph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction())
                .AddNode<MockAction>("attack", new MockAction())
                .AddEdge("move", "attack", new AlwaysTrueCondition())
                .Build();

            // Assert
            Assert.IsNotNull(graph, "Graph should be created");
        }

        [Test]
        public void AddNode_DuplicateKey_ThrowsException()
        {
            // Arrange
            var builder = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction());

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => 
                builder.AddNode<MockAction>("move", new MockAction()));
        }

        [Test]
        public void AddEdge_NonExistentFromNode_ThrowsException()
        {
            // Arrange
            var builder = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction());

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => 
                builder.AddEdge("nonexistent", "move", new AlwaysTrueCondition()));
        }

        [Test]
        public void AddEdge_NonExistentToNode_ThrowsException()
        {
            // Arrange
            var builder = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction());

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => 
                builder.AddEdge("move", "nonexistent", new AlwaysTrueCondition()));
        }

        [Test]
        public void AddEdges_CreatesMultipleEdges()
        {
            // Arrange & Act
            var graph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("idle", new MockAction())
                .AddNode<MockAction>("move", new MockAction())
                .AddNode<MockAction>("attack", new MockAction())
                .AddEdges("idle", 
                    ("move", new AlwaysTrueCondition()),
                    ("attack", new AlwaysTrueCondition()))
                .Build();

            // Assert
            var edges = graph.GetOutgoingEdges(NodeId.Create("idle"));
            Assert.AreEqual(2, System.Linq.Enumerable.Count(edges), "Should create 2 edges");
        }

        [Test]
        public void Clear_ResetsBuilder()
        {
            // Arrange
            var builder = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("move", new MockAction())
                .AddNode<MockAction>("attack", new MockAction())
                .Clear();

            // Act & Assert - should throw since builder is cleared
            Assert.Throws<System.InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void Build_PreservesEdgePriority()
        {
            // Arrange & Act
            var graph = new BehaviorGraphBuilder()
                .WithEntry<MockAction>("idle", new MockAction())
                .AddNode<MockAction>("move", new MockAction())
                .AddNode<MockAction>("attack", new MockAction())
                .AddEdge("idle", "move", new AlwaysTrueCondition(), priority: 1)
                .AddEdge("idle", "attack", new AlwaysTrueCondition(), priority: 2)
                .Build();

            // Assert - edges should be ordered by priority (highest first)
            var edges = System.Linq.Enumerable.ToList(graph.GetOutgoingEdges(NodeId.Create("idle")));
            Assert.AreEqual(2, edges.Count, "Should have 2 edges");
            Assert.AreEqual(NodeId.Create("attack"), edges[0].To, "Higher priority edge should be first");
            Assert.AreEqual(NodeId.Create("move"), edges[1].To, "Lower priority edge should be second");
        }
    }
}
