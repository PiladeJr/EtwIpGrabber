namespace EtwIpGrabber.TcpLifeCycleReconstruction.Models
{
    internal readonly struct TcpCommunityIdKey
    {
        public readonly uint IpA;
        public readonly ushort PortA;

        public readonly uint IpB;
        public readonly ushort PortB;

        public TcpCommunityIdKey(
            uint localIp,
            ushort localPort,
            uint remoteIp,
            ushort remotePort)
        {
            if (Compare(localIp, localPort, remoteIp, remotePort) <= 0)
            {
                IpA = localIp;
                PortA = localPort;
                IpB = remoteIp;
                PortB = remotePort;
            }
            else
            {
                IpA = remoteIp;
                PortA = remotePort;
                IpB = localIp;
                PortB = localPort;
            }
        }

        private static int Compare(
            uint ip1,
            ushort port1,
            uint ip2,
            ushort port2)
        {
            int ipCmp = ip1.CompareTo(ip2);
            return ipCmp != 0
                ? ipCmp
                : port1.CompareTo(port2);
        }
    }
}

