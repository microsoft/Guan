// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
namespace Guan.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Context to provide the following during rule execution:
    /// 1. The asserted predicates.
    /// 2. Global variables, including some system variables controlling:
    ///    2.1: resolve direction
    ///    2.2: trace/debug flag
    /// 3. A hierarhy of contexts to track parallel execution.
    /// </summary>
    public class QueryContext : IPropertyContext, IWritablePropertyContext
    {
        private static long seqGenerator = 0;
        private readonly IFunctorProvider provider;
        private readonly QueryContext parent;
        private List<QueryContext> children;
        private readonly QueryContext root;
        private string orderProperty;
        private ResolveOrder order;
        private readonly Dictionary<string, object> variables;
        private readonly Module dynamicModule;
        private bool isLocalStable;
        private bool isStable;
        private bool isLocalSuspended;
        private bool isSuspended;
        private bool isCancelled;
        private bool enableTrace;
        private bool enableDebug;
        private readonly List<IWaitingTask> waiting;
        private readonly long seq;

        public QueryContext(IFunctorProvider provider)
        {
            this.seq = Interlocked.Increment(ref seqGenerator);
            this.provider = provider;
            this.root = this;
            this.order = ResolveOrder.None;
            this.variables = new Dictionary<string, object>();
            this.dynamicModule = new Module("asserted");
            this.isStable = this.isLocalStable = false;
            this.isSuspended = this.isLocalSuspended = false;
            this.isCancelled = false;
            this.enableTrace = false;
            this.enableDebug = false;
            this.waiting = new List<IWaitingTask>();
        }

        protected QueryContext(QueryContext parent)
        {
            this.seq = Interlocked.Increment(ref seqGenerator);
            this.parent = parent;
            this.provider = parent.provider;
            this.root = parent.root;
            this.orderProperty = parent.orderProperty;
            this.dynamicModule = parent.dynamicModule;
            this.order = parent.order;
            this.variables = parent.variables;
            this.isStable = this.isLocalStable = false;
            this.isSuspended = this.isLocalSuspended = false;
            this.isCancelled = false;
            this.enableTrace = parent.enableTrace;
            this.enableDebug = parent.enableDebug;
            this.waiting = parent.waiting;
            lock (this.root)
            {
                this.SetStable(false);
                this.parent.AddChild(this);
            }
        }

        public event EventHandler Suspended;
        public event EventHandler Resumed;

        public IFunctorProvider Provider
        {
            get
            {
                return this.provider;
            }
        }

        public string OrderProperty
        {
            get
            {
                return this.orderProperty;
            }
        }

        public ResolveOrder Order
        {
            get
            {
                return this.order;
            }
        }

        public bool IsCancelled
        {
            get
            {
                return this.isCancelled;
            }

            set
            {
                Queue<QueryContext> queue = new Queue<QueryContext>();
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    QueryContext context = queue.Dequeue();
                    if (!context.IsCancelled)
                    {
                        context.isCancelled = true;
                        context.isSuspended = true;

                        if (context.children != null)
                        {
                            foreach (QueryContext child in context.children)
                            {
                                queue.Enqueue(child);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Used to coordinate parallel execution where some tasks should be run
        /// together.
        /// A context is stable if the context itself and all its children contexts
        /// are stable.
        /// There is an execution queue at the root level for all tasks that should be
        /// executed together and they will only be executed when the root context
        /// is stable, which should only be true when no more such tasks will be
        /// generated (until some such task is completed and new task is triggered).
        /// </summary>
        internal bool IsStable
        {
            get
            {
                return this.isStable;
            }

            set
            {
                lock (this.root)
                {
                    this.SetStable(value);
                }
            }
        }

        /// <summary>
        /// Whether the context is suspended, which can happen during parallel
        /// execution.
        /// A context is suspended when either itself or any parent is suspended.
        /// </summary>
        internal bool IsSuspended
        {
            get
            {
                return this.isSuspended;
            }

            set
            {
                this.isLocalSuspended = value;
                this.UpdateSuspended(this.parent != null ? this.parent.isSuspended : false);
            }
        }

        internal bool EnableTrace
        {
            get
            {
                return this.enableTrace;
            }

            set
            {
                this.enableTrace = value;
            }
        }

        internal bool EnableDebug
        {
            get
            {
                return this.enableDebug || this.enableTrace;
            }

            set
            {
                this.enableDebug = value;
            }
        }

        internal Module DynamicModule
        {
            get
            {
                return this.dynamicModule;
            }
        }

        public virtual object this[string name]
        {
            get
            {
                if (name == "Order")
                {
                    return this.order;
                }

                _ = this.variables.TryGetValue(name, out object result);
                return result;
            }

            set
            {
                if (name == "Order")
                {
                    if (value is ResolveOrder order1)
                    {
                        this.order = order1;
                    }
                    else
                    {
                        this.order = (ResolveOrder)Enum.Parse(typeof(ResolveOrder), (string)value);
                    }
                }
                else
                {
                    this.variables[name] = value;
                }
            }
        }

        public void SetDirection(string directionProperty, ResolveOrder direction)
        {
            this.orderProperty = directionProperty;
            this.order = direction;
        }

        public override string ToString()
        {
            return this.seq.ToString();
        }

        internal virtual QueryContext CreateChild()
        {
            return new QueryContext(this);
        }

        internal void ClearChildren()
        {
            lock (this.root)
            {
                if (this.children != null)
                {
                    this.children.Clear();
                }

                this.isStable = this.isLocalStable;
            }
        }

        internal void AddWaiting(IWaitingTask suspended)
        {
            this.waiting.Add(suspended);
        }

        private void AddChild(QueryContext child)
        {
            if (this.children == null)
            {
                this.children = new List<QueryContext>();
            }

            this.children.Add(child);
        }

        private void SetStable(bool value)
        {
            this.isLocalStable = value;
            QueryContext context = this;
            bool updated;
            do
            {
                updated = context.UpdateStable();
                context = context.parent;
            }
            while (context != null && updated);

            if (this.root.isStable)
            {
                this.root.Start();
            }
        }

        private bool UpdateStable()
        {
            bool result = this.isLocalStable;
            for (int i = 0; result && this.children != null && i < this.children.Count; i++)
            {
                result = this.children[i].isStable;
            }

            if (result == this.isStable)
            {
                return false;
            }

            this.isStable = result;
            return true;
        }

        private void UpdateSuspended(bool value)
        {
            bool newValue = (value || this.isLocalSuspended);
            if (newValue == this.isSuspended)
            {
                return;
            }

            this.isSuspended = newValue;
            if (this.children != null)
            {
                foreach (QueryContext child in this.children)
                {
                    child.UpdateSuspended(this.isSuspended);
                }
            }

            if (this.isSuspended)
            {
                this.Suspended?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.Resumed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Start()
        {
            foreach (IWaitingTask suspended in this.waiting)
            {
                _ = Task.Run(() => { suspended.Start(); });
            }

            this.waiting.Clear();
        }
    }
}