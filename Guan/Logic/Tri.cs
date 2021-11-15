// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;

    public class Tri<T>
        where T : class
    {
        private TriNode root;

        public Tri()
        {
            this.root = new TriNode(0);
        }

        public void Add(string key, T value)
        {
            this.root.Add(key, value);
        }

        public T Get(string key, ref int start)
        {
            return this.root.Get(key, ref start);
        }

        public T Get(string key)
        {
            int start = 0;
            T result = this.Get(key, ref start);
            if (start != key.Length)
            {
                return null;
            }

            return result;
        }

        private class TriNode
        {
            private int level;
            private T value;
            private char begin;
            private TriNode[] children;

            public TriNode(int level)
            {
                this.value = null;
                this.level = level;
            }

            public void Add(string key, T value)
            {
                if (this.level == key.Length)
                {
                    if (this.value != null)
                    {
                        throw new ArgumentException("Duplicate value for: " + key);
                    }

                    this.value = value;
                }
                else
                {
                    int i;
                    if (this.children == null)
                    {
                        this.children = new TriNode[1];
                        this.begin = key[this.level];
                        i = 0;
                    }
                    else
                    {
                        i = key[this.level] - this.begin;
                        if (i < 0)
                        {
                            TriNode[] children = new TriNode[this.children.Length - i];
                            Array.Copy(this.children, 0, children, -i, this.children.Length);
                            this.children = children;
                            this.begin = key[this.level];
                            i = 0;
                        }
                        else if (i >= this.children.Length)
                        {
                            TriNode[] children = new TriNode[i + 1];
                            Array.Copy(this.children, 0, children, 0, this.children.Length);
                            this.children = children;
                        }
                    }

                    if (this.children[i] == null)
                    {
                        this.children[i] = new TriNode(this.level + 1);
                    }

                    this.children[i].Add(key, value);
                }
            }

            public T Get(string key, ref int start)
            {
                if (start + this.level < key.Length)
                {
                    int i = key[start + this.level] - this.begin;
                    if (this.children != null && i >= 0 && i < this.children.Length && this.children[i] != null)
                    {
                        T result = this.children[i].Get(key, ref start);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                if (this.value != null)
                {
                    start += this.level;
                }

                return this.value;
            }
        }
    }
}
