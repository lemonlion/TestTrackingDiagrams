namespace TestTrackingDiagrams.Tracking;

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