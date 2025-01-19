using System.Net;
using System.Net.Sockets;

namespace Example.Api.Tests.Component.Shared.HttpFakes;

public static class PortChecker
{
    public static void AssertPortIsNotInUse(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new Exception($"Url '{url}' is an invalid format.");

        var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];

        try
        {
            using var tcpListener = new TcpListener(ipAddress, uri.Port);
            tcpListener.Start();
            tcpListener.Stop();
        }
        catch (SocketException)
        {
            throw new Exception($"Url '{url}' is currently in use, and cannot be bound. =");
        }
    }
}