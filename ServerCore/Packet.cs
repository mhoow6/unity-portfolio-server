using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    // C_ 가 붙은 건 클라이언트에서 서버로 보내는 패킷, S_는 서버에서 클라이언트로 보내는 패킷
    public enum PacketID
    {
        C_Chat = 1,
        S_Chat
    }

    // size  id   data
    // ----|----|-----  ...  |
    // [][] [][] [][][] ...  |
    public abstract class Packet
    {
        protected ushort size;
        protected ushort packetId;
        protected abstract void Read(ArraySegment<byte> buffer);
    }

    public class C_Chat : Packet
    {
        int playerId;
        string chat;

        C_Chat(ArraySegment<byte> segment)
        {
            packetId = (ushort)PacketID.C_Chat;

            Read(segment);
        }

        protected sealed override void Read(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }

}
