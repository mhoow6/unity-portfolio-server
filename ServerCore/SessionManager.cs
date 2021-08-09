using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        // 1 클라 -> 1 서버세션
        // 단일 클라이언트는 하나의 서버세션만을 갖는다.
        List<ServerSession> serverSessions = new List<ServerSession>();

        // 1 서버 -> n 클라세션
        Dictionary<int, ClientSession> clientSessions = new Dictionary<int, ClientSession>();
        List<ArraySegment<byte>> pendingBuffs = new List<ArraySegment<byte>>();
        int _sessionId = 0;

        Object _lock = new object();

        /// <summary>
        /// 한 클라이언트가 서버와 연결된 세션에게 패킷을 보내는데 사용되는 테스트 함수
        /// </summary>
        public void ClientSendForEach()
        {
            uint fakeID = 1000;

            lock (_lock)
            {
                foreach (ServerSession session in serverSessions)
                {
                    C_Chat chat = new C_Chat();
                    chat.playerId = fakeID++;
                    chat.chat = "sg";

                    session.Send(chat.Write());
                }
            }
        }

        /// <summary>
        /// 서버와 연결된 클라이언트 세션 체크용 함수
        /// </summary>
        public void CheckSessions()
        {
            foreach (KeyValuePair<int, ClientSession> session in clientSessions)
                Console.WriteLine($"서버는 [세션:{session.Value.sessionId}] 유지중.");
        }

        /// <summary>
        /// 서버와 연결된 클라이언트 세션을 딕셔너리에서 제거합니다.
        /// </summary>
        /// <param name="session">제거할 세션</param>
        public void Kick(Session session)
        {
            lock (_lock)
            {
                if (clientSessions.TryGetValue(session.sessionId, out _))
                    clientSessions.Remove(session.sessionId);
            }    
        }

        public ClientSession Find(int sessionId)
        {
            return clientSessions[sessionId];
        }

        /// <summary>
        /// 클라이언트가 서버와의 연결고리인 세션을 만듭니다.
        /// </summary>
        /// <returns></returns>
        public ServerSession MakeServerSession()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();

                serverSessions.Add(session);
                return session;
            }
        }

        /// <summary>
        /// 서버가 클라이언트와의 연결고리인 세션을 만듭니다.
        /// </summary>
        /// <returns></returns>
        public ClientSession MakeClientSession()
        {
            lock (_lock)
            {
                ClientSession session = new ClientSession();

                session.sessionId = ++_sessionId;

                clientSessions.Add(session.sessionId, session);
                return session;
            }
        }
    }
}
