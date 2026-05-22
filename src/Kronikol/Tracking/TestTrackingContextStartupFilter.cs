using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Kronikol.Tracking;

/// <summary>
/// An <see cref="IStartupFilter"/> that automatically registers <see cref="TestTrackingContextMiddleware"/>
/// at the beginning of the request pipeline. This ensures test identity headers are propagated
/// into <see cref="TestIdentityScope.Current"/> before any request handling occurs, enabling
/// background tasks (Task.Run, fire-and-forget) to inherit test context via AsyncLocal.
/// <para>
/// Register via <c>services.AddTestTrackingContextPropagation()</c>.
/// </para>
/// </summary>
public class TestTrackingContextStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<TestTrackingContextMiddleware>();
            next(app);
        };
    }
}
