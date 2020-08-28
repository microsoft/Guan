using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Guan.Common;

namespace Guan.Logic
{
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
        private QueryContext parent_;
        private List<QueryContext> children_;
        private QueryContext root_;
        private string orderProperty_;
        private ResolveOrder order_;
        private Dictionary<string, object> variables_;
        private Module asserted_;
        private bool isLocalStable_;
        private bool isStable_;
        private bool isLocalSuspended_;
        private bool isSuspended_;
        private bool isCancelled_;
        private bool enableTrace_;
        private bool enableDebug_;
        private List<IWaitingTask> waiting_;
        private long seq_;

        public event EventHandler Suspended;
        public event EventHandler Resumed;

        private static long Seq = 0;

        public QueryContext()
        {
            seq_ = Interlocked.Increment(ref Seq);
            root_ = this;
            order_ = ResolveOrder.None;
            variables_ = new Dictionary<string, object>();
            asserted_ = new Module("asserted");
            isStable_ = isLocalStable_ = false;
            isSuspended_ = isLocalSuspended_ = false;
            isCancelled_ = false;
            enableTrace_ = false;
            enableDebug_ = false;
            waiting_ = new List<IWaitingTask>();
        }

        protected QueryContext(QueryContext parent)
        {
            seq_ = Interlocked.Increment(ref Seq);
            parent_ = parent;
            root_ = parent.root_;
            orderProperty_ = parent.orderProperty_;
            asserted_ = parent.asserted_;
            order_ = parent.order_;
            variables_ = parent.variables_;
            isStable_ = isLocalStable_ = false;
            isSuspended_ = isLocalSuspended_ = false;
            isCancelled_ = false;
            enableTrace_ = parent.enableTrace_;
            enableDebug_ = parent.enableDebug_;
            waiting_ = parent.waiting_;
            lock (root_)
            {
                SetStable(false);
                parent_.AddChild(this);
            }
        }

        internal virtual QueryContext CreateChild()
        {
            return new QueryContext(this);
        }

        private void AddChild(QueryContext child)
        {
            if (children_ == null)
            {
                children_ = new List<QueryContext>();
            }

            children_.Add(child);
        }

        internal void ClearChildren()
        {
            lock (root_)
            {
                if (children_ != null)
                {
                    children_.Clear();
                }
                isStable_ = isLocalStable_;
            }
        }

        public void SetDirection(string directionProperty, ResolveOrder direction)
        {
            orderProperty_ = directionProperty;
            order_ = direction;
        }

        public virtual object this[string name]
        {
            get
            {
                if (name == "Order")
                {
                    return order_;
                }

                object result;
                variables_.TryGetValue(name, out result);
                return result;
            }
            set
            {
                if (name == "Order")
                {
                    if (value is ResolveOrder)
                    {
                        order_ = (ResolveOrder)value;
                    }
                    else
                    {
                        order_ = (ResolveOrder)Enum.Parse(typeof(ResolveOrder), (string)value);
                    }
                }
                else
                {
                    variables_[name] = value;
                }
            }
        }

        public string OrderProperty
        {
            get
            {
                return orderProperty_;
            }
        }

        public ResolveOrder Order
        {
            get
            {
                return order_;
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
                return isStable_;
            }
            set
            {
                lock (root_)
                {
                    SetStable(value);
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
                return isSuspended_;
            }
            set
            {
                isLocalSuspended_ = value;
                UpdateSuspended(parent_ != null ? parent_.isSuspended_ : false);
            }
        }

        internal bool EnableTrace
        {
            get
            {
                return enableTrace_;
            }
            set
            {
                enableTrace_ = value;
            }
        }

        internal bool EnableDebug
        {
            get
            {
                return enableDebug_ || enableTrace_;
            }
            set
            {
                enableDebug_ = value;
            }
        }

        public bool IsCancelled
        {
            get
            {
                return isCancelled_;
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
                        context.isCancelled_ = true;
                        context.isSuspended_ = true;

                        if (context.children_ != null)
                        {
                            foreach (QueryContext child in context.children_)
                            {
                                queue.Enqueue(child);
                            }
                        }
                    }
                }
            }
        }

        private void SetStable(bool value)
        {
            isLocalStable_ = value;
            QueryContext context = this;
            bool updated;
            do
            {
                updated = context.UpdateStable();
                context = context.parent_;
            } while (context != null && updated);

            if (root_.isStable_)
            {
                root_.Start();
            }
        }

        private bool UpdateStable()
        {
            bool result = isLocalStable_;
            for (int i = 0; result && children_ != null && i < children_.Count; i++)
            {
                result = children_[i].isStable_;
            }

            if (result == isStable_)
            {
                return false;
            }

            isStable_ = result;
            return true;
        }

        private void UpdateSuspended(bool value)
        {
            bool newValue = (value || isLocalSuspended_);
            if (newValue == isSuspended_)
            {
                return;
            }

            isSuspended_ = newValue;
            if (children_ != null)
            {
                foreach (QueryContext child in children_)
                {
                    child.UpdateSuspended(isSuspended_);
                }
            }

            if (isSuspended_)
            {
                Suspended?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Resumed?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void Assert(CompoundTerm term, bool append)
        {
            asserted_.Add(term, append);
        }

        internal PredicateType GetAssertedPredicateType(string name)
        {
            return asserted_.GetPredicateType(name);
        }

        internal void AddWaiting(IWaitingTask suspended)
        {
            waiting_.Add(suspended);
        }

        private void Start()
        {
            foreach (IWaitingTask suspended in waiting_)
            {
                Task.Run(() => { suspended.Start(); });
            }

            waiting_.Clear();
        }

        public override string ToString()
        {
            return seq_.ToString();
        }
    }
}
