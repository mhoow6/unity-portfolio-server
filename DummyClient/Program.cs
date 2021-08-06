using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace DummyClient
{
    class Program
    {
        const int SEND_BUFFER_RESERVE_SIZE = 4096;
        static SendBuffer sendBuffer = new SendBuffer();
        static Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
        static SocketAsyncEventArgs sendAsyncArgs;
        static SocketAsyncEventArgs recvAsyncArgs;
        static List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();
        static RecvBuffer recvBuffer = new RecvBuffer();
        static Object _lock = new object();

        static void Main(string[] args)
        {
            // Init
            Container container = Container.Instance;
            Connector connector = new Connector();
            connector.Connect(container.host, () => { return SessionManager.Instance.MakeServerSession(); }, 1);
            // Init

            while (true)
            {
                ;
            }
        }


        static void RecvHandler(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            // 문자열의 사이즈
            byte[] bSize = new byte[sizeof(ushort)];
            Array.Copy(buffer.Array, buffer.Offset + count, bSize, 0, sizeof(ushort));
            count += sizeof(ushort);
            ushort messageLength = BitConverter.ToUInt16(bSize);
            Console.WriteLine($"서버에게 받은 문자열의 길이: {messageLength}");

            // 실제 데이터가 담긴 문자열
            byte[] bMessage = new byte[messageLength];
            Array.Copy(buffer.Array, buffer.Offset + count, bMessage, 0, messageLength);
            count += messageLength;
            string message = Encoding.UTF8.GetString(bMessage);
            Console.WriteLine($"서버에게 받은 문자열의 내용: {message}");

            recvBuffer.Clean();
        }

        static void Send(Socket sock)
        {
            while (true)
            {
                ushort count = 0;
                ArraySegment<byte> sndBuffer = sendBuffer.Reserve(SEND_BUFFER_RESERVE_SIZE);

                string message = "sg";
                byte[] tmp = Encoding.Default.GetBytes(message);

                byte[] size = BitConverter.GetBytes((ushort)tmp.Length);
                Array.Copy(size, 0, sndBuffer.Array, sndBuffer.Offset, sizeof(ushort));
                count += sizeof(ushort);
                Array.Copy(tmp, 0, sndBuffer.Array, sndBuffer.Offset + count, tmp.Length);
                count += (ushort)tmp.Length;

                lock (_lock)
                {
                    sendQueue.Enqueue(argsBuffer);

                    if (pendingList.Count == 0)
                        RegisterSend(sendArgs);
                }
            }
        }
    }
}
