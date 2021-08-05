using System;
using System.Net;

namespace ServerCore
{
    public class Container
    {
        public static Container Instance { get; } = new Container();
        public IPEndPoint host { get => _host; }
        public int backlog { get => BACKLOG; }

        const int PORT = 7777;
        const int BACKLOG = 100;
        IPEndPoint _host;

        Container()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            IPAddress hostIP = hostEntry.AddressList[0];
            _host = new IPEndPoint(hostIP, PORT);
        }
    }
}
