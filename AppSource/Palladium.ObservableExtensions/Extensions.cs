using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Palladium.Logging;

namespace Palladium.ObservableExtensions;

public static class Extensions
{
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

	public static IObservable<T> DebugToLogs<T>(this IObservable<T> observable, string? message = null)
	{
#if DEBUG
		return observable.Do(
			obj => Log.Emit(new EventId(), LogLevel.Information, $"OnAction {message}: \"{obj}\""),
			ex => Log.Emit(new EventId(), LogLevel.Error, $"OnException {message}: {ex}"),
			() => Log.Emit(new EventId(), LogLevel.Information, $"OnCompleted {message}") );
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