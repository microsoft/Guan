// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Guan.Common
{
    /// <summary>
    /// MinHeap implementation.
    /// The operations is NOT thread safe and it is up to the caller
    /// to synchronize access to the heap.
    /// </summary>
    /// <typeparam name="T">The type stored in the heap</typeparam>
    public class MinHeap<T> : IEnumerable<T>
    {
        private List<T> m_data;
        private IComparer<T> m_comparer;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MinHeap()
        {
            m_data = new List<T>();
            m_comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Capacity of the heap</param>
        public MinHeap(int capacity)
            : this(capacity, Comparer<T>.Default)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Capacity of the heap</param>
        /// <param name="comparer">Comparer for the heap.</param>
        public MinHeap(int capacity, IComparer<T> comparer)
        {
            m_data = new List<T>(capacity);
            m_comparer = comparer;
        }

        /// <summary>
        /// Whether the heap is empty
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return (m_data.Count == 0);
            }
        }

        /// <summary>
        /// The number of elements in the heap
        /// </summary>
        public int Count
        {
            get
            {
                return m_data.Count;
            }
        }

        /// <summary>
        /// Capacity of the heap.
        /// </summary>
        public int Capacity
        {
            get
            {
                return m_data.Capacity;
            }
        }

        private void Swap(int i, int j)
        {
            T tmp = m_data[i];
            m_data[i] = m_data[j];
            m_data[j] = tmp;
        }

        /// <summary>
        /// The min item stored in the heap.
        /// </summary>
        public T Top
        {
            get
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException("heap is empty");
                }

                return m_data[0];
            }
        }

        /// <summary>
        /// Add an item to the heap.
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            int index = m_data.Count;
            int parentIndex;

            m_data.Add(item);
            while ((index > 0) &&
                   (m_comparer.Compare(item, m_data[(parentIndex = (index - 1) / 2)]) < 0))
            {
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        /// <summary>
        /// Extract the min item from the heap.
        /// </summary>
        /// <returns>The min item in the heap.</returns>
        public T Extract()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("heap is empty");
            }

            T result = m_data[0];
            T item = m_data[0] = m_data[m_data.Count - 1];
            m_data.RemoveAt(m_data.Count - 1);

            int index = 0;
            while (true)
            {
                int minIndex;
                int leftIndex = (index << 1) + 1;
                if ((leftIndex < m_data.Count) && 
                    (m_comparer.Compare(item, m_data[leftIndex]) > 0))
                {
                    minIndex = leftIndex;
                }
                else
                {
                    minIndex = index;
                }

                int rightIndex = leftIndex + 1;
                if ((rightIndex < m_data.Count) && 
                    (m_comparer.Compare(m_data[minIndex], m_data[rightIndex]) > 0))
                {
                    minIndex = rightIndex;
                }

                if (minIndex == index)
                {
                    break;
                }

                Swap(index, minIndex);
                index = minIndex;
            }

            return result;
        }

        /// <summary>
        /// Clear all items in the heap.
        /// </summary>
        public void Clear()
        {
            m_data.Clear();
        }

        /// <summary>
        /// Get an enumerator for all the items currently in heap.
        /// </summary>
        /// <returns>The enumerator for the heap.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return m_data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_data.GetEnumerator();
        }

        /// <summary>
        /// String representation of the heap.
        /// </summary>
        /// <returns>String representation of the heap.</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder(1024);

            foreach (T item in m_data)
            {
                result.AppendFormat("{0} ", item);
            }

            if (result.Length > 0)
            {
                result.Length--;
            }

            return result.ToString();
        }
    }
}
