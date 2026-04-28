using NUnit.Framework;
using UnityEngine;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for type-safe NodeId struct.
    /// Verifies equality, creation, and type-based generation.
    /// </summary>
    public class NodeIdTests
    {
        [Test]
        public void Create_GeneratesUniqueId()
        {
            // Arrange & Act
            var id1 = NodeId.Create("move");
            var id2 = NodeId.Create("attack");

            // Assert
            Assert.AreNotEqual(id1, id2, "Different keys should generate different IDs");
        }

        [Test]
        public void Create_SameKey_GeneratesSameId()
        {
            // Arrange & Act
            var id1 = NodeId.Create("move");
            var id2 = NodeId.Create("move");

            // Assert
            Assert.AreEqual(id1, id2, "Same key should generate same ID");
        }

        [Test]
        public void FromType_GeneratesId()
        {
            // Arrange & Act
            var id1 = NodeId.FromType<MockAction>();
            var id2 = NodeId.FromType<MockAction>();

            // Assert
            Assert.AreEqual(id1, id2, "Same type should generate same ID");
        }

        [Test]
        public void FromType_DifferentTypes_DifferentIds()
        {
            // Arrange & Act
            var id1 = NodeId.FromType<MockAction>();
            var id2 = NodeId.FromType<MockDelayedAction>();

            // Assert
            Assert.AreNotEqual(id1, id2, "Different types should generate different IDs");
        }

        [Test]
        public void FromType_WithTypeObject_GeneratesId()
        {
            // Arrange & Act
            var id1 = NodeId.FromType(typeof(MockAction));
            var id2 = NodeId.FromType(typeof(MockAction));

            // Assert
            Assert.AreEqual(id1, id2, "Same type object should generate same ID");
        }

        [Test]
        public void FromType_GenericAndTypeObject_AreEqual()
        {
            // Arrange & Act
            var id1 = NodeId.FromType<MockAction>();
            var id2 = NodeId.FromType(typeof(MockAction));

            // Assert
            Assert.AreEqual(id1, id2, "Generic and Type object should generate same ID");
        }

        [Test]
        public void Equals_WithSameId_ReturnsTrue()
        {
            // Arrange
            var id1 = NodeId.Create("test");
            var id2 = NodeId.Create("test");

            // Act & Assert
            Assert.IsTrue(id1.Equals(id2), "IDs with same key should be equal");
            Assert.IsTrue(id1 == id2, "== operator should work");
        }

        [Test]
        public void Equals_WithDifferentId_ReturnsFalse()
        {
            // Arrange
            var id1 = NodeId.Create("test1");
            var id2 = NodeId.Create("test2");

            // Act & Assert
            Assert.IsFalse(id1.Equals(id2), "IDs with different keys should not be equal");
            Assert.IsTrue(id1 != id2, "!= operator should work");
        }

        [Test]
        public void GetHashCode_SameId_ReturnsSameHash()
        {
            // Arrange
            var id1 = NodeId.Create("test");
            var id2 = NodeId.Create("test");

            // Act & Assert
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode(), "Same IDs should have same hash code");
        }

        [Test]
        public void ToString_ReturnsReadableFormat()
        {
            // Arrange
            var id = NodeId.Create("move");

            // Act
            var str = id.ToString();

            // Assert
            Assert.IsNotNull(str, "ToString should return non-null");
            Assert.IsNotEmpty(str, "ToString should return non-empty string");
        }
    }
}
