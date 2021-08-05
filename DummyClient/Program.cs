using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    class Program
    {
        const int SEND_BUFFER_RESERVE_SIZE = 4096;

        static Socket sock;
        static SendBuffer sendBuffer = new SendBuffer();
        static RecvBuffer recvBuffer = new RecvBuffer();

        static void Main(string[] args)
        {
            Container container = Container.Instance;

            sock = new Socket(container.host.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
            asyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectedCompleted);
            asyncArgs.RemoteEndPoint = container.host;
            asyncArgs.UserToken = sock;
            RegisterConnect(asyncArgs);

            while (true)
            {
                ushort count = 0;
                ArraySegment<byte> sndBuffer = sendBuffer.Reserve(SEND_BUFFER_RESERVE_SIZE);

                Console.Write($"보내고 싶은 문자열: ");
                string message = Console.ReadLine();
                byte[] tmp = Encoding.Default.GetBytes(message);

                byte[] size = BitConverter.GetBytes((ushort)tmp.Length);
                Array.Copy(size, 0, sndBuffer.Array, sndBuffer.Offset, sizeof(ushort));
                count += sizeof(ushort);
                Array.Copy(tmp, 0, sndBuffer.Array, sndBuffer.Offset + count, tmp.Length);
                count += (ushort)tmp.Length;

                ArraySegment<byte> argsBuffer = sendBuffer.Close(count);

                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
                sendArgs.SetBuffer(argsBuffer.Array, argsBuffer.Offset, argsBuffer.Count);

                RegisterSend(sendArgs);
            }
        }

        static void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;

            if (socket == null)
                return;

            bool pending = socket.ConnectAsync(args);

            if (pending == false)
                OnConnectedCompleted(null, args);
        }

        static void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
                RegisterRecv(sock);
            else
                Console.WriteLine($"[시스템] OnAcceptCompleted 도중 {args.SocketError} 이 발생했습니다.");
        }

        static void RegisterRecv(Socket sock)
        {
            recvBuffer.Clean();
            ArraySegment<byte> segment = recvBuffer.WriteSegment;

            SocketAsyncEventArgs asyncArgs = new SocketAsyncEventArgs();
            asyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            asyncArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = sock.ReceiveAsync(asyncArgs);

            if (pending == false)
                OnRecvCompleted(null, asyncArgs);
        }

        static void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                RecvHandler(args.Buffer);
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
            string recv = BitConverter.ToString(buffer.Array);
            Console.WriteLine($"[서버] {recv}");
        }

        static void RegisterSend(SocketAsyncEventArgs args)
        {
            bool pending = sock.SendAsync(args);

            if (pending == false)
                OnSendCompleted(null, args);
        }

        static void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
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
