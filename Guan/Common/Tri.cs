// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace Guan.Common
{
    public class Tri<T> where T : class
    {
        class TriNode
        {
            private int level_;
            private T value_;
            private char begin_;
            private TriNode[] children_;

            public TriNode(int level)
            {
                level_ = level;
            }

            public void Add(string key, T value)
            {
                if (level_ == key.Length)
                {
                    if (value_ != null)
                    {
                        throw new ArgumentException("Duplicate value for: " + key);
                    }
                    value_ = value;
                }
                else
                {
                    int i;
                    if (children_ == null)
                    {
                        children_ = new TriNode[1];
                        begin_ = key[level_];
                        i = 0;
                    }
                    else
                    {
                        i = key[level_] - begin_;
                        if (i < 0)
                        {
                            TriNode[] children = new TriNode[children_.Length - i];
                            Array.Copy(children_, 0, children, -i, children_.Length);
                            children_ = children;
                            begin_ = key[level_];
                            i = 0;
                        }
                        else if (i >= children_.Length)
                        {
                            TriNode[] children = new TriNode[i + 1];
                            Array.Copy(children_, 0, children, 0, children_.Length);
                            children_ = children;
                        }
                    }

                    if (children_[i] == null)
                    {
                        children_[i] = new TriNode(level_ + 1);
                    }

                    children_[i].Add(key, value);
                }
            }

            public T Get(string key, ref int start)
            {
                if (start + level_ == key.Length)
                {
                    if (value_ != null)
                    {
                        start += level_;
                    }
                    return value_;
                }

                int i = key[start + level_] - begin_;
                if (children_ != null && i >= 0 && i < children_.Length && children_[i] != null)
                {
                    T result = children_[i].Get(key, ref start);
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (value_ != null)
                {
                    start += level_;
                }

                return value_;
            }
        }

        private TriNode root_;

        public Tri()
        {
            root_ = new TriNode(0);
        }

        public void Add(string key, T value)
        {
            root_.Add(key, value);
        }

        public T Get(string key, ref int start)
        {
            return root_.Get(key, ref start);
        }

        public T Get(string key)
        {
            int start = 0;
            T result = Get(key, ref start);
            if (start != key.Length)
            {
                return null;
            }

            return result;
        }
    }
}
