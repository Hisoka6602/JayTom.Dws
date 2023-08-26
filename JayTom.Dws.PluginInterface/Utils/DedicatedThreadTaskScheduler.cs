using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace JayTom.Dws.PluginInterface.Utils {

    public sealed class DedicatedThreadTaskScheduler : TaskScheduler {
        private readonly BlockingCollection<Task> _tasks = new();

        protected override IEnumerable<Task>? GetScheduledTasks() => _tasks;

        protected override void QueueTask(Task task) => _tasks.Add(task);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        public DedicatedThreadTaskScheduler(int threadCount) {
            var threads = new Thread[threadCount];

            for (var index = 0; index < threadCount; index++) {
                threads[index] = new Thread(_ => {
                    while (true) {
                        TryExecuteTask(_tasks.Take());
                    }
                });
            }

            Array.ForEach(threads, it => it.Start());
        }
    }
}