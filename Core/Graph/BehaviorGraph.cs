using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Immutable behavior graph representing a state machine.
/// 
/// Features:
/// - Type-safe node references via NodeId
/// - Conditional edge transitions via ICondition
/// - Priority-based edge selection
/// - Validation on construction
/// 
/// Graph must have:
/// - At least one node
/// - Entry node must exist in node collection
/// - All edge references must point to valid nodes
/// </summary>
public class BehaviorGraph
{
    public NodeId EntryNodeId { get; }
    
    private readonly Dictionary<NodeId, BehaviorNode> _nodes;
    private readonly List<BehaviorEdge> _edges;

    public BehaviorGraph(NodeId entryNodeId, IEnumerable<BehaviorNode> nodes, IEnumerable<BehaviorEdge> edges)
    {
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (edges == null)
            throw new ArgumentNullException(nameof(edges));

        var nodeList = nodes.ToList();
        if (nodeList.Count == 0)
            throw new ArgumentException("Graph must contain at least one node", nameof(nodes));

        _nodes = nodeList.ToDictionary(n => n.Id);
        _edges = edges.ToList();
        EntryNodeId = entryNodeId;

        // Validate entry node exists
        if (!_nodes.ContainsKey(entryNodeId))
            throw new InvalidOperationException($"Entry node {entryNodeId} does not exist in graph");

        // Validate all edges reference valid nodes
        foreach (var edge in _edges)
        {
            if (!_nodes.ContainsKey(edge.From))
                throw new InvalidOperationException($"Edge references non-existent 'from' node: {edge.From}");
            if (!_nodes.ContainsKey(edge.To))
                throw new InvalidOperationException($"Edge references non-existent 'to' node: {edge.To}");
        }
    }

    /// <summary>
    /// Gets a node by its ID. Returns null if not found.
    /// </summary>
    public BehaviorNode GetNode(NodeId id)
    {
        _nodes.TryGetValue(id, out var node);
        return node;
    }

    /// <summary>
    /// Gets all outgoing edges from a node, ordered by priority (highest first).
    /// </summary>
    public IEnumerable<BehaviorEdge> GetOutgoingEdges(NodeId fromId)
    {
        return _edges
            .Where(e => e.From == fromId)
            .OrderByDescending(e => e.Priority);
    }

    /// <summary>
    /// Gets all nodes in the graph.
    /// </summary>
    public IEnumerable<BehaviorNode> GetAllNodes() => _nodes.Values;

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IEnumerable<BehaviorEdge> GetAllEdges() => _edges;
}
