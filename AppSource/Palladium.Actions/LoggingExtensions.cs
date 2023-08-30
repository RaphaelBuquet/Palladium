using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Microsoft.Extensions.Logging;
using Palladium.Controls;
using MiniLog = Palladium.Logging.MiniLog;

namespace Palladium.ActionsService;

public static class LoggingExtensions
{
	public static IObservable<Inline> AdaptForControl(this MiniLog miniLog)
	{
		return Observable.Create<Inline>(observer =>
		{
			var disposables = new CompositeDisposable();
			var successBrush = new DynamicResourceExtension("Validation-Success");
			var failureBrush = new DynamicResourceExtension("Validation-Error");

			miniLog.Success.Subscribe(b =>
			{
				var inline = new Run(b ? "Operation completed successfully" : "Operation failed");
				// defer resolution of value
				inline.Bind(TextElement.ForegroundProperty, b ? successBrush : failureBrush);
				observer.OnNext(inline);
				observer.OnNext(SmartLineBreak.Instance);
			}).DisposeWith(disposables);

			miniLog.Entries.Subscribe(entry =>
			{
				var inline = new Run();
				if (entry.LogLevel != LogLevel.Information)
				{
					inline.Text = $"{entry.LogLevel}: {entry.Message}";
				}
				else
				{
					inline.Text = entry.Message;
				}

				if (entry.LogLevel == LogLevel.Critical || entry.LogLevel == LogLevel.Error)
				{
					inline.Bind(TextElement.ForegroundProperty, failureBrush);
				}

				observer.OnNext(inline);
				observer.OnNext(SmartLineBreak.Instance);
			}).DisposeWith(disposables);

			return disposables;
		});
	}
}