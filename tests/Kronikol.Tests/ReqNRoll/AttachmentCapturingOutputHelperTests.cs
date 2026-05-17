using Reqnroll;
using Kronikol.ReqNRoll;

namespace Kronikol.Tests.ReqNRoll;

public class AttachmentCapturingOutputHelperTests
{
    [Fact]
    public void AddAttachment_delegates_to_inner()
    {
        var inner = new FakeOutputHelper();
        var wrapper = new AttachmentCapturingOutputHelper(inner);

        wrapper.AddAttachment("/tmp/file.txt");

        Assert.Single(inner.Attachments);
        Assert.Equal("/tmp/file.txt", inner.Attachments[0]);
    }

    [Fact]
    public void AddAttachment_calls_Track_Attachment()
    {
        using var scope = Kronikol.Tracking.TestIdentityScope.Begin(
            "AttachmentTest", "AttachmentTest");

        var inner = new FakeOutputHelper();
        var wrapper = new AttachmentCapturingOutputHelper(inner);

        Kronikol.Tracking.StepCollector.StartStep(
            "AttachmentTest", "When", "I do something", null, null);
        wrapper.AddAttachment("/tmp/screenshot.png");
        Kronikol.Tracking.StepCollector.CompleteStep(
            "AttachmentTest", passed: true);

        var steps = Kronikol.Tracking.StepCollector.GetSteps("AttachmentTest");
        Assert.Single(steps);
        Assert.NotNull(steps[0].Attachments);
        Assert.Single(steps[0].Attachments);
        Assert.Equal("screenshot.png", steps[0].Attachments[0].Name);
    }

    [Fact]
    public void WriteLine_delegates_to_inner()
    {
        var inner = new FakeOutputHelper();
        var wrapper = new AttachmentCapturingOutputHelper(inner);

        wrapper.WriteLine("Hello");

        Assert.Single(inner.Lines);
        Assert.Equal("Hello", inner.Lines[0]);
    }

    [Fact]
    public void WriteLine_with_format_delegates_to_inner()
    {
        var inner = new FakeOutputHelper();
        var wrapper = new AttachmentCapturingOutputHelper(inner);

        wrapper.WriteLine("Hello {0}", "World");

        Assert.Single(inner.FormattedLines);
        Assert.Equal("Hello {0}", inner.FormattedLines[0].Format);
        Assert.Equal(new object[] { "World" }, inner.FormattedLines[0].Args);
    }

    private class FakeOutputHelper : IReqnrollOutputHelper
    {
        public List<string> Attachments { get; } = new();
        public List<string> Lines { get; } = new();
        public List<(string Format, object[] Args)> FormattedLines { get; } = new();

        public void AddAttachment(string filePath) => Attachments.Add(filePath);
        public void WriteLine(string message) => Lines.Add(message);
        public void WriteLine(string format, params object[] args) => FormattedLines.Add((format, args));
    }
}
