using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class PacketHandler
    {
        public static void C_ChatHandler(Session session, ArraySegment<byte> buffer)
        {
            S_Chat chat = new S_Chat();
            chat.Read(buffer);

            ArraySegment<byte> sendBuffer = chat.Write();

            SessionManager.Instance.Find(session.sessionId).Send(sendBuffer);
        }

        public static void S_ChatHandler(Session session, ArraySegment<byte> buffer)
        {
            C_Chat chat = new C_Chat();
            chat.Read(buffer);

            Console.WriteLine($"[전체채팅] {chat.playerId}: {chat.chat}");
        }
    }
}
