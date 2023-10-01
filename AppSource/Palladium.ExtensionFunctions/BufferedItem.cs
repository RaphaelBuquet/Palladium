namespace Palladium.ExtensionFunctions;

public struct BufferedItem<T>
{
	public T Value;
	public Exception? Exception;
	public bool IsCompleted;
}