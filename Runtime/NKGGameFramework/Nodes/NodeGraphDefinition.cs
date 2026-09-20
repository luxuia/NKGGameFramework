using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Nodes
{

    public sealed class NodeGraphDefinition
    {
        public string? EntryNodeId { get; init; }

        public List<NodeDefinition> Nodes { get; init; } = new();

        public List<NodeLinkDefinition> Links { get; init; } = new();

        public NodeGraphValidationResult Validate()
        {
            return NodeGraphValidator.Validate(this);
        }

        public bool TryValidate(out NodeGraphValidationResult result)
        {
            result = Validate();
            return result.IsValid;
        }

        public NodeGraphIndex CreateIndex()
        {
            var validation = Validate();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.ToString());
            }

            return new NodeGraphIndex(this);
        }
    }

    public sealed class NodeDefinition
    {
        public string Id { get; init; }

        public string Type { get; init; } = NodeTypes.Default;

        public string? Name { get; init; }

        public NodePosition Position { get; init; }

        public Dictionary<string, string> Parameters { get; init; } = new();

        public List<NodePortDefinition> Ports { get; init; } = new();

        public IEnumerable<NodePortDefinition> Inputs => Ports.Where(static port => port.Direction == NodePortDirection.Input);

        public IEnumerable<NodePortDefinition> Outputs => Ports.Where(static port => port.Direction == NodePortDirection.Output);
    }

    public readonly struct NodePosition : IEquatable<NodePosition>
    {
        public readonly float X;
        public readonly float Y;

        public NodePosition(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public bool Equals(NodePosition other) => EqualityComparer<float>.Default.Equals(X, other.X) && EqualityComparer<float>.Default.Equals(Y, other.Y);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override bool Equals(object? obj) => obj is NodePosition other && Equals(other);

        public static bool operator ==(NodePosition left, NodePosition right) => left.Equals(right);
        public static bool operator !=(NodePosition left, NodePosition right) => !left.Equals(right);

        public void Deconstruct(out float x, out float y)
        {
            x = X; y = Y;
        }
    }

    public sealed class NodePortDefinition
    {
        public const int UnlimitedConnections = -1;

        public string Id { get; init; }

        public string? Name { get; init; }

        public NodePortDirection Direction { get; init; }

        public string? ValueType { get; init; }

        public int MaxConnections { get; init; } = UnlimitedConnections;

        public static NodePortDefinition Input(
            string id,
            string? name = null,
            string? valueType = null,
            int maxConnections = 1)
        {
            return new NodePortDefinition
            {
                Id = id,
                Name = name,
                Direction = NodePortDirection.Input,
                ValueType = valueType,
                MaxConnections = maxConnections,
            };
        }

        public static NodePortDefinition Output(
            string id,
            string? name = null,
            string? valueType = null,
            int maxConnections = UnlimitedConnections)
        {
            return new NodePortDefinition
            {
                Id = id,
                Name = name,
                Direction = NodePortDirection.Output,
                ValueType = valueType,
                MaxConnections = maxConnections,
            };
        }
    }

    public enum NodePortDirection
    {
        Input,
        Output,
    }

    public sealed class NodeLinkDefinition
    {
        public string Id { get; init; } = string.Empty;

        public string FromNodeId { get; init; }

        public string FromPortId { get; init; }

        public string ToNodeId { get; init; }

        public string ToPortId { get; init; }
    }

    public static class NodeTypes
    {
        public const string Default = "node";
        public const string Entry = "entry";
        public const string Action = "action";
        public const string Composite = "composite";
        public const string Condition = "condition";
    }
}
