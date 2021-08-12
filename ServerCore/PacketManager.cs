using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class PacketManager
    {
        public static PacketManager Instance { get; } = new PacketManager();

        byte[] bPacketID = new byte[sizeof(ushort)];
        Dictionary<ushort, Action<Session, ArraySegment<byte>>> handler = new Dictionary<ushort, Action<Session, ArraySegment<byte>>>();

        PacketManager()
        {
            Init();
        }

        void Init()
        {
            handler.Add((ushort)PacketID.C_Chat, PacketHandler.C_ChatHandler);
            handler.Add((ushort)PacketID.S_Chat, PacketHandler.S_ChatHandler);
            handler.Add((ushort)PacketID.C_FileRequest, PacketHandler.C_FileRequestHandler);
            handler.Add((ushort)PacketID.S_FileResponse, PacketHandler.S_FileResponseHandler);
        }

        
        public void OnRecvPacket(Session session, ArraySegment<byte> buffer)
        {
            int count = 0;

            // 패킷 길이 스킵
            count += sizeof(int);

            // 패킷 ID 파싱
            Array.Copy(buffer.Array, buffer.Offset + count, bPacketID, 0, sizeof(ushort));
            ushort packetID = BitConverter.ToUInt16(bPacketID);

            // 패킷 ID에 따라 Init에서 지정한 함수 호출
            Action<Session, ArraySegment<byte>> action;
            if (handler.TryGetValue(packetID, out action))
                action.Invoke(session, buffer);
        }  
    }
}
