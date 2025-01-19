namespace Example.Api.Responses;

public class CakeResponse
{
    public Guid BatchId { get; set; } = Guid.NewGuid();
    public string[] Ingredients { get; set; } = {};
}