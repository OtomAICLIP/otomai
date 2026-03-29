// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace Bubble.Core.Network.Transport.Sockets.Internal;

internal sealed class IOQueue : PipeScheduler, IThreadPoolWorkItem
{
    private readonly ConcurrentQueue<Work> _workItems;

    private int _doingWork;

    public IOQueue()
    {
        _workItems = [];
    }

    public override void Schedule(Action<object?> action, object? state)
    {
        _workItems.Enqueue(new Work(action, state));

        // Set working if it wasn't (via atomic Interlocked).
        if (Interlocked.CompareExchange(ref _doingWork, 1, 0) is 0)
            // Wasn't working, schedule.
            System.Threading.ThreadPool.UnsafeQueueUserWorkItem(this, false);
    }

    void IThreadPoolWorkItem.Execute()
    {
        while (true)
        {
            while (_workItems.TryDequeue(out var item))
                item.Callback(item.State);

            // All work done.

            // Set _doingWork (0 == false) prior to checking IsEmpty to catch any missed work in interim.
            // This doesn't need to be volatile due to the following barrier (i.e. it is volatile).
            _doingWork = 0;

            // Ensure _doingWork is written before IsEmpty is read.
            // As they are two different memory locations, we insert a barrier to guarantee ordering.
            Thread.MemoryBarrier();

            // Check if there is work to do
            if (_workItems.IsEmpty)
                // Nothing to do, exit.
                break;

            // Is work, can we set it as active again (via atomic Interlocked), prior to scheduling?
            if (Interlocked.Exchange(ref _doingWork, 1) is 1)
                // Execute has been rescheduled already, exit.
                break;

            // Is work, wasn't already scheduled so continue loop.
        }
    }

    private record struct Work(Action<object?> Callback, object? State);
}