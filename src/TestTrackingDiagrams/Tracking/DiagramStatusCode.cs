using System.Net;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// A named wrapper for <see cref="OneOf{TOption1, TOption2}"/> that represents a diagram
/// status code — either an <see cref="HttpStatusCode"/> or a <c>string</c> label
/// (e.g. <c>"Hit"</c>, <c>"Miss"</c>, <c>"Sent"</c>).
/// <para>
/// Use this type instead of <c>OneOf&lt;HttpStatusCode, string&gt;</c> when your project also
/// references the popular <c>OneOf</c> NuGet package, to avoid the
/// <c>CS0104: 'OneOf&lt;,&gt;' is an ambiguous reference</c> compiler error.
/// </para>
/// <example>
/// <code>
/// DiagramStatusCode status = HttpStatusCode.OK;                  // from HttpStatusCode
/// DiagramStatusCode status = "Hit";                              // from string
/// OneOf&lt;HttpStatusCode, string&gt; oneOf = status;            // assignment-compatible
/// </code>
/// </example>
/// </summary>
public class DiagramStatusCode : OneOf<HttpStatusCode, string>
{
    private DiagramStatusCode(object? value) : base(value) { }

    public static implicit operator DiagramStatusCode(HttpStatusCode value) => new(value);
    public static implicit operator DiagramStatusCode(string value) => new(value);
}
