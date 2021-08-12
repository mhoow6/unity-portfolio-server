using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    // C_ 가 붙은 건 클라이언트에서 서버로 보내는 패킷, S_는 서버에서 클라이언트로 보내는 패킷
    public enum PacketID
    {
        C_Chat = 1,
        S_Chat,
        C_FileRequest,
        S_FileResponse
    }

    // -------- Header --------
    //  packetsize   packetId
    // | -------- | ----------|
    //   [][][][]        [][]       
    public abstract class Packet
    {
        protected int size;
        protected ushort packetId;
        protected int DEFAULT_RESERVE_SIZE { get => 4096; }
        public abstract void Read(ArraySegment<byte> buffer);
        public abstract ArraySegment<byte> Write();
    }

    // -------- DATA --------
    // playerId      chat
    // -------- | ------ ... |
    // [][][][]   [][][] ... |
    public abstract class ChatPacket : Packet
    {
        public string chat;
        public uint playerId;

        public override void Read(ArraySegment<byte> buffer)
        {
            int count = 0;

            count += sizeof(ushort); // skip size
            count += sizeof(ushort); // skip packetID

            this.playerId = BitConverter.ToUInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(uint);

            unsafe
            {
                fixed (byte* ptr = &buffer.Array[buffer.Offset + count])
                    this.chat = Encoding.Default.GetString(ptr, buffer.Count - count);
            }

            count += this.chat.Length;
        }

        public override ArraySegment<byte> Write()
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

    public class C_Chat : ChatPacket
    {
        public C_Chat()
        {
            packetId = (ushort)PacketID.C_Chat;
        }
    }

    public class S_Chat : ChatPacket
    {
        public S_Chat()
        {
            packetId = (ushort)PacketID.S_Chat;
        }
    }

    public abstract class FilePacket : Packet
    {
        public string fileName;
        public int fileNameLength;
    }

    // ------------------ DATA -----------------
    //     seek     fileNameLength    fileName
    // | -------- | -------------- | ---------- |
    //   [][][][]      [][][][] 
    public class C_FileRequest : FilePacket
    {
        public int seek;

        public C_FileRequest()
        {
            packetId = (ushort)PacketID.C_FileRequest;
        }

        public C_FileRequest(string _fileName, int _seek = 0)
        {
            packetId = (ushort)PacketID.C_FileRequest;
            seek = _seek;
            fileName = _fileName;
            fileNameLength = _fileName.Length;    
        }

        public sealed override void Read(ArraySegment<byte> buffer)
        {
            int count = 0;

            // size
            this.size = BitConverter.ToInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // skip packetID
            count += sizeof(ushort);

            // seek
            this.seek = BitConverter.ToInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // fileNameLength
            this.fileNameLength = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // fileName
            unsafe
            {
                fixed (byte* ptr = &buffer.Array[buffer.Offset + count])
                    this.fileName = Encoding.Default.GetString(ptr, this.fileNameLength);
            }
            count += this.fileNameLength;
        }

        public sealed override ArraySegment<byte> Write()
        {
            int count = 0;
            ArraySegment<byte> reserveBuffer = SendBufferHelper.Reserve(DEFAULT_RESERVE_SIZE);

            // size count
            count += sizeof(int);

            // packetID
            byte[] packetID = BitConverter.GetBytes((ushort)this.packetId);
            Array.Copy(packetID, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(ushort));
            count += sizeof(ushort);

            // seek
            byte[] bSeek = BitConverter.GetBytes(this.seek);
            Array.Copy(bSeek, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(int));
            count += sizeof(int);

            // fileNameLength
            byte[] bFileNameLength = BitConverter.GetBytes(this.fileNameLength);
            Array.Copy(bFileNameLength, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(int));
            count += sizeof(int);

            // fileName
            byte[] bFileName = Encoding.Default.GetBytes(this.fileName);
            Array.Copy(bFileName, 0, reserveBuffer.Array, reserveBuffer.Offset + count, this.fileNameLength);
            count += this.fileNameLength;

            // size
            byte[] size = BitConverter.GetBytes(count);
            Array.Copy(size, 0, reserveBuffer.Array, reserveBuffer.Offset, sizeof(int));

            ArraySegment<byte> sendBuffer = SendBufferHelper.Close(count);

            return sendBuffer;
        }
    }

    // ----------------------- DATA ----------------------------
    //   originSize    fileNameLength    fileName     fileByte
    // | ---------- | ---------------| ---------- | ---------- |
    //    [][][][]       [][][][]           
    public class S_FileResponse : FilePacket
    {
        public int originSize;
        public byte[] file;

        public S_FileResponse()
        {
            packetId = (ushort)PacketID.S_FileResponse;
        }

        public S_FileResponse(string _fileName, ArraySegment<byte> _file)
        {
            packetId = (ushort)PacketID.S_FileResponse;
            fileName = _fileName;
            fileNameLength = _fileName.Length;
            originSize = _file.Array.Length;
            file = new byte[_file.Count];
            Array.Copy(_file.Array, _file.Offset, file, 0, _file.Count);
        }

        public sealed override void Read(ArraySegment<byte> buffer)
        {
            int count = 0;

            // size
            this.size = BitConverter.ToInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // skip packetID
            count += sizeof(ushort);

            // originSize & file Alloc
            this.originSize = BitConverter.ToInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // fileNameLength
            this.fileNameLength = BitConverter.ToInt32(buffer.Array, buffer.Offset + count);
            count += sizeof(int);

            // fileName
            unsafe
            {
                fixed (byte* ptr = &buffer.Array[buffer.Offset + count])
                    this.fileName = Encoding.Default.GetString(ptr, fileNameLength);
            }
            count += fileNameLength;

            // file
            file = new byte[buffer.Count - count];
            Array.Copy(buffer.Array, buffer.Offset + count, file, 0, buffer.Count - count);
            count += buffer.Count - count;
        }

        public sealed override ArraySegment<byte> Write()
        {
            int count = 0;
            ArraySegment<byte> reserveBuffer = SendBufferHelper.Reserve(DEFAULT_RESERVE_SIZE);

            // size count
            count += sizeof(int);

            // packetID
            byte[] packetID = BitConverter.GetBytes((ushort)this.packetId);
            Array.Copy(packetID, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(ushort));
            count += sizeof(ushort);

            // originSize
            byte[] bFileSize = BitConverter.GetBytes(this.originSize);
            Array.Copy(bFileSize, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(int));
            count += sizeof(int);

            // fileNameLength
            byte[] bFileNameLength = BitConverter.GetBytes(this.fileNameLength);
            Array.Copy(bFileNameLength, 0, reserveBuffer.Array, reserveBuffer.Offset + count, sizeof(int));
            count += sizeof(int);

            // fileName
            byte[] bFileName = Encoding.Default.GetBytes(this.fileName);
            Array.Copy(bFileName, 0, reserveBuffer.Array, reserveBuffer.Offset + count, this.fileNameLength);
            count += this.fileNameLength;

            // file
            Array.Copy(file, 0, reserveBuffer.Array, reserveBuffer.Offset + count, file.Length);
            count += file.Length;

            // size
            byte[] size = BitConverter.GetBytes(count);
            Array.Copy(size, 0, reserveBuffer.Array, reserveBuffer.Offset, sizeof(int));

            ArraySegment<byte> sendBuffer = SendBufferHelper.Close(count);

            return sendBuffer;
        }
    }
}
