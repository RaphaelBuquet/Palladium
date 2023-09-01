using System.Reactive.Subjects;

namespace Palladium.ObservableExtensions;

/// <summary>
///     Useful for view models that need to emit some values in their constructor, but the bindings with the view haven't
///     yet been installed.
/// </summary>
public class ReplayFirstValuesSubject<T> : ISubject<T>
{
	private readonly int repeatCount;
	private readonly T[] items;
	private readonly Subject<T> subject = new ();
	private int itemsCount;
	private bool isTerminated = false;

	public ReplayFirstValuesSubject(int repeatCount)
	{
		this.repeatCount = repeatCount;
		items = new T[repeatCount];
	}

	/// <inheritdoc />
	public void OnCompleted()
	{
		isTerminated = true;
		subject.OnCompleted();
	}

	/// <inheritdoc />
	public void OnError(Exception error)
	{
		isTerminated = true;
		subject.OnError(error);
	}

	/// <inheritdoc />
	public void OnNext(T value)
	{
		// Note: thread safety isn't hugely important here.
		// If an item is added moments after OnCompleted/OnError is called, it isn't a big deal.
		// If indeed multiple threads are handling this object, then the caller is expecting an undeterministic order.
		if (!isTerminated && itemsCount < repeatCount)
		{
			items[itemsCount] = value;
			Thread.MemoryBarrier(); // make sure Subscribe doesn't try to read an array item that hasn't been set yet  
			itemsCount++;
		}
		subject.OnNext(value);
	}

	/// <inheritdoc />
	public IDisposable Subscribe(IObserver<T> observer)
	{
		for (var i = 0; i < itemsCount; i++)
		{
			observer.OnNext(items[i]);
		}
		return subject.Subscribe(observer);
	}
}