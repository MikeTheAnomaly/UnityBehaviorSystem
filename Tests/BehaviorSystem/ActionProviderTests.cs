using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

    /// <summary>
    /// Tests for action providers (GoToActionProvider, JumpActionProvider).
    /// Verifies that providers create concrete actions that auto-subscribe to BehaviorLoop.
    /// </summary>
    public class ActionProviderTests
    {
        private GameObject _testGameObject;

        [SetUp]
        public void Setup()
        {
            _testGameObject = new GameObject("TestActor");
        }

        [TearDown]
        public void Teardown()
        {
            if (_testGameObject != null)
                Object.DestroyImmediate(_testGameObject);
        }

        [Test]
        public void GoToActionProvider_CreatesLerpActionByDefault()
        {
            // Arrange & Act
            _testGameObject.AddComponent<GoToActionProvider>();

            // Assert - lerp action should be created by default
            var lerpAction = _testGameObject.GetComponent<GoToLerpAction>();
            Assert.IsNotNull(lerpAction, "GoToLerpAction should be created by default");
        }


    [Test]
    public void GoToActionProvider_SetTarget_UpdatesAction()
    {
        // Arrange
        var actionProvider = _testGameObject.AddComponent<GoToActionProvider>();

            // Act
            var target1 = new Vector3(5, 0, 5);
            actionProvider.SetTarget(target1);

            // Assert
            Assert.AreEqual(target1, actionProvider.GetTarget());
        }
    }

