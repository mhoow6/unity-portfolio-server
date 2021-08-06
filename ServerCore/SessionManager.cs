using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        List<ServerSession> serverSessions = new List<ServerSession>();
        List<ClientSession> clientSessions = new List<ClientSession>();

        Object _lock = new object();

        public ServerSession MakeServerSession()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();
                serverSessions.Add(session);
                return session;
            }
        }

        public ClientSession MakeSessionWithClient()
        {
            lock (_lock)
            {
                ClientSession session = new ClientSession();
                clientSessions.Add(session);
                return session;
            }
        }
    }
}
