using NUnit.Framework;
using UnityEngine;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Tests for concrete condition implementations.
    /// </summary>
    public class ConditionTests
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
        public void AlwaysTrueCondition_AlwaysReturnsTrue()
        {
            // Arrange
            var condition = new AlwaysTrueCondition();

            // Act & Assert
            Assert.IsTrue(condition.Evaluate(_context), "Should always return true");
            Assert.IsTrue(condition.Evaluate(_context), "Should remain true on multiple evaluations");
        }

        [Test]
        public void AlwaysTrueCondition_NullContext_ReturnsTrue()
        {
            // Arrange
            var condition = new AlwaysTrueCondition();

            // Act & Assert
            Assert.IsTrue(condition.Evaluate(null), "Should return true even with null context");
        }

        [Test]
        public void TargetExistsCondition_WithTarget_ReturnsTrue()
        {
            // Arrange
            var holder = _context.AddComponent<MockTargetHolder>();
            holder.target = new GameObject("Target").transform;
            var condition = new TargetExistsCondition<MockTargetHolder>(h => h.target);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsTrue(result, "Should return true when target exists");

            // Cleanup
            Object.DestroyImmediate(holder.target.gameObject);
        }

        [Test]
        public void TargetExistsCondition_NoTarget_ReturnsFalse()
        {
            // Arrange
            var holder = _context.AddComponent<MockTargetHolder>();
            holder.target = null;
            var condition = new TargetExistsCondition<MockTargetHolder>(h => h.target);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsFalse(result, "Should return false when target is null");
        }

        [Test]
        public void TargetExistsCondition_NoComponent_ReturnsFalse()
        {
            // Arrange
            var condition = new TargetExistsCondition<MockTargetHolder>(h => h.target);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsFalse(result, "Should return false when component missing");
        }

        [Test]
        public void DistanceToTargetCondition_WithinRange_ReturnsTrue()
        {
            // Arrange
            var holder = _context.AddComponent<MockTargetHolder>();
            var target = new GameObject("Target");
            target.transform.position = _context.transform.position + Vector3.forward * 5f;
            holder.target = target.transform;
            
            var condition = new IsWithinDistanceToTargetCondition<MockTargetHolder>(h => h.target, 10f);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsTrue(result, "Should return true when target within range");

            // Cleanup
            Object.DestroyImmediate(target);
        }

        [Test]
        public void DistanceToTargetCondition_OutOfRange_ReturnsFalse()
        {
            // Arrange
            var holder = _context.AddComponent<MockTargetHolder>();
            var target = new GameObject("Target");
            target.transform.position = _context.transform.position + Vector3.forward * 20f;
            holder.target = target.transform;
            
            var condition = new IsWithinDistanceToTargetCondition<MockTargetHolder>(h => h.target, 10f);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsFalse(result, "Should return false when target out of range");

            // Cleanup
            Object.DestroyImmediate(target);
        }

        [Test]
        public void DistanceToTargetCondition_NoTarget_ReturnsFalse()
        {
            // Arrange
            var holder = _context.AddComponent<MockTargetHolder>();
            holder.target = null;
            var condition = new IsWithinDistanceToTargetCondition<MockTargetHolder>(h => h.target, 10f);

            // Act
            var result = condition.Evaluate(_context);

            // Assert
            Assert.IsFalse(result, "Should return false when target is null");
        }

        /// <summary>
        /// Mock component that holds a target reference for testing conditions.
        /// </summary>
        private class MockTargetHolder : MonoBehaviour
        {
            public Transform target;
        }
    }
}
