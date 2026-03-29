// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Bubble.Core.Network.Internal;

internal static class TaskToApm
{
    public static IAsyncResult Begin(Task task, AsyncCallback? callback, object? state)
    {
        return new TaskAsyncResult(task, state, callback);
    }

    public static void End(IAsyncResult asyncResult)
    {
        if (asyncResult is TaskAsyncResult twar)
        {
            twar._task.GetAwaiter().GetResult();
            return;
        }

        throw new ArgumentNullException(nameof(asyncResult));
    }

    public static TResult End<TResult>(IAsyncResult asyncResult)
    {
        if (asyncResult is TaskAsyncResult { _task: Task<TResult> task })
            return task.GetAwaiter().GetResult();

        throw new ArgumentNullException(nameof(asyncResult));
    }

    internal sealed class TaskAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback? _callback;

        internal readonly Task _task;

        public object? AsyncState { get; }

        public bool CompletedSynchronously { get; }

        public bool IsCompleted =>
            _task.IsCompleted;

        public WaitHandle AsyncWaitHandle =>
            ((IAsyncResult)_task).AsyncWaitHandle;

        internal TaskAsyncResult(Task task, object? state, AsyncCallback? callback)
        {
            Debug.Assert(task is not null);
            _task = task;
            AsyncState = state;

            if (task.IsCompleted)
            {
                // Synchronous completion.  Invoke the callback.  No need to store it.
                CompletedSynchronously = true;
                callback?.Invoke(this);
            }
            else if (callback is not null)
            {
                // Asynchronous completion, and we have a callback; schedule it. We use OnCompleted rather than ContinueWith in
                // order to avoid running synchronously if the task has already completed by the time we get here but still run
                // synchronously as part of the task's completion if the task completes after (the more common case).
                _callback = callback;
                _task
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .OnCompleted(InvokeCallback); // allocates a delegate, but avoids a closure
            }
        }

        private void InvokeCallback()
        {
            Debug.Assert(!CompletedSynchronously);
            Debug.Assert(_callback is not null);
            _callback.Invoke(this);
        }
    }
}