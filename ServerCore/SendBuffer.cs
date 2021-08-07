using System;

namespace ServerCore
{
    public class SendBuffer
    {
        public int freeSize { get => buffer.Count - _usedSize; }

        const int BUFFERSIZE = 65535;
        ArraySegment<byte> buffer;
        int _usedSize = 0;

        public SendBuffer(int chunkSize)
        {
            buffer = new ArraySegment<byte>(new byte[chunkSize], _usedSize, chunkSize);
        }

        public ArraySegment<byte> Reserve(int reserveSize)
        {
            if (reserveSize > freeSize)
            {
                _usedSize = 0;
                buffer = new ArraySegment<byte>(new byte[BUFFERSIZE], _usedSize, BUFFERSIZE);
                return new ArraySegment<byte>(buffer.Array, buffer.Offset + _usedSize, reserveSize);
            }

            return new ArraySegment<byte>(buffer.Array, buffer.Offset + _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> usedBuff = new ArraySegment<byte>(buffer.Array, buffer.Offset + _usedSize, usedSize);

            _usedSize += usedSize;

            return usedBuff;
        }
    }
}
