using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace Palladium.ExtensionFunctions.Tests;

public class ObservableExtensionsTests
{
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

	[Test]
	public void AddTaskCompletion_EmitsTaskAgainWhenItCompletes()
	{
		// arrange
		var tcs = new TaskCompletionSource<int>();
		var subject = new BehaviorSubject<Task<int>>(tcs.Task);
		var taskObservable = subject.AddTaskCompletion();

		// assert
		using var enumerator = taskObservable.ToEnumerable().GetEnumerator();
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(tcs.Task, enumerator.Current);

		// act
		tcs.SetResult(1);

		// assert
		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(tcs.Task, enumerator.Current);
	}

	[Test]
	public void AddTaskCompletion_CompletedTaskIsOnlyEmittedOnce()
	{
		// arrange
		var tcs = new TaskCompletionSource<int>();
		tcs.SetResult(1);
		var testScheduler = new TestScheduler();
		var subject = new BehaviorSubject<Task<int>>(tcs.Task);

		// act
		var observer = testScheduler.CreateObserver<Task<int>?>();
		var taskObservable = subject.AddTaskCompletion();
		taskObservable.Subscribe(observer);
		testScheduler.AdvanceBy(1);

		// assert
		observer.Messages.AssertEqual(ReactiveTest.OnNext<Task<int>?>(0, tcs.Task));
	}

	[Test]
	public void AddTaskCompletion_WhenTaskObservableCompletes_ObservableCompletes()
	{
		// arrange
		var testScheduler = new TestScheduler();
		var completedObservable = Observable.Empty<Task<int>>(testScheduler);

		// act
		var observer = testScheduler.CreateObserver<Task<int>?>();
		var taskObservable = completedObservable.AddTaskCompletion();
		taskObservable.Subscribe(observer);
		testScheduler.AdvanceBy(1);

		// assert
		observer.Messages.AssertEqual(ReactiveTest.OnCompleted<Task<int>?>(1));
	}

	[Test]
	public void AddTaskCompletion_DoesNotEmitsTaskAgain_WhenAnotherTaskIsEmitted()
	{
		// arrange
		var tcs1 = new TaskCompletionSource<int>();
		var tcs2 = new TaskCompletionSource<int>();
		var subject = new BehaviorSubject<Task<int>>(tcs1.Task);
		var taskObservable = subject.AddTaskCompletion();
		var testScheduler = new TestScheduler();
		var observer = testScheduler.CreateObserver<Task<int>?>();
		taskObservable.Subscribe(observer);

		// act
		testScheduler.AdvanceBy(1);
		subject.OnNext(tcs2.Task);

		// act
		testScheduler.AdvanceBy(1);
		tcs1.SetResult(1);

		// act
		testScheduler.AdvanceBy(1);
		tcs2.SetResult(2);


		// assert
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, (Task<int>?)tcs1.Task),
			ReactiveTest.OnNext(1, (Task<int>?)tcs2.Task),
			ReactiveTest.OnNext(3, (Task<int>?)tcs2.Task)
		);
	}

	[Test]
	public void AddTaskCompletion_DoesNotEmitsTaskAgain_WhenNullTaskIsEmitted()
	{
		// arrange
		var tcs1 = new TaskCompletionSource<int>();
		var subject = new BehaviorSubject<Task<int>?>(tcs1.Task);
		var taskObservable = subject.AddTaskCompletion();
		var testScheduler = new TestScheduler();
		var observer = testScheduler.CreateObserver<Task<int>?>();
		taskObservable.Subscribe(observer);

		// act
		testScheduler.AdvanceBy(1);
		subject.OnNext(null);

		// act
		testScheduler.AdvanceBy(1);
		tcs1.SetResult(1);

		// assert
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, (Task<int>?)tcs1.Task),
			ReactiveTest.OnNext(1, (Task<int>?)null)
		);
	}

	[Test]
	public void AddTaskCompletion_EmitsNullTask()
	{
		// arrange
		var subject = new BehaviorSubject<Task<int>?>(null);
		var taskObservable = subject.AddTaskCompletion();
		var testScheduler = new TestScheduler();
		var observer = testScheduler.CreateObserver<Task<int>?>();
		taskObservable.Subscribe(observer);

		// assert
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, (Task<int>?)null)
		);
	}

	[Test]
	public void CombineLatestNoEmit_SequenceTest()
	{
		// arrange
		var left = new Subject<int>();
		var right = new ReplaySubject<string>();
		var toTest = left.CombineLatestNoEmit(right, (number, text) => $"{number}{text}");
		var observer = new TestScheduler().CreateObserver<string>();
		toTest.Subscribe(observer);

		// act
		left.OnNext(0);
		left.OnNext(1);
		right.OnNext("A");
		right.OnNext("B");
		right.OnNext("C");
		left.OnNext(2);
		left.OnNext(3);
		right.OnNext("D");

		// assert 
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, "1A"),
			ReactiveTest.OnNext(0, "2C"),
			ReactiveTest.OnNext(0, "3C")
		);
	}
}