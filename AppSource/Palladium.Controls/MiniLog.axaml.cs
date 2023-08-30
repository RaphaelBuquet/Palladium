using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using DynamicData;
using DynamicData.Binding;
using Palladium.ObservableExtensions;

namespace Palladium.Controls;

[TemplatePart("PART_TextBlock", typeof(TextBlock))]
public class MiniLog : TemplatedControl
{
	public static readonly DirectProperty<MiniLog, IObservable<Inline>?> TextStreamProperty = AvaloniaProperty.RegisterDirect<MiniLog, IObservable<Inline>?>(
		nameof(TextStream), o => o.TextStream, (o, v) => o.TextStream = v);

	private IObservable<Inline>? textStream = Observable.Never<Inline>();
	private TextBlock? textBlock;
	private IDisposable? textStreamSubscription = null;
	private InlineCollection? inlines = null;
	private IDisposable? inlinesSubscription = null;
	private readonly Subject<bool> isReady = new ();

	/// <inheritdoc />
	public MiniLog()
	{
		PropertyChanged += (s, propertyChangedEventArgs) =>
		{
			if (propertyChangedEventArgs.Property == TextStreamProperty)
			{
				UpdateTextBlockInlines();
			}
		};
	}

	/// <summary>
	///     This is the recommended way to manipulate the long contents.
	/// </summary>
	/// <example>
	///     Create and export the IObservable from a View Model, where you can push values:
	///     <code>
	///  	public class ViewModel
	///  	{
	/// 			...
	///  	
	/// 			private ReplaySubject&lt;Inline&gt; outputStream = new (2);
	/// 			public IObservable&lt;Inline&gt; OutputStream => outputStream;
	///  	
	/// 			...
	///  	
	/// 			// push values
	/// 			outputStream.OnNext(new Run("Hello World"));
	///  	
	/// 			...
	///  	}
	///  	</code>
	///     Bind the IObservable from the View Model to the control in XAML:
	///     <code>
	///  	&lt;controls:MiniLog TextStream="{Binding OutputStream}"/&gt;
	///  	</code>
	/// </example>
	public IObservable<Inline>? TextStream
	{
		get => textStream;
		set => SetAndRaise(TextStreamProperty, ref textStream, value);
	}

	/// <summary>
	///     Gets or sets the inlines. This is needed to be able to add content in XAML.
	///     Getting this value will override the <see cref="TextStream" />.
	/// </summary>
	[Content]
	public InlineCollection Inlines
	{
		get
		{
			if (inlines != null)
			{
				return inlines;
			}

			var subject = new Subject<Inline>();
			TextStream = subject;
			inlines = new InlineCollection();

			inlinesSubscription = inlines.ToObservableChangeSet<InlineCollection, Inline>()
				.WhereReasonsAre(ListChangeReason.Add)
				.Subscribe(change =>
				{
					foreach (var item in change)
					{
						subject.OnNext(item.Item.Current);
					}
				});

			return inlines;
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		textBlock = e.NameScope.Find<TextBlock>("PART_TextBlock");
		isReady.OnNext(true);
		isReady.OnCompleted();
	}

	private void UpdateTextBlockInlines()
	{
		inlinesSubscription?.Dispose();
		inlines = null;
		textBlock?.Inlines?.Clear();

		textStreamSubscription?.Dispose();

		if (textStream != null)
		{
			var accumulatedLineBreaks = new Queue<SmartLineBreak>();
			textStreamSubscription = textStream
				.BufferWithToggle(isReady)
				.Subscribe(inline =>
				{
					// buffer line breaks
					if (inline is SmartLineBreak smartLineBreak)
					{
						accumulatedLineBreaks.Enqueue(smartLineBreak);
						return;
					}

					// flush previous line breaks
					foreach (SmartLineBreak lineBreak in accumulatedLineBreaks)
					{
						textBlock?.Inlines?.Add(lineBreak);
					}
					accumulatedLineBreaks.Clear();

					textBlock?.Inlines?.Add(inline);
				});
		}
	}
}