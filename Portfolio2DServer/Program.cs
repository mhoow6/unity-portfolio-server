using System;
using System.Net;
using System.Net.Sockets;
using ServerCore;

namespace Server
{
    class Program
    {
        static Socket sock;
        static RecvBuffer recvBuffer = new RecvBuffer();

        static void Main(string[] args)
        {
            Container container = Container.Instance;

            sock = new Socket(container.host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine($"[시스템] 서버 Bind 중..");
            sock.Bind(container.host);

            Console.WriteLine($"[시스템] 서버 Listen 중..");
            sock.Listen(container.backlog);

            SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
            asyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(asyncArgs);

            while (true)
            {
                ;
            }
        }

        static void RegisterAccept(SocketAsyncEventArgs args)
        {
            Console.WriteLine($"[시스템] 클라이언트 Accept 할려고 기다리는 중..");
            args.AcceptSocket = null;

            bool pending = sock.AcceptAsync(args);

            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        static void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Console.WriteLine($"[시스템] {args.AcceptSocket.RemoteEndPoint} Accept 완료!");
                RegisterRecv(args.AcceptSocket);
            }
            else
                Console.WriteLine($"[시스템] OnAcceptCompleted 도중 {args.SocketError} 이 발생했습니다.");

            RegisterAccept(args);
        }

        static void RegisterRecv(Socket sock)
        {
            Console.WriteLine($"[시스템] {sock.RemoteEndPoint} 한테서 Receive 기다리는 중..");

            recvBuffer.Clean();
            ArraySegment<byte> segment = recvBuffer.WriteSegment;

            SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
            asyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            asyncArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);
            asyncArgs.UserToken = sock;

            bool pending = sock.ReceiveAsync(asyncArgs);

            if (pending == false)
                OnRecvCompleted(null, asyncArgs);
        }

        static void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;

            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                Console.WriteLine($"[시스템] {sock.RemoteEndPoint} 한테서 Receive 받았습니다.");

                if (recvBuffer.Used(args.BytesTransferred) == false)
                {
                    Console.WriteLine($"[시스템] RecvBuffer가 부족하여 데이터 처리에 실패했습니다.");
                    return;
                }

                RecvHandler(recvBuffer.ReadSegment);
            }
            else
            {
                Console.WriteLine($"[시스템] OnRecvCompleted 도중 {args.SocketError} 이 발생했습니다.");
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
        }

        static void RecvHandler(ArraySegment<byte> buffer)
        {
            ushort count = 0;

            // size를 나타내는 바이트는 ushort
            byte[] bSize = new byte[sizeof(ushort)];
            Array.Copy(buffer.Array, buffer.Offset + count, bSize, count, sizeof(ushort));
            count += sizeof(ushort);
            ushort size = BitConverter.ToUInt16(bSize);

            // 문자열
            Console.WriteLine($"받은 문자열의 길이: {size}");

            /*string recv = BitConverter.ToString(buffer.Array);
            Console.WriteLine($"{recv}");*/

            recvBuffer.Clean();
        }
    }
}
