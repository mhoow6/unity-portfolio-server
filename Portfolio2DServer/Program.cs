using System;
using System.Net;
using System.Net.Sockets;
using ServerCore;
using System.Text;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        const int SEND_BUFFER_RESERVE_SIZE = 4096;

        static Socket listenSock;
        static RecvBuffer recvBuffer = new RecvBuffer();
        static SendBuffer sendBuffer = new SendBuffer();

        static void Main(string[] args)
        {
            Container container = Container.Instance;

            listenSock = new Socket(container.host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Console.WriteLine($"[Server] 서버 Bind 중..");
            listenSock.Bind(container.host);

            Console.WriteLine($"[Server] 서버 Listen 중..");
            listenSock.Listen(container.backlog);

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
            Console.WriteLine($"[Server] 클라이언트 Accept 할려고 기다리는 중..");
            args.AcceptSocket = null;

            bool pending = listenSock.AcceptAsync(args);

            if (pending == false)
                OnAcceptCompleted(null, args);
        }

        static void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Console.WriteLine($"[Server] {args.AcceptSocket.RemoteEndPoint} Accept 완료!");
                RegisterRecv(args.AcceptSocket);
            }
            else
                Console.WriteLine($"[Server] OnAcceptCompleted 도중 {args.SocketError} 이 발생했습니다.");

            RegisterAccept(args);
        }

        static void RegisterRecv(Socket sock)
        {
            Console.WriteLine($"[Server] {sock.RemoteEndPoint} 한테서 Receive 기다리는 중..");

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
                Console.WriteLine($"[Server] {sock.RemoteEndPoint} 한테서 Receive 받았습니다.");

                if (recvBuffer.Used(args.BytesTransferred) == false)
                {
                    Console.WriteLine($"[Server] RecvBuffer가 부족하여 데이터 처리에 실패했습니다.");
                    return;
                }

                RecvHandler(recvBuffer.ReadSegment, sock);
            }
            else
            {
                Console.WriteLine($"[Server] OnRecvCompleted 도중 {args.SocketError} 이 발생했습니다.");
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
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
            Console.WriteLine($"받은 문자열의 길이: {messageLength}");

            // 실제 데이터가 담긴 문자열
            byte[] bMessage = new byte[messageLength];
            Array.Copy(buffer.Array, buffer.Offset + count, bMessage, 0, messageLength);
            count += messageLength;
            string message = Encoding.UTF8.GetString(bMessage);
            Console.WriteLine($"받은 문자열의 내용: {message}");

            recvBuffer.Clean();
        }

        static void RecvHandler(ArraySegment<byte> buffer, Socket sock)
        {
            ushort count = 0;

            // 문자열의 사이즈
            byte[] bSize = new byte[sizeof(ushort)];
            Array.Copy(buffer.Array, buffer.Offset + count, bSize, 0, sizeof(ushort));
            count += sizeof(ushort);
            ushort messageLength = BitConverter.ToUInt16(bSize);
            Console.WriteLine($"받은 문자열의 길이: {messageLength}");

            // 실제 데이터가 담긴 문자열
            byte[] bMessage = new byte[messageLength];
            Array.Copy(buffer.Array, buffer.Offset + count, bMessage, 0, messageLength);
            count += messageLength;
            string message = Encoding.UTF8.GetString(bMessage);
            Console.WriteLine($"받은 문자열의 내용: {message}");

            BroadCast(buffer, sock);

            recvBuffer.Clean();
        }

        static void BroadCast(ArraySegment<byte> buffer, Socket sock)
        {
            SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
            asyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            asyncArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            asyncArgs.UserToken = sock;

            RegisterSend(asyncArgs);
        }

        static void RegisterSend(SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;

            Console.WriteLine($"[시스템] {sock.RemoteEndPoint}에게 RegisterSend 이 발생했습니다.");

            bool pending = sock.SendAsync(args);

            if (pending == false)
                OnSendCompleted(null, args);
        }

        static void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;

            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                Console.WriteLine($"[시스템] {sock.RemoteEndPoint} OnSendCompleted!!.");
                RegisterRecv(sock);
            }
            else
            {
                Console.WriteLine($"[시스템] OnSendCompleted 도중 {args.SocketError} 이 발생했습니다.");
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
        }
    }
}
