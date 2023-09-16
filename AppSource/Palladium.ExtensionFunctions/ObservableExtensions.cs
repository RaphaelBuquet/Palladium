using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

namespace Palladium.ObservableExtensions;

public static class ObservableExtensions
{
	public static IObservable<(T? Previous, T Current)> PairWithPrevious<T>(this IObservable<T> observable)
	{
		return observable.Scan(
			(Previous: default(T), Current: default(T)!),
			(acc, current) => (acc.Current, current));
	}

	public static IObservable<T> BufferWithToggle<T>(this IObservable<T> observable, IObservable<bool> unlockedToggle)
	{
		return Observable.Create<T>(observer =>
		{
			var disposables = new CompositeDisposable();

			var unlocked = false;
			var queue = new ConcurrentQueue<BufferedItem<T>>();
			var slim = new ReaderWriterLockSlim();

			IDisposable bufferSubscription = observable.Subscribe(
				value =>
				{
					slim.EnterReadLock();
					try
					{
						if (!unlocked)
						{
							queue.Enqueue(new BufferedItem<T> { Value = value });
						}
						else
						{
							observer.OnNext(value);
						}
					}
					finally
					{
						slim.ExitReadLock();
					}
				},
				exception =>
				{
					slim.EnterReadLock();
					try
					{
						if (!unlocked)
						{
							queue.Enqueue(new BufferedItem<T> { Exception = exception });
						}
						else
						{
							observer.OnError(exception);
						}
					}
					finally
					{
						slim.ExitReadLock();
					}
				},
				() =>
				{
					slim.EnterReadLock();
					try
					{
						if (!unlocked)
						{
							queue.Enqueue(new BufferedItem<T> { IsCompleted = true });
						}
						else
						{
							observer.OnCompleted();
						}
					}
					finally
					{
						slim.ExitReadLock();
					}
				});


			IDisposable unlockSubscription = unlockedToggle.Subscribe(newUnlockedState =>
			{
				slim.EnterWriteLock();
				try
				{
					if (newUnlockedState == unlocked) return;
					// unlock
					if (newUnlockedState)
					{
						foreach (var bufferedItem in queue)
						{
							if (bufferedItem.IsCompleted)
							{
								observer.OnCompleted();
							}
							else if (bufferedItem.Exception != null)
							{
								observer.OnError(bufferedItem.Exception);
							}
							observer.OnNext(bufferedItem.Value);
						}
						queue.Clear();
						unlocked = true;
					}
					// lock
					else
					{
						unlocked = false;
					}
				}
				finally
				{
					slim.ExitWriteLock();
				}
			});

			unlockSubscription.DisposeWith(disposables);
			bufferSubscription.DisposeWith(disposables);

			return disposables;
		});
	}

	/// <summary>
	///     When a task is emitted, this observable will also emit it. The difference is that once the task completes, it will
	///     be emitted again.
	///     This is a useful way of observing a state as "pending" (a task is running) and getting the finished state when it's
	///     complete.
	///     If another task is added while the last task is still running, the last task will not be watched anymore.
	/// </summary>
	/// <param name="tasksObservable"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static IObservable<Task<T>> AddTaskCompletion<T>(this IObservable<Task<T>> tasksObservable)
	{
		return Observable.Create<Task<T>>(observer =>
		{
			var composite = new CompositeDisposable();
			var locker = new ReaderWriterLockSlim();
			Task? currentTask;

			tasksObservable.Subscribe(task =>
			{
				// using this to prevent previously watched task from broadcasting OnNext.
				locker.EnterWriteLock();
				currentTask = task;
				locker.ExitWriteLock();

				if (task.IsCompleted)
				{
					observer.OnNext(task);
				}
				else
				{
					observer.OnNext(task);
					task.ContinueWith(task1 =>
					{
						try
						{
							locker.EnterReadLock();
							if (task1 == currentTask)
							{
								observer.OnNext(task1);
							}
						}
						finally
						{
							locker.ExitReadLock();
						}
					}, TaskContinuationOptions.ExecuteSynchronously);
				}
			}).DisposeWith(composite);

			return composite;
		});
	}

	public static IObservable<T> DebugToLogs<T>(this IObservable<T> observable, Log? log, string? message = null)
	{
#if DEBUG
		return observable.Do(
			obj => log?.Emit(new EventId(), LogLevel.Information, $"OnAction {message}: \"{obj}\""),
			ex => log?.Emit(new EventId(), LogLevel.Error, $"OnException {message}: {ex}"),
			() => log?.Emit(new EventId(), LogLevel.Information, $"OnCompleted {message}") );
#else
		return observable;
#endif
	}

	public static IObservable<T> DebugToConsole<T>(this IObservable<T> observable, string? message = null)
	{
#if DEBUG
		return observable.Do(
			obj => Console.WriteLine($"OnAction {message}: \"{obj}\""),
			ex => Console.WriteLine($"OnException {message}: {ex}"),
			() => Console.WriteLine($"OnCompleted {message}") );
#else
		return observable;
#endif
	}
}