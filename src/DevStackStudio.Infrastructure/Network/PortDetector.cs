using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DevStackStudio.Infrastructure.Network;

public static class PortDetector
{
    public static bool IsPortInUse(int port)
    {
        if (port <= 0 || port > 65535)
        {
            return true;
        }

        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            if (tcpListeners.Any(endpoint => endpoint.Port == port))
            {
                return true;
            }

            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();
            if (tcpConnections.Any(conn => conn.LocalEndPoint.Port == port))
            {
                return true;
            }
        }
        catch
        {
            // If network inspection is restricted, try socket binding
        }

        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            return false;
        }
        catch
        {
            return true;
        }
    }

    public static int FindNextAvailablePort(int startPort, params int[] reservedPorts)
    {
        var reserved = new HashSet<int>(reservedPorts);
        for (var port = startPort; port <= 65535; port++)
        {
            if (reserved.Contains(port))
            {
                continue;
            }

            if (!IsPortInUse(port))
            {
                return port;
            }
        }

        return startPort;
    }
}
