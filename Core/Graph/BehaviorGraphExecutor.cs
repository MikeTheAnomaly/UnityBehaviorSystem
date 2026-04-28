using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using BehaviorSystem.Core.Logging;

/// <summary>
/// Executes behavior graphs by traversing nodes and following conditional edges.
/// 
/// Execution model:
/// 1. Execute current node's action
/// 2. Check transitions DURING execution (for ICancellableAction)
/// 3. Evaluate transitions after completion
/// 4. Transition to first valid edge's target node
/// 5. If no valid transition, stay on current node
/// 
/// Interruption support:
/// - If action implements ICancellableAction, checks transitions each frame
/// - Can interrupt mid-execution for reactive behavior
/// - Non-cancellable actions complete before transition check
/// 
/// This creates a reactive state machine where:
/// - Actions determine what behavior executes
/// - Conditions determine state transitions
/// - Priority determines transition precedence
/// - Interrupts enable immediate reactions
/// </summary>
public class BehaviorGraphExecutor
{
    private readonly BehaviorGraph _graph;
    private NodeId _currentNodeId;

    public BehaviorGraphExecutor(BehaviorGraph graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _currentNodeId = graph.EntryNodeId;
    }

    /// <summary>
    /// Executes one iteration of the behavior graph.
    /// 
    /// Steps:
    /// 1. Execute current node's action
    /// 2. Check for transitions during execution (if cancellable)
    /// 3. Evaluate transitions after completion
    /// 4. Move to next node or stay
    /// </summary>
    public IEnumerator ExecuteIteration(GameObject context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // Get and execute current node
        var currentNode = _graph.GetNode(_currentNodeId);
        if (currentNode == null)
        {
            BehaviorSystemLogger.LogError($"Current node {_currentNodeId} not found in graph");
            yield break;
        }

        // Check if action is cancellable and reset its state
        var cancellable = currentNode.Action as ICancellableAction;
        if (cancellable != null)
        {
            BehaviorSystemLogger.Log($"Resetting cancellable action: {currentNode.Action.GetType().Name}");
            cancellable.Reset(); // Clear cancellation state before execution
        }
        
        var actionEnumerator = currentNode.Execute(context);
        
        // Execute action with optional mid-execution interruption
        while (actionEnumerator.MoveNext())
        {
            yield return actionEnumerator.Current;
            
            // If action is cancellable, check for transitions each frame
            if (cancellable != null)
            {
                var validTransition = GetValidTransition(context);
                if (validTransition != null)
                {
                    // Interrupt action
                    BehaviorSystemLogger.Log($"Interrupting {currentNode.Action.GetType().Name}, transitioning to {validTransition.To}");
                    cancellable.Cancel();
                    _currentNodeId = validTransition.To;
                    yield break; // Exit immediately to new state
                }
            }
        }

        BehaviorSystemLogger.Log($"Action {currentNode.Action.GetType().Name} completed, checking transitions...");
        
        // Action completed (or was cancelled) - check transitions
        var transition = GetValidTransition(context);
        if (transition != null)
        {
            BehaviorSystemLogger.Log($"Transitioning from {_currentNodeId} to {transition.To}");
            _currentNodeId = transition.To;
        }
        else
        {
            BehaviorSystemLogger.Log($"No valid transition, staying on {_currentNodeId} and repeating if applicable");
            if(currentNode.Action is IRepeatableAction repeatable && repeatable.ShouldRepeat)
            {
                currentNode.Execute(context);
            }
            else
            {
                BehaviorSystemLogger.Log($"Not repeating action at {_currentNodeId}, execution will pause here.");
            }
        }
    }

    /// <summary>
    /// Gets the first valid transition from the current node.
    /// Returns null if no valid transitions exist.
    /// </summary>
    private BehaviorEdge GetValidTransition(GameObject context)
    {
        var outgoingEdges = _graph.GetOutgoingEdges(_currentNodeId);
        BehaviorSystemLogger.Log($"Checking {outgoingEdges.Count()} outgoing edges from {_currentNodeId}");
        
        foreach (var edge in outgoingEdges)
        {
            bool canTraverse = edge.CanTraverse(context);
            BehaviorSystemLogger.Log($"Edge {edge.From} -> {edge.To}: CanTraverse={canTraverse}, Condition={edge.Condition.GetType().Name}");
            
            if (canTraverse)
            {
                return edge;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the current node being executed.
    /// </summary>
    public BehaviorNode GetCurrentNode() => _graph.GetNode(_currentNodeId);

    /// <summary>
    /// Gets the current node ID.
    /// </summary>
    public NodeId GetCurrentNodeId() => _currentNodeId;

    /// <summary>

    /// Resets execution to the entry node.
    /// </summary>
    public void Reset() => _currentNodeId = _graph.EntryNodeId;
}
