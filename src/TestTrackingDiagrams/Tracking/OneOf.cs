namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// A discriminated union that holds a value of either <typeparamref name="TOption1"/> or <typeparamref name="TOption2"/>.
/// Used in <see cref="RequestResponseLog"/> to represent values that can be multiple types
/// (e.g. <see cref="System.Net.Http.HttpMethod"/> or a string method name).
/// </summary>
/// <typeparam name="TOption1">The first possible type.</typeparam>
/// <typeparam name="TOption2">The second possible type.</typeparam>
public class OneOf<TOption1, TOption2>
{
    protected OneOf(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public static implicit operator OneOf<TOption1, TOption2>(TOption1 value) => new(value);

    public static implicit operator OneOf<TOption1, TOption2>(TOption2 value) => new(value);
}