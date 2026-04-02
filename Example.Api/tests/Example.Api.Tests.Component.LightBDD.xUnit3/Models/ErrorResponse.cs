namespace Example.Api.Tests.Component.LightBDD.xUnit3.Models;

public class ErrorResponse
{
    public required string Type { get; set; }
    public required string Title { get; set; }
    public int Status { get; set; }
    public required Dictionary<string, string[]> Errors { get; set; }
    public required string TraceId { get; set; }
}
