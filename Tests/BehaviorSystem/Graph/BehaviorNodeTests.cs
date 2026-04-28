using NUnit.Framework;
using UnityEngine;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for BehaviorNode construction and ID assignment.
    /// </summary>
    public class BehaviorNodeTests
    {
        [Test]
        public void Constructor_AssignsIdAndAction()
        {
            // Arrange
            var id = NodeId.Create("test");
            var action = new MockAction();

            // Act
            var node = new BehaviorNode<MockAction>(id, action);

            // Assert
            Assert.AreEqual(id, node.Id, "Node should have assigned ID");
            Assert.AreEqual(action, node.Action, "Node should have assigned action");
        }

        [Test]
        public void Constructor_NullAction_ThrowsException()
        {
            // Arrange
            var id = NodeId.Create("test");

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new BehaviorNode<MockAction>(id, null));
        }

        [Test]
        public void AsNonGeneric_ReturnsWrappedNode()
        {
            // Arrange
            var id = NodeId.Create("test");
            var action = new MockAction();
            var typedNode = new BehaviorNode<MockAction>(id, action);

            // Act
            var node = typedNode.AsNonGeneric();

            // Assert
            Assert.IsNotNull(node, "AsNonGeneric should return non-null");
            Assert.AreEqual(id, node.Id, "Non-generic wrapper should preserve ID");
            Assert.AreEqual(action, node.Action, "Non-generic wrapper should preserve action");
        }

        [Test]
        public void BehaviorNode_ExecutesAction()
        {
            // Arrange
            var id = NodeId.Create("test");
            var action = new MockAction();
            var node = new BehaviorNode<MockAction>(id, action);
            var context = new GameObject("TestContext");

            // Act
            var enumerator = node.Execute(context);
            while (enumerator.MoveNext()) { }

            // Assert
            Assert.AreEqual(1, action.ExecutionCount, "Action should execute once");
            Assert.AreEqual(context, action.LastContext, "Action should receive context");

            // Cleanup
            Object.DestroyImmediate(context);
        }
    }
}
