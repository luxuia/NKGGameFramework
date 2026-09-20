using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NKGGameFramework
{
    /// <summary>
    /// .NET 6 PriorityQueue&lt;TElement,TPriority&gt; 的 netstandard2.1 backport（二叉堆，
    /// MIT 源自 dotnet/runtime，简化为仅覆盖框架内用到的 API 面：
    /// Enqueue / Dequeue / Peek / TryDequeue / TryPeek / Count / Clear）。
    /// 语义与 .NET 一致：同优先级出队顺序不保证稳定。
    /// </summary>
    internal sealed class PriorityQueue<TElement, TPriority>
    {
        private (TElement Element, TPriority Priority)[] _heap;
        private int _count;

        private static readonly IComparer<TPriority> s_comparer = Comparer<TPriority>.Default;

        public PriorityQueue()
        {
            _heap = new (TElement, TPriority)[4];
            _count = 0;
        }

        public int Count => _count;

        public void Clear()
        {
            // 清引用，帮助 GC；对无引用的类型无副作用。
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (_count == _heap.Length)
            {
                Grow();
            }

            SiftUp(_count++, (element, priority));
        }

        public TElement Peek()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            return _heap[0].Element;
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (_count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            element = _heap[0].Element;
            priority = _heap[0].Priority;
            return true;
        }

        public TElement Dequeue()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            var result = _heap[0].Element;
            RemoveRoot();
            return result;
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (_count == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            element = _heap[0].Element;
            priority = _heap[0].Priority;
            RemoveRoot();
            return true;
        }

        private void RemoveRoot()
        {
            int last = --_count;
            if (last > 0)
            {
                var moved = _heap[last];
                _heap[last] = default;
                SiftDown(0, moved);
            }
            else
            {
                _heap[0] = default;
            }
        }

        private void SiftUp(int index, (TElement Element, TPriority Priority) node)
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (s_comparer.Compare(node.Priority, _heap[parent].Priority) >= 0)
                {
                    break;
                }

                _heap[index] = _heap[parent];
                index = parent;
            }

            _heap[index] = node;
        }

        private void SiftDown(int index, (TElement Element, TPriority Priority) node)
        {
            int half = _count >> 1;
            while (index < half)
            {
                int best = (index << 1) + 1;
                int right = best + 1;

                if (right < _count && s_comparer.Compare(_heap[right].Priority, _heap[best].Priority) < 0)
                {
                    best = right;
                }

                if (s_comparer.Compare(node.Priority, _heap[best].Priority) <= 0)
                {
                    break;
                }

                _heap[index] = _heap[best];
                index = best;
            }

            _heap[index] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow()
        {
            var newSize = _heap.Length * 2;
            var newHeap = new (TElement, TPriority)[newSize];
            Array.Copy(_heap, newHeap, _count);
            _heap = newHeap;
        }
    }
}
