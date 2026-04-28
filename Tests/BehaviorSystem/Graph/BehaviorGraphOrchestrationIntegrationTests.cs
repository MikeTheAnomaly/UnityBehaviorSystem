using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BehaviorSystem.Tests.Graph
{
    /// <summary>
    /// Integration test verifying LSP compliance.
    /// BehaviorGraphOrchestration should work identically to AllAtOnce/PriorityQueue
    /// from the perspective of BaseUnityAction auto-subscription.
    /// </summary>
    public class BehaviorGraphOrchestrationIntegrationTests
    {
        [UnityTest]
        public IEnumerator BaseUnityAction_AutoSubscribes_ToGraphOrchestration()
        {
            // Arrange - Create gameobject with orchestration
            var go = new GameObject("TestActor");
            var orchestration = go.AddComponent<BehaviorGraphOrchestration>();
            
            // Create mock Unity action that auto-subscribes
            var mockActionComponent = go.AddComponent<MockUnityAction>();
            
            // Set entry node (since action auto-subscribed)
            orchestration.SetEntryNode<MockUnityAction>();

            // Act - Wait for Start to complete subscription
            yield return null;
            
            // Execute one iteration (use ExecuteIteration for tests, not ExecuteActions which loops forever)
            var enumerator = orchestration.ExecuteIteration(go);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            Assert.AreEqual(1, mockActionComponent.ExecutionCount, 
                "Action should auto-subscribe and execute via graph orchestration");

            // Cleanup
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Mock MonoBehaviour action for testing auto-subscription.
        /// </summary>
        private class MockUnityAction : BaseUnityAction
        {
            public int ExecutionCount { get; private set; }

            public override IEnumerator Execute(GameObject context)
            {
                ExecutionCount++;
                yield return null;
            }
        }
    }
}
