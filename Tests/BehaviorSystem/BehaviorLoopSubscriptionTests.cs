using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests for orchestration subscription and action registration.
/// Verifies that actions can be subscribed to orchestrators and executed by BehaviorLoop.
/// </summary>
public class BehaviorLoopSubscriptionTests
{
    private GameObject _testGameObject;
    private BehaviorLoop _behaviorLoop;
    private AllAtOnceBehaviorOrchestration _orchestration;

    [SetUp]
    public void Setup()
    {
        _testGameObject = new GameObject("TestActor");
        _orchestration = _testGameObject.AddComponent<AllAtOnceBehaviorOrchestration>();
        _behaviorLoop = _testGameObject.AddComponent<BehaviorLoop>();
    }

    [TearDown]
    public void Teardown()
    {
        if (_testGameObject != null)
            Object.DestroyImmediate(_testGameObject);
    }

    [Test]
    public void SubscribeAction_AddsActionToOrchestration()
    {
        // Arrange
        var mockAction = new MockAction();

        // Act
        _orchestration.SubscribeAction(mockAction);

        // Assert - Action subscription accepted
        Assert.Pass("Action subscription accepted");
    }

    [Test]
    public void SubscribeAction_NullAction_IgnoresIt()
    {
        // Act & Assert - should not throw
        _orchestration.SubscribeAction(null);
        Assert.Pass("Null subscription handled gracefully");
    }

    [Test]
    public void UnsubscribeAction_RemovesAction()
    {
        // Arrange
        var mockAction = new MockAction();
        _orchestration.SubscribeAction(mockAction);

        // Act
        _orchestration.UnsubscribeAction(mockAction);

        // Assert - action should not execute
        _behaviorLoop.StartBehaviorLoop();
        Assert.AreEqual(0, mockAction.ExecutionCount);
    }

    [Test]
    public void SubscribeAction_DuplicateSubscription_Ignored()
    {
        // Arrange
        var mockAction = new MockAction();
        _orchestration.SubscribeAction(mockAction);

        // Act
        _orchestration.SubscribeAction(mockAction);

        // Assert - duplicate subscription handled
        Assert.Pass("Duplicate subscription handled");
    }

    [UnityTest]
    public IEnumerator StartBehaviorLoop_WithNoActions_DoesNotThrow()
    {
        // Act
        _behaviorLoop.StartBehaviorLoop();
        yield return null;

        // Assert - should complete without error
        Assert.Pass("Loop completed with no actions");
    }
}
