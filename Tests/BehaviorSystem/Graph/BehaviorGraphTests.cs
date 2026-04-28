using NUnit.Framework;
using System.Linq;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorGraph construction and validation.
    /// </summary>
    public class BehaviorGraphTests
    {
        [Test]
        public void Constructor_EmptyGraph_ThrowsException()
        {
            // Arrange
            var entryId = NodeId.Create("entry");

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => 
                new BehaviorGraph(entryId, new BehaviorNode[0], new BehaviorEdge[0]));
        }

        [Test]
        public void Constructor_NoEntryNode_ThrowsException()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var otherId = NodeId.Create("other");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(otherId, action).AsNonGeneric();

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => 
                new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]));
        }

        [Test]
        public void Constructor_ValidGraph_Succeeds()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(entryId, action).AsNonGeneric();

            // Act
            var graph = new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]);

            // Assert
            Assert.IsNotNull(graph, "Graph should be created");
            Assert.AreEqual(entryId, graph.EntryNodeId, "Graph should have entry node ID");
        }

        [Test]
        public void GetNode_ExistingId_ReturnsNode()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(entryId, action).AsNonGeneric();
            var graph = new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]);

            // Act
            var result = graph.GetNode(entryId);

            // Assert
            Assert.AreEqual(node, result, "Should return the correct node");
        }

        [Test]
        public void GetNode_NonExistingId_ReturnsNull()
        {
            // Arrange
            var entryId = NodeId.Create("entry");
            var otherId = NodeId.Create("other");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(entryId, action).AsNonGeneric();
            var graph = new BehaviorGraph(entryId, new[] { node }, new BehaviorEdge[0]);

            // Act
            var result = graph.GetNode(otherId);

            // Assert
            Assert.IsNull(result, "Should return null for non-existing ID");
        }

        [Test]
        public void GetOutgoingEdges_ReturnsFilteredEdges()
        {
            // Arrange
            var id1 = NodeId.Create("node1");
            var id2 = NodeId.Create("node2");
            var id3 = NodeId.Create("node3");
            
            var node1 = new BehaviorNode<MockAction>(id1, new MockAction()).AsNonGeneric();
            var node2 = new BehaviorNode<MockAction>(id2, new MockAction()).AsNonGeneric();
            var node3 = new BehaviorNode<MockAction>(id3, new MockAction()).AsNonGeneric();
            
            var edge1 = new BehaviorEdge(id1, id2, new AlwaysTrueCondition());
            var edge2 = new BehaviorEdge(id1, id3, new AlwaysTrueCondition());
            var edge3 = new BehaviorEdge(id2, id3, new AlwaysTrueCondition());
            
            var graph = new BehaviorGraph(id1, new[] { node1, node2, node3 }, new[] { edge1, edge2, edge3 });

            // Act
            var outgoing = graph.GetOutgoingEdges(id1).ToList();

            // Assert
            Assert.AreEqual(2, outgoing.Count, "Should return 2 outgoing edges from node1");
            Assert.Contains(edge1, outgoing, "Should contain edge to node2");
            Assert.Contains(edge2, outgoing, "Should contain edge to node3");
        }

        [Test]
        public void GetOutgoingEdges_NoEdges_ReturnsEmpty()
        {
            // Arrange
            var id1 = NodeId.Create("node1");
            var id2 = NodeId.Create("node2");
            
            var node1 = new BehaviorNode<MockAction>(id1, new MockAction()).AsNonGeneric();
            var node2 = new BehaviorNode<MockAction>(id2, new MockAction()).AsNonGeneric();
            
            var graph = new BehaviorGraph(id1, new[] { node1, node2 }, new BehaviorEdge[0]);

            // Act
            var outgoing = graph.GetOutgoingEdges(id2).ToList();

            // Assert
            Assert.AreEqual(0, outgoing.Count, "Should return empty for node with no outgoing edges");
        }
    }
}
