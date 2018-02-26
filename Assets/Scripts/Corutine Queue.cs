//from: http://jacksondunstan.com/articles/3241

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	/// <summary>
	/// Imposes a limit on the maximum number of coroutines that can be running at any given time. Runs
	/// coroutines until the limit is reached and then begins queuing coroutines instead. When
	/// coroutines finish, queued coroutines are run.
	/// </summary>
	/// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3241</author>
	public class CoroutineQueue
	{
		/// <summary>
		/// Maximum number of coroutines to run at once
		/// </summary>
		private readonly uint _maxActive;

		/// <summary>
		/// Delegate to start coroutines with
		/// </summary>
		private readonly Func<IEnumerator, Coroutine> _coroutineStarter;

		/// <summary>
		/// Queue of coroutines waiting to start
		/// </summary>
		private readonly Queue<IEnumerator> _queue;

		/// <summary>
		/// Number of currently active coroutines
		/// </summary>
		public uint NumActive;

		/// <summary>
		/// Create the queue, initially with no coroutines
		/// </summary>
		/// <param name="maxActive">
		/// Maximum number of coroutines to run at once. This must be at least one.
		/// </param>
		/// <param name="coroutineStarter">
		/// Delegate to start coroutines with. Normally you'd pass
		/// <see cref="MonoBehaviour.StartCoroutine"/> for this.
		/// </param>
		/// <exception cref="ArgumentException">
		/// If maxActive is zero.
		/// </exception>
		public CoroutineQueue(uint maxActive, Func<IEnumerator, Coroutine> coroutineStarter)
		{
			if (maxActive == 0)
			{
				throw new ArgumentException("Must be at least one", "maxActive");
			}
			_maxActive = maxActive;
			_coroutineStarter = coroutineStarter;
			_queue = new Queue<IEnumerator>();
		}

		/// <summary>
		/// If the number of active coroutines is under the limit specified in the constructor, run the
		/// given coroutine. Otherwise, queue it to be run when other coroutines finish.
		/// </summary>
		/// <param name="coroutine">Coroutine to run or queue</param>
		public void Run(IEnumerator coroutine)
		{
			if (NumActive < _maxActive)
			{
				var runner = CoroutineRunner(coroutine);
				_coroutineStarter(runner);
			}
			else
			{
				_queue.Enqueue(coroutine);
			}
		}

		/// <summary>
		/// Runs a coroutine then runs the next queued coroutine (via <see cref="Run"/>) if available.
		/// Increments <see cref="NumActive"/> before running the coroutine and decrements it after.
		/// </summary>
		/// <returns>Values yielded by the given coroutine</returns>
		/// <param name="coroutine">Coroutine to run</param>
		private IEnumerator CoroutineRunner(IEnumerator coroutine)
		{
			NumActive++;
			while (coroutine.MoveNext())
			{
				yield return coroutine.Current;
			}
			NumActive--;
			if (_queue.Count > 0)
			{
				var next = _queue.Dequeue();
				Run(next);
			}
		}
	}
}