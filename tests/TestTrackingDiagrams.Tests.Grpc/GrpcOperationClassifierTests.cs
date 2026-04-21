using Grpc.Core;
using TestTrackingDiagrams.Extensions.Grpc;

namespace TestTrackingDiagrams.Tests.Grpc;

public class GrpcOperationClassifierTests
{
    // ─── Classification ─────────────────────────────────────

    [Fact]
    public void Classify_Unary_ReturnsUnaryCall()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.Unary, "/greet.Greeter/SayHello", "greet.Greeter", "SayHello");

        Assert.Equal(GrpcOperation.UnaryCall, result.Operation);
    }

    [Fact]
    public void Classify_ServerStreaming_ReturnsServerStreamingCall()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.ServerStreaming, "/chat.ChatService/StreamMessages", "chat.ChatService", "StreamMessages");

        Assert.Equal(GrpcOperation.ServerStreamingCall, result.Operation);
    }

    [Fact]
    public void Classify_ClientStreaming_ReturnsClientStreamingCall()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.ClientStreaming, "/upload.UploadService/Upload", "upload.UploadService", "Upload");

        Assert.Equal(GrpcOperation.ClientStreamingCall, result.Operation);
    }

    [Fact]
    public void Classify_DuplexStreaming_ReturnsDuplexStreamingCall()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.DuplexStreaming, "/chat.ChatService/Chat", "chat.ChatService", "Chat");

        Assert.Equal(GrpcOperation.DuplexStreamingCall, result.Operation);
    }

    // ─── Info fields ───────────────────────────────────────

    [Fact]
    public void Classify_FullMethodName_Preserved()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.Unary, "/greet.Greeter/SayHello", "greet.Greeter", "SayHello");

        Assert.Equal("/greet.Greeter/SayHello", result.FullMethodName);
    }

    [Fact]
    public void Classify_ServiceName_Preserved()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.Unary, "/greet.Greeter/SayHello", "greet.Greeter", "SayHello");

        Assert.Equal("greet.Greeter", result.ServiceName);
    }

    [Fact]
    public void Classify_MethodName_Preserved()
    {
        var result = GrpcOperationClassifier.Classify(
            MethodType.Unary, "/greet.Greeter/SayHello", "greet.Greeter", "SayHello");

        Assert.Equal("SayHello", result.MethodName);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Raw_IncludesFullNameAndType()
    {
        var op = new GrpcOperationInfo(GrpcOperation.UnaryCall, "greet.Greeter", "SayHello", "/greet.Greeter/SayHello");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Raw);

        Assert.Equal("/greet.Greeter/SayHello [UnaryCall]", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_UnaryReturnsMethodName()
    {
        var op = new GrpcOperationInfo(GrpcOperation.UnaryCall, "greet.Greeter", "SayHello", "/greet.Greeter/SayHello");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Detailed);

        Assert.Equal("SayHello", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ServerStreamingIncludesAnnotation()
    {
        var op = new GrpcOperationInfo(GrpcOperation.ServerStreamingCall, "chat.Chat", "StreamMessages", "/chat.Chat/StreamMessages");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Detailed);

        Assert.Equal("StreamMessages (server-stream)", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_ClientStreamingIncludesAnnotation()
    {
        var op = new GrpcOperationInfo(GrpcOperation.ClientStreamingCall, "upload.Upload", "Upload", "/upload.Upload/Upload");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Detailed);

        Assert.Equal("Upload (client-stream)", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_DuplexStreamingIncludesAnnotation()
    {
        var op = new GrpcOperationInfo(GrpcOperation.DuplexStreamingCall, "chat.Chat", "Chat", "/chat.Chat/Chat");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Detailed);

        Assert.Equal("Chat (duplex-stream)", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReturnsMethodNameOnly()
    {
        var op = new GrpcOperationInfo(GrpcOperation.UnaryCall, "greet.Greeter", "SayHello", "/greet.Greeter/SayHello");

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Summarised);

        Assert.Equal("SayHello", label);
    }

    [Fact]
    public void GetDiagramLabel_NullMethodName_DefaultsToCall()
    {
        var op = new GrpcOperationInfo(GrpcOperation.UnaryCall, null, null, null);

        var label = GrpcOperationClassifier.GetDiagramLabel(op, GrpcTrackingVerbosity.Summarised);

        Assert.Equal("Call", label);
    }

    // ─── Status code mapping ──────────────────────────────

    [Theory]
    [InlineData(StatusCode.OK, System.Net.HttpStatusCode.OK)]
    [InlineData(StatusCode.NotFound, System.Net.HttpStatusCode.NotFound)]
    [InlineData(StatusCode.PermissionDenied, System.Net.HttpStatusCode.Forbidden)]
    [InlineData(StatusCode.Unauthenticated, System.Net.HttpStatusCode.Unauthorized)]
    [InlineData(StatusCode.InvalidArgument, System.Net.HttpStatusCode.BadRequest)]
    [InlineData(StatusCode.DeadlineExceeded, System.Net.HttpStatusCode.RequestTimeout)]
    [InlineData(StatusCode.AlreadyExists, System.Net.HttpStatusCode.Conflict)]
    [InlineData(StatusCode.Unavailable, System.Net.HttpStatusCode.ServiceUnavailable)]
    [InlineData(StatusCode.Unimplemented, System.Net.HttpStatusCode.NotImplemented)]
    [InlineData(StatusCode.Internal, System.Net.HttpStatusCode.InternalServerError)]
    public void MapGrpcStatusToHttp_MapsCorrectly(StatusCode grpcStatus, System.Net.HttpStatusCode expected)
    {
        var result = GrpcTrackingInterceptor.MapGrpcStatusToHttp(grpcStatus);
        Assert.Equal(expected, result);
    }
}
