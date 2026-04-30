namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// A named wrapper for <see cref="OneOf{TOption1, TOption2}"/> that represents a diagram
/// method label — either an <see cref="HttpMethod"/> (for standard HTTP operations) or a
/// <c>string</c> (for custom labels like <c>"Blob Upload"</c>, <c>"Cache Get (Hit)"</c>).
/// <para>
/// Use this type instead of <c>OneOf&lt;HttpMethod, string&gt;</c> when your project also
/// references the popular <c>OneOf</c> NuGet package, to avoid the
/// <c>CS0104: 'OneOf&lt;,&gt;' is an ambiguous reference</c> compiler error.
/// </para>
/// <example>
/// <code>
/// DiagramMethod method = "Blob Upload";            // from string
/// DiagramMethod method = HttpMethod.Post;           // from HttpMethod
/// OneOf&lt;HttpMethod, string&gt; oneOf = method;  // assignment-compatible
/// </code>
/// </example>
/// </summary>
public class DiagramMethod : OneOf<HttpMethod, string>
{
    private DiagramMethod(object? value) : base(value) { }

    public static implicit operator DiagramMethod(HttpMethod value) => new(value);
    public static implicit operator DiagramMethod(string value) => new(value);
}
