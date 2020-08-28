using System;
using System.Threading;
using System.Threading.Tasks;

namespace Guan.Common
{
    public abstract class CancellableTaskSource<T>
    {
        private TaskCompletionSource<T> source_;

        public CancellableTaskSource(CancellationToken cancellationToken)
        {
            source_ = new TaskCompletionSource<T>();
            cancellationToken.Register(() => { Cancel(); });
        }

        protected abstract void OnCancelled();

        public Task<T> Task
        {
            get
            {
                return source_.Task;
            }
        }

        private void Cancel()
        {
            if (source_.TrySetCanceled())
            {
                System.Threading.Tasks.Task.Run(() => OnCancelled());
            }
        }

        public bool Complete(T result)
        {
            return source_.TrySetResult(result);
        }

        public void Complete(Exception e)
        {
            source_.SetException(e);
        }
    }

    public abstract class CancellableTaskSource : CancellableTaskSource<bool>
    {
        public CancellableTaskSource(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }

        public bool Complete()
        {
            return Complete(true);
        }
    }
}
