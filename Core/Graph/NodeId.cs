using System;

/// <summary>
/// Type-safe identifier for behavior graph nodes.
/// 
/// Prevents runtime errors from string typos by providing:
/// - Compile-time checking via readonly struct
/// - Factory methods for explicit creation
/// - Type-based generation for stronger coupling
/// 
/// Usage:
/// - NodeId.Create("move") for explicit keys
/// - NodeId.FromType&lt;MoveAction&gt;() for type-based keys
/// </summary>
public readonly struct NodeId : IEquatable<NodeId>
{
    private readonly string _key;

    private NodeId(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("NodeId key cannot be null or empty", nameof(key));
        
        _key = key;
    }

    /// <summary>
    /// Creates a NodeId from an explicit key string.
    /// </summary>
    public static NodeId Create(string key) => new NodeId(key);

    /// <summary>
    /// Creates a NodeId from a type name.
    /// Useful for strongly coupling nodes to their action types.
    /// </summary>
    public static NodeId FromType<T>() => new NodeId(typeof(T).FullName);

    /// <summary>
    /// Creates a NodeId from a Type object.
    /// Used for runtime type-based node creation.
    /// </summary>
    public static NodeId FromType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        return new NodeId(type.FullName);
    }

    public bool Equals(NodeId other) => _key == other._key;

    public override bool Equals(object obj) => obj is NodeId other && Equals(other);

    public override int GetHashCode() => _key?.GetHashCode() ?? 0;

    public static bool operator ==(NodeId left, NodeId right) => left.Equals(right);

    public static bool operator !=(NodeId left, NodeId right) => !left.Equals(right);

    public override string ToString() => $"NodeId({_key})";
}
