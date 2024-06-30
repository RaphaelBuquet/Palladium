using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

namespace Palladium.ExtensionFunctions;

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


#nullable disable
	/// <summary>
	///     When a task is emitted, this observable will also emit it. The difference is that once the task completes, it will
	///     be emitted again.
	///     This is a useful way of observing a state as "pending" (a task is running) and getting the finished state when it's
	///     complete.
	///     If another task is added while the last task is still running, the last task will not be watched anymore.
	/// </summary>
	/// <param name="tasksObservable"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns>
	///     An observable of tasks. If <paramref name="tasksObservable" /> emits null values then it will emit null values
	///     too.
	/// </returns>
	public static IObservable<Task<T>> AddTaskCompletion<T>(this IObservable<Task<T>> tasksObservable)
	{
		if (tasksObservable is null) throw new ArgumentNullException(nameof(tasksObservable));
		return Observable.Create<Task<T>>(observer =>
		{
			var composite = new CompositeDisposable();
			var locker = new ReaderWriterLockSlim();
			Task currentTask;

			tasksObservable.Subscribe(
				task =>
				{
					// using this to prevent previously watched task from broadcasting OnNext.
					locker.EnterWriteLock();
					currentTask = task;
					locker.ExitWriteLock();

					if (task == null || task.IsCompleted)
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
				},
				observer.OnError,
				observer.OnCompleted).DisposeWith(composite);

			return composite;
		});
	}
#nullable restore

	public static IDisposable LoggedCatch(this IObservable<Exception> observable, Log? log, string? message = null)
	{
		return observable.Subscribe(e => log?.Emit(new EventId(), LogLevel.Error, message, e));
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

	public static IObservable<T> IgnoreErrors<T>(this IObservable<T> observable)
	{
		return Observable.Create<T>(observer => { return observable.Subscribe(observer.OnNext, _ => { }, observer.OnCompleted); });
	}

	/// <summary>
	///     Returns only values that are not null.
	///     Converts the nullability.
	/// </summary>
	/// <typeparam name="T">The type of value emitted by the observable.</typeparam>
	/// <param name="observable">The observable that can contain nulls.</param>
	/// <returns>A non nullable version of the observable that only emits valid values.</returns>
	public static IObservable<T> WhereNotNull<T>(this IObservable<T?> observable) where T : struct
	{
		return observable
			.Where(x => x.HasValue)
			.Select(x => x!.Value);
	}

	/// <summary>
	///     <para>
	///         Similar to CombineLatest, but if a new value is emitted by <paramref name="right" />, it won't emit a new pair.
	///         New pairs are only emitted when <paramref name="left" /> emits, along with the last recorded value of
	///         <paramref name="right" />.
	///     </para>
	///     <para>
	///         The first pair is emitted once both <paramref name="left" /> and <paramref name="right" /> have emitted at
	///         least a
	///         value.
	///     </para>
	/// </summary>
	public static IObservable<TResult> CombineLatestNoEmit<TLeft, TRight, TResult>
	(
		this IObservable<TLeft> left,
		IObservable<TRight> right,
		Func<TLeft, TRight, TResult> resultSelector
	)
	{
		var refcountedLeft = left.Publish().RefCount();
		var refcountedRight = right.Publish().RefCount();

		var onCompletedNotifier = new Subject<TLeft?>();

		var hasAccessedRight = 0;

		return Observable.Join(
			refcountedLeft,
			refcountedRight,
			value =>
			{
				// immediately close the time window, if an item has ever been emitted by the right
				if (hasAccessedRight > 0)
				{
					return Observable.Empty<TLeft>();
				}
				// otherwise, close the time window when a new item is emitted, or when an item from the right is emitted.
				return refcountedLeft.TakeUntil(onCompletedNotifier);
			},
			value =>
			{
				hasAccessedRight++;
				// don't complete the first one, as it would get completed before this function returns
				// so instead complete the second one just before it's about to be used :)
				if (hasAccessedRight == 2)
				{
					onCompletedNotifier.OnNext(default);
				}
				return refcountedRight;
			},
			resultSelector);
	}
}