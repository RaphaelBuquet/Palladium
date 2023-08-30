using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Microsoft.Reactive.Testing;
using ReactiveUI;

namespace Palladium.Controls.Tests;

public class MiniLogTests
{
	[Test]
	public void ContentAddedToInlines_UpdatesTextStream()
	{
		// arrange
		var scheduler = new TestScheduler();
		var observer = scheduler.CreateObserver<Inline>();
		var control = new MiniLog();
		control.WhenAnyValue(x => x.TextStream)
			.Switch()
			.Subscribe(observer);
		scheduler.Start();

		// act 
		var inline = new Run("Hello");
		control.Inlines.Add(inline);

		// assert
		observer.Messages.AssertEqual(ReactiveTest.OnNext<Inline>(0, inline));
	}

	[AvaloniaTest]
	public void WhenTextStreamChanges_TextBlockIsUpdated()
	{
		// arrange
		var source = new Subject<Inline>();
		var control = new MiniLog { TextStream = source };

		// act
		var inline1 = new Run("Hello ");
		var inline2 = new Bold { Inlines = new InlineCollection { new Run("world") } };
		source.OnNext(inline1);
		source.OnNext(inline2);
		var window = new Window
		{
			Content = control
		};
		window.Show();

		// assert
		TextBlock? textBlock = control.GetTemplateChildren().OfType<TextBlock>().FirstOrDefault();
		Assert.IsNotNull(textBlock);
		CollectionAssert.AreEqual(new InlineCollection { inline1, inline2 },  textBlock!.Inlines);
	}

	[AvaloniaTest]
	public void WhenContentIsAddedToInlines_TextBlockIsUpdated()
	{
		// arrange
		var scheduler = new TestScheduler();
		var observer = scheduler.CreateObserver<Inline>();
		var control = new MiniLog();
		control.WhenAnyValue(x => x.TextStream)
			.Switch()
			.Subscribe(observer);
		scheduler.Start();

		// act 
		var inline = new Run("Hello");
		control.Inlines.Add(inline);
		var window = new Window
		{
			Content = control
		};
		window.Show();

		// assert
		TextBlock? textBlock = control.GetTemplateChildren().OfType<TextBlock>().FirstOrDefault();
		Assert.IsNotNull(textBlock);
		CollectionAssert.AreEqual(new InlineCollection { inline },  textBlock!.Inlines);
	}
}