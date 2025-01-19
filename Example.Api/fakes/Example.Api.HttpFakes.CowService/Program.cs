namespace Example.Api.HttpFakes.CowService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer().AddSwaggerGen();

        var app = builder.Build();
        app.UseSwagger().UseSwaggerUI().UseHttpsRedirection();

        app.MapGet("/milk", () => new { Milk = "Some_Fresh_Milk" });

        app.Run();
    }
}