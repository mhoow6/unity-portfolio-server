using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class SessionManager
    {
        public static SessionManager Instance { get; } = new SessionManager();

        // 클라와 만든 서버와의 연결 세션들
        List<ServerSession> serverSessions = new List<ServerSession>();

        // 서버가 만든 클라와의 연결 세션들
        List<ClientSession> clientSessions = new List<ClientSession>();
        int sessionId = 0;

        Object _lock = new object();

        /// <summary>
        /// 한 클라이언트가 서버와 연결된 세션에게 패킷을 보내는데 사용되는 테스트 함수
        /// </summary>
        public void ClientSendForEach()
        {
            lock (_lock)
            {
                foreach (ServerSession session in serverSessions)
                {
                    C_Chat chat = new C_Chat(1000, "sg");
                    session.Send(chat.Write());
                }
            }
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

                session.sessionId = ++this.sessionId;

                clientSessions.Add(session);
                return session;
            }
        }
    }
}
