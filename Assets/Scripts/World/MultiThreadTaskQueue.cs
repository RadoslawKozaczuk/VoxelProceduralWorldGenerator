using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.Scripts.World
{
    /// <summary>
    /// This queue adds some additional overhead although it is still way faster than it would have been without parallelization.
    /// On the other hand it is way more convenient to use.
    /// For example it encapsulates things that are often source of problems - the necessity of parameters copy.
    /// </summary>
    class MultiThreadTaskQueue
    {
        readonly int _logicalProcessorCount = Environment.ProcessorCount;
        readonly List<Task> _pendingTasks = new List<Task>();
        bool _isRunning = false; // this queue is very simplistic and adding new tasks is impossible when the queue is executing tasks
        int _index = 0;

        /// <summary>
        /// Adds the given action to the queue.
        /// Template types must match in type, order and number the parameters of the given method.
        /// Important: To run the task in parallel add all tasks and then call RunAllInParallel method.
        /// </summary>
        public void ScheduleTask<T1, T2, T3>(Action<T1, T2, T3> action, params object[] array)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
#if UNITY_EDITOR || UNITY_DEVELOPMENT
            Assertions(action.Method, array, 3);
#endif

            // use variable capture to 'pass in' parameters in order to avoid data share
            // we have to do it because values changed outside of a task are also changed in the task
            T1 copy1 = (T1)array[0];
            T2 copy2 = (T2)array[1];
            T3 copy3 = (T3)array[2];

            _pendingTasks.Add(new Task(() => action(copy1, copy2, copy3)));
        }

        public void ScheduleTask<T1, T2>(Action<T1, T2> action, params object[] array)
            where T1 : struct
            where T2 : struct
        {
#if UNITY_EDITOR || UNITY_DEVELOPMENT
            Assertions(action.Method, array, 3);
#endif

            // use variable capture to 'pass in' parameters in order to avoid data share
            // we have to do it because values changed outside of a task are also changed in the task
            T1 copy1 = (T1)array[0];
            T2 copy2 = (T2)array[1];

            _pendingTasks.Add(new Task(() => action(copy1, copy2)));
        }

        public void ScheduleTask<T>(Action<T> action, params object[] array)
            where T : struct
        {
#if UNITY_EDITOR || UNITY_DEVELOPMENT
            Assertions(action.Method, array, 3);
#endif

            // use variable capture to 'pass in' parameters in order to avoid data share
            // we have to do it because values changed outside of a task are also changed in the task
            T copy = (T)array[0];

            _pendingTasks.Add(new Task(() => action(copy)));
        }

        public void RunAllInParallel()
        {
            _isRunning = true;

            var _ongoingTasks = new Task[_logicalProcessorCount];

            // start first 8 (or any processors the target machine has)
            for (int i = 0; i < _logicalProcessorCount; i++)
            {
                if (_index == _pendingTasks.Count - 1) // less than 8 was scheduled
                    break;

                _ongoingTasks[i] = _pendingTasks[_index++];
                _ongoingTasks[i].Start();
            }

            // start new task as soon as we have a free thread available
            // and keep on doing that until you reach the end of the array
            do
            {
                int completedId = Task.WaitAny(_ongoingTasks);

                if (_index == _pendingTasks.Count - 1)
                    break;

                _ongoingTasks[completedId] = _pendingTasks[_index++];
                _ongoingTasks[completedId].Start();
            }
            while (true);

            Task.WaitAll(_ongoingTasks);

            _pendingTasks.Clear();
            _index = 0;
            _isRunning = false;
        }

#if UNITY_EDITOR || UNITY_DEVELOPMENT
        void Assertions(System.Reflection.MethodInfo info, object[] paramArray, int paramNumber)
        {
            System.Reflection.ParameterInfo[] paramInfo = info.GetParameters();

            if (paramInfo.Length != paramNumber)
                throw new System.ArgumentException($"Given action has the number of parameters different than {paramNumber}. " +
                    "Please use a method overload with the number of parameters corresponding to the number of parameters of the action.",
                    "action");
            else if (paramArray.Length != paramNumber)
                throw new System.ArgumentException($"The number of parameters is different than expected ({paramNumber}). " +
                    "Please use a different method overload.",
                    "array");
            else if (_isRunning)
                throw new System.ArgumentException("Adding new tasks after queue execution start in not allowed.");
        }
#endif
    }
}
