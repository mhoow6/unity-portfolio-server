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

    // -------- Header --------
    //  packetsize   packetId
    // | -------- | ----------|
    //     [][]        [][]       
    public abstract class Packet
    {
        protected ushort size;
        protected ushort packetId;
        protected int DEFAULT_RESERVE_SIZE { get => 4096; }
        public abstract void Read(ArraySegment<byte> buffer);
        public abstract ArraySegment<byte> Write();
    }

    // -------- DATA --------
    // playerId      chat
    // -------- | ------ ... |
    // [][][][]   [][][] ... |
    public class C_Chat : Packet
    {
        public string Chat { get => chat; }
        public uint PlayerId { get => playerId; }

        string chat;
        uint playerId;
        public C_Chat(uint playerId = 0, string chat = "")
        {
            packetId = (ushort)PacketID.C_Chat;
            this.playerId = playerId; // Test
            this.chat = chat; // Test
        }

        public sealed override void Read(ArraySegment<byte> buffer)
        {
            int count = 0;

            count += sizeof(ushort); // skip size
            count += sizeof(ushort); // skip packetID

            this.playerId = BitConverter.ToUInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(uint);

            this.chat = BitConverter.ToString(buffer.Array, buffer.Offset + count);
            count += this.chat.Length;
        }

        public sealed override ArraySegment<byte> Write()
        {
            ushort count = 0;
            ArraySegment<byte> reserveBuffer = SendBufferHelper.Reserve(DEFAULT_RESERVE_SIZE);

            // size count
            count += sizeof(ushort); 

            // packetID
            byte[] packetID = BitConverter.GetBytes((ushort)this.packetId);
            Array.Copy(packetID, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(ushort));
            count += sizeof(ushort);

            // playerID
            byte[] bPlayerid = BitConverter.GetBytes(this.playerId);
            Array.Copy(bPlayerid, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(uint));
            count += sizeof(uint);

            // chat
            byte[] bChat = Encoding.Default.GetBytes(this.chat);
            Array.Copy(bChat, 0, reserveBuffer.Array, reserveBuffer.Offset + count, bChat.Length);
            count += (ushort)bChat.Length;

            // size
            byte[] size = BitConverter.GetBytes(count);
            Array.Copy(size, 0, reserveBuffer.Array, reserveBuffer.Offset, sizeof(ushort));

            ArraySegment<byte> sendBuffer = SendBufferHelper.Close(count);

            return sendBuffer;
        }
    }

}
