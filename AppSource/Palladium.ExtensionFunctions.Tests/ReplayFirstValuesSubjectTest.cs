using Microsoft.Reactive.Testing;
using Palladium.ObservableExtensions;

namespace Palladium.ExtensionFunctions.Tests;

public class ReplayFirstValuesSubjectTest
{
	[Test]
	public void FirstValue_EmittedAgain_WithNewSubscription()
	{
		// arrange
		var subject = new ReplayFirstValuesSubject<int>(1);
		var list1 = new List<int>();
		var list2 = new List<int>();

		// act
		subject.Subscribe(list1.Add);
		subject.OnNext(1);
		subject.Subscribe(list2.Add);

		// assert
		CollectionAssert.AreEqual(new [] { 1 }, list1);
		CollectionAssert.AreEqual(new [] { 1 }, list2);
	}

	[Test]
	public void FirstValue_AlwaysEmitted_WithNewSubscription()
	{
		// arrange
		var subject = new ReplayFirstValuesSubject<int>(1);
		var list1 = new List<int>();
		var list2 = new List<int>();

		// act
		subject.OnNext(1);
		subject.Subscribe(list1.Add);
		subject.Subscribe(list2.Add);

		// assert
		CollectionAssert.AreEqual(new [] { 1 }, list1);
		CollectionAssert.AreEqual(new [] { 1 }, list2);
	}

	[Test]
	public void FirstValue_EmittedEvenAfterCompleted_WithNewSubscription()
	{
		// arrange
		var scheduler = new TestScheduler();
		var observer = scheduler.CreateObserver<int>();
		var subject = new ReplayFirstValuesSubject<int>(1);

		// act
		subject.OnNext(1);
		subject.OnCompleted();
		subject.Subscribe(observer);

		// assert
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, 1),
			ReactiveTest.OnCompleted<int>(0));
	}

	[Test]
	public void FirstValue_EmittedEvenAfterError_WithNewSubscription()
	{
		// arrange
		var scheduler = new TestScheduler();
		var observer = scheduler.CreateObserver<int>();
		var subject = new ReplayFirstValuesSubject<int>(1);

		// act
		subject.OnNext(1);
		var exception = new Exception();
		subject.OnError(exception);
		subject.Subscribe(observer);

		// assert
		observer.Messages.AssertEqual(
			ReactiveTest.OnNext(0, 1),
			ReactiveTest.OnError<int>(0, exception));
	}
}