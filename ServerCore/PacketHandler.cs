using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class PacketHandler
    {
        public static void C_ChatHandler(Session session, Packet packet)
        {
            C_Chat pkt = packet as C_Chat;

            Console.WriteLine($"{pkt.PlayerId} 에서 온 채팅내용: {pkt.Chat}");
        }
    }
}
