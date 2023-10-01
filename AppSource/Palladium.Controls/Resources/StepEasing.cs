using Avalonia.Animation.Easings;

namespace Palladium.Controls.Resources;

public class StepEasing : Easing
{
	/// <inheritdoc />
	public override double Ease(double progress)
	{
		return Math.Floor(progress * 8.0) / 8.0;
	}
}