using System.Collections;
using UnityEngine;
using System.Reflection;

    /// <summary>
    /// Mock action for testing. Executes instantly and tracks execution.
    /// </summary>
    public class MockAction : IAction
    {
        public int ExecutionCount { get; private set; }
        public GameObject LastContext { get; private set; }
        public bool HasExecuted => ExecutionCount > 0;

        public IEnumerator Execute(GameObject context)
        {
            ExecutionCount++;
            LastContext = context;
            yield return null;
        }

        public void Reset()
        {
            ExecutionCount = 0;
            LastContext = null;
        }
    }

    /// <summary>
    /// Mock action that takes a specified number of frames to execute.
    /// </summary>
    public class MockDelayedAction : IAction
    {
        private readonly int _framesToWait;
        public int ExecutionCount { get; private set; }

        public MockDelayedAction(int framesToWait = 3)
        {
            _framesToWait = framesToWait;
        }

        public IEnumerator Execute(GameObject context)
        {
            ExecutionCount++;
            for (int i = 0; i < _framesToWait; i++)
                yield return null;
        }

        public void Reset()
        {
            ExecutionCount = 0;
        }
    }

    /// <summary>
    /// Mock action that can throw exceptions for testing error handling.
    /// </summary>
    public class MockFailingAction : IAction
    {
        private readonly string _errorMessage;
        public int ExecutionAttempts { get; private set; }

        public MockFailingAction(string errorMessage = "Mock action failed")
        {
            _errorMessage = errorMessage;
        }

        public IEnumerator Execute(GameObject context)
        {
            ExecutionAttempts++;
            throw new System.Exception(_errorMessage);
        }

        public void Reset()
        {
            ExecutionAttempts = 0;
        }
    }

    /// <summary>
    /// Mock action with priority for testing priority-based orchestration.
    /// </summary>
    public class MockPriorityAction : IAction, IPriority
    {
        public int Priority { get; set; }
        public int ExecutionCount { get; private set; }
        public int ExecutionOrder { get; private set; }

        private static int _globalExecutionOrder = 0;

        public MockPriorityAction(int priority = 0)
        {
            Priority = priority;
        }

        public IEnumerator Execute(GameObject context)
        {
            ExecutionCount++;
            ExecutionOrder = ++_globalExecutionOrder;
            yield return null;
        }

        public void Reset()
        {
            ExecutionCount = 0;
            ExecutionOrder = 0;
        }

        public static void ResetGlobalOrder()
        {
            _globalExecutionOrder = 0;
        }
    }

