using Reqnroll;
using Kronikol.Tracking;

namespace Kronikol.ReqNRoll;

/// <summary>
/// Decorator for <see cref="IReqnrollOutputHelper"/> that automatically captures
/// attachments added via <see cref="IReqnrollOutputHelper.AddAttachment"/>
/// into the test tracking diagram report.
/// </summary>
internal class AttachmentCapturingOutputHelper : IReqnrollOutputHelper
{
    private readonly IReqnrollOutputHelper _inner;

    public AttachmentCapturingOutputHelper(IReqnrollOutputHelper inner)
    {
        _inner = inner;
    }

    public void AddAttachment(string filePath)
    {
        Track.Attachment(filePath);
        _inner.AddAttachment(filePath);
    }

    public void WriteLine(string message) => _inner.WriteLine(message);
    public void WriteLine(string format, params object[] args) => _inner.WriteLine(format, args);
}
