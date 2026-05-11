using Reqnroll;
using Reqnroll.Plugins;
using Reqnroll.UnitTestProvider;
using TestTrackingDiagrams.ReqNRoll;

[assembly: RuntimePlugin(typeof(AttachmentCapturingPlugin))]

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Reqnroll runtime plugin that wraps the <see cref="IReqnrollOutputHelper"/>
/// with <see cref="AttachmentCapturingOutputHelper"/> to automatically capture
/// attachments into the test tracking diagram report.
/// </summary>
public class AttachmentCapturingPlugin : IRuntimePlugin
{
    public void Initialize(
        RuntimePluginEvents runtimePluginEvents,
        RuntimePluginParameters runtimePluginParameters,
        UnitTestProviderConfiguration unitTestProviderConfiguration)
    {
        runtimePluginEvents.CustomizeTestThreadDependencies += (_, args) =>
        {
            // Resolve the original before replacing the registration
            var original = args.ObjectContainer.Resolve<IReqnrollOutputHelper>();
            args.ObjectContainer.RegisterInstanceAs<IReqnrollOutputHelper>(
                new AttachmentCapturingOutputHelper(original));
        };
    }
}
