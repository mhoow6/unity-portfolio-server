using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class PacketManager
    {
        public static PacketManager Instance { get; } = new PacketManager();

        byte[] bPacketID = new byte[sizeof(ushort)];
        Dictionary<ushort, Action<Session, Packet>> handler = new Dictionary<ushort, Action<Session, Packet>>();

        PacketManager()
        {
            Init();
        }

        void Init()
        {
            handler.Add((ushort)PacketID.C_Chat, PacketHandler.C_ChatHandler);
        }

        
        public void OnRecvPacket(ClientSession session, ArraySegment<byte> buffer)
        {
            ushort count = 0;

            count += sizeof(ushort); // skip header-size

            // 패킷 ID 파싱
            Array.Copy(buffer.Array, buffer.Offset + count, bPacketID, 0, sizeof(ushort));
            ushort packetID = BitConverter.ToUInt16(bPacketID);
            count += sizeof(ushort);

            Packet pkt = null;

            switch (packetID)
            {
                case (ushort)PacketID.C_Chat:
                    pkt = new C_Chat();  
                    break;
            }

            if (pkt != null)
            {
                pkt.Read(buffer);

                Action<Session, Packet> action;
                if (handler.TryGetValue((ushort)PacketID.C_Chat, out action))
                    action.Invoke(session, pkt);
            }
        }  
    }
}
