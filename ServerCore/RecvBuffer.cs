using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class RecvBuffer
    {
        /// <summary>
        /// 현재까지 사용한 버퍼를 가져옵니다.
        /// </summary>
        public ArraySegment<byte> ReadSegment
        {
            get => new ArraySegment<byte>(buffer.Array, buffer.Offset, _usedSize);
        }

        /// <summary>
        /// 총 버퍼에서 사용할 수 있는 버퍼를 가져옵니다.
        /// </summary>
        public ArraySegment<byte> WriteSegment
        {
            get => new ArraySegment<byte>(buffer.Array, buffer.Offset + _usedSize, _freeSize);
        }

        const int BUFFERSIZE = 65535;
        ArraySegment<byte> buffer;

        int _usedSize = 0;
        int _freeSize { get => buffer.Count - _usedSize; }


        public RecvBuffer()
        {
            buffer = new ArraySegment<byte>(new byte[BUFFERSIZE], 0, BUFFERSIZE);
        }

        /// <summary>
        /// 사용했던 버퍼를 초기화해줍니다.
        /// </summary>
        public void Clean()
        {
            int dataSize = this._usedSize;

            if (dataSize == 0)
                _usedSize = 0;
            else
            {
                Array.Copy(buffer.Array, buffer.Offset + _usedSize, buffer.Array, buffer.Offset, _freeSize);
                _usedSize = dataSize;
            }
        }

        public bool Used(int usedSize)
        {
            if (_freeSize < usedSize)
                return false;

            _usedSize = usedSize;
            return true;
        }
    }
}
