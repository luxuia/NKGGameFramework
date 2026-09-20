using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Nodes
{

    public sealed class NodeGraphEvents
    {
        public event Action<NodeGraphNodeEvent>? NodeAdded;
        public event Action<NodeGraphNodeEvent>? NodeRemoved;
        public event Action<NodeGraphPortLineEvent>? PortConnected;
        public event Action<NodeGraphPortLineEvent>? PortDisconnected;

        internal void Publish(NodeGraphNodeEvent eventArgs)
        {
            switch (eventArgs.Kind)
            {
                case NodeGraphEventKind.NodeAdded:
                    NodeAdded?.Invoke(eventArgs);
                    break;
                case NodeGraphEventKind.NodeRemoved:
                    NodeRemoved?.Invoke(eventArgs);
                    break;
            }
        }

        internal void Publish(NodeGraphPortLineEvent eventArgs)
        {
            switch (eventArgs.Kind)
            {
                case NodeGraphEventKind.PortConnected:
                    PortConnected?.Invoke(eventArgs);
                    break;
                case NodeGraphEventKind.PortDisconnected:
                    PortDisconnected?.Invoke(eventArgs);
                    break;
            }
        }
    }

    public readonly struct NodeGraphNodeEvent : IEquatable<NodeGraphNodeEvent>
    {
        public readonly NodeGraphEventKind Kind;
        public readonly Node Node;

        public NodeGraphNodeEvent(NodeGraphEventKind Kind, Node Node)
        {
            this.Kind = Kind;
            this.Node = Node;
        }

        public bool Equals(NodeGraphNodeEvent other) => EqualityComparer<NodeGraphEventKind>.Default.Equals(Kind, other.Kind) && EqualityComparer<Node>.Default.Equals(Node, other.Node);

        public override int GetHashCode() => HashCode.Combine(Kind, Node);

        public override bool Equals(object? obj) => obj is NodeGraphNodeEvent other && Equals(other);

        public static bool operator ==(NodeGraphNodeEvent left, NodeGraphNodeEvent right) => left.Equals(right);
        public static bool operator !=(NodeGraphNodeEvent left, NodeGraphNodeEvent right) => !left.Equals(right);

        public void Deconstruct(out NodeGraphEventKind kind, out Node node)
        {
            kind = Kind; node = Node;
        }
    }

    public readonly struct NodeGraphPortLineEvent : IEquatable<NodeGraphPortLineEvent>
    {
        public readonly NodeGraphEventKind Kind;
        public readonly NodePortLine PortLine;

        public NodeGraphPortLineEvent(NodeGraphEventKind Kind, NodePortLine PortLine)
        {
            this.Kind = Kind;
            this.PortLine = PortLine;
        }

        public bool Equals(NodeGraphPortLineEvent other) => EqualityComparer<NodeGraphEventKind>.Default.Equals(Kind, other.Kind) && EqualityComparer<NodePortLine>.Default.Equals(PortLine, other.PortLine);

        public override int GetHashCode() => HashCode.Combine(Kind, PortLine);

        public override bool Equals(object? obj) => obj is NodeGraphPortLineEvent other && Equals(other);

        public static bool operator ==(NodeGraphPortLineEvent left, NodeGraphPortLineEvent right) => left.Equals(right);
        public static bool operator !=(NodeGraphPortLineEvent left, NodeGraphPortLineEvent right) => !left.Equals(right);

        public void Deconstruct(out NodeGraphEventKind kind, out NodePortLine portLine)
        {
            kind = Kind; portLine = PortLine;
        }
    }

    public enum NodeGraphEventKind
    {
        NodeAdded,
        NodeRemoved,
        PortConnected,
        PortDisconnected,
    }
}
