using System.Reactive.Linq;
using System.Reactive.Subjects;
using Palladium.ObservableExtensions;

namespace Palladium.ExtensionFunctions.Tests;

public class ObservableExtensionsTests
{
	[Test]
	public void Test()
	{
		var s = new Subject<int>();
		s.OnCompleted();
		var itDoes = false;
		s.Subscribe(i => { },
			() => { itDoes = true; });

		Assert.IsTrue(itDoes);
	}

	[Test]
	public void PairWithPrevious_EmitsWithPrevious()
	{
		// arrange
		var observable = Observable.Range(1, 3);
		var result = new List<(int, int)>();

		// act
		var bufferedObservable = observable.PairWithPrevious();
		bufferedObservable.Subscribe(result.Add);

		// assert
		CollectionAssert.AreEqual(new [] { (0, 1), (1, 2), (2, 3) }, result);
	}

	[Test]
	public void BufferedUntil_Emits_WhenSubscribingAfterRelease_OnColdObservable()
	{
		// arrange
		var observable = Observable.Range(1, 3);
		var unlockedToggle = new BehaviorSubject<bool>(true);
		var result = new List<int>();

		// act
		var bufferedObservable = observable.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);

		// assert
		CollectionAssert.AreEqual(new [] { 1, 2, 3 }, result);
	}

	[Test]
	public void BufferedUntil_OnColdObservable_ReleasesValuesOnUnlock()
	{
		// arrange
		var observable = Observable.Range(1, 3);
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = observable.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);
		unlockedToggle.OnNext(true);

		// assert
		CollectionAssert.AreEqual(new [] { 1, 2, 3 }, result);
	}

	[Test]
	public void BufferedUntil_OnHotObservable_ReleasesValuesOnUnlock()
	{
		// arrange
		var subject = new Subject<int>();
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = subject.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);
		subject.OnNext(1);
		subject.OnNext(2);
		subject.OnNext(3);
		unlockedToggle.OnNext(true);

		// assert
		CollectionAssert.AreEqual(new [] { 1, 2, 3 }, result);
	}

	[Test]
	public void BufferedUntil_PassesThroughValues_EmittedAfterUnlock()
	{
		// arrange
		var subject = new Subject<int>();
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = subject.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);
		unlockedToggle.OnNext(true);
		subject.OnNext(1);
		subject.OnNext(2);
		subject.OnNext(3);

		// assert
		CollectionAssert.AreEqual(new [] { 1, 2, 3 }, result);
	}

	[Test]
	public void BufferedUntil_DoesNotPassThroughValues_BeforeUnlock()
	{
		// arrange
		var observable = Observable.Range(1, 3);
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = observable.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);

		// assert
		CollectionAssert.AreEqual(Array.Empty<int>(), result);
	}

	[Test]
	public void BufferedUntil_WithMultipleLockUnlocks_ReleasesValuesOnUnlock()
	{
		// arrange
		var subject = new Subject<int>();
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = subject.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);
		subject.OnNext(1);
		unlockedToggle.OnNext(true);
		unlockedToggle.OnNext(false);
		subject.OnNext(2);

		// assert 
		CollectionAssert.AreEqual(new [] { 1 }, result);

		// act
		unlockedToggle.OnNext(true);

		// assert
		CollectionAssert.AreEqual(new [] { 1, 2 }, result);
	}

	[Test]
	public void BufferedUntil_WithUnlockInAnotherThread_ReleasesValuesOnUnlock()
	{
		// arrange
		var subject = new Subject<int>();
		var unlockedToggle = new Subject<bool>();
		var result = new List<int>();

		// act
		var bufferedObservable = subject.BufferWithToggle(unlockedToggle);
		bufferedObservable.Subscribe(result.Add);
		Task t1 = Task.Run(() => subject.OnNext(1));
		Task t2 = Task.Run(() => subject.OnNext(2));
		Task t3 = Task.Run(() => subject.OnNext(3));
		unlockedToggle.OnNext(true);

		// assert 
		Task.WaitAll(t1, t2, t3);
		CollectionAssert.AreEquivalent(new [] { 1, 2, 3 }, result); // order is not deterministic
	}
}