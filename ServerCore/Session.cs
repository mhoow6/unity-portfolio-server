using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public abstract class Session
    {
        Socket socket;

        SocketAsyncEventArgs sendAsyncArgs;
        Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();
        
        SocketAsyncEventArgs recvAsyncArgs;
        RecvBuffer recvBuffer = new RecvBuffer();
        
        // For Lock
        Object _lock = new Object();

        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnRecv(ArraySegment<byte> buffer);

        /// <summary>
        /// 세션을 시작합니다.
        /// </summary>
        /// <param name="sock">상대방과 연결된 소켓을 넘겨주세요.</param>
        public void Start(Socket sock)
        {
            socket = sock;
            sendAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            recvAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(List<ArraySegment<byte>> sendBuffList)
        {
            if (sendBuffList.Count == 0)
                return;

            lock (_lock)
            {
                foreach (var sendBuff in sendBuffList)
                    sendQueue.Enqueue(sendBuff);

                if (pendingList.Count == 0)
                    RegisterSend();
            }
        }

        public void Send(ArraySegment<byte> sendBuff)
        {
            lock (_lock)
            {
                sendQueue.Enqueue(sendBuff);

                if (pendingList.Count == 0)
                    RegisterSend();
            }
        }

        #region 소켓 Send와 Receive 과정
        void RegisterSend()
        {
            pendingList.Clear();

            while (sendQueue.Count > 0)
            {
                ArraySegment<byte> sendBuff = sendQueue.Dequeue();
                pendingList.Add(sendBuff);
            }

            sendAsyncArgs.BufferList = pendingList;

            bool pending = socket.SendAsync(sendAsyncArgs);

            if (pending == false)
                OnSendCompleted(null, sendAsyncArgs);
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock (_lock)
            {
                Socket sock = args.UserToken as Socket;

                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    args.BufferList = null;
                    pendingList.Clear();

                    if (sendQueue.Count > 0)
                        RegisterSend();
                }
                else
                {
                    Console.WriteLine($"[Session] OnSendCompleted 도중 {args.SocketError} 이 발생했습니다.");
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                }
            }

        }

        void RegisterRecv()
        {
            recvBuffer.Clean();
            ArraySegment<byte> segment = recvBuffer.WriteSegment;
            recvAsyncArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

            bool pending = socket.ReceiveAsync(recvAsyncArgs);

            if (pending == false)
                OnRecvCompleted(null, recvAsyncArgs);
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;

            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                OnRecv(args.Buffer);
                RegisterRecv();
            }
            else
            {
                Console.WriteLine($"[Session] OnRecvCompleted 도중 {args.SocketError} 이 발생했습니다.");
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
        }
        #endregion
    }

    public class ServerSession : Session
    {
        public sealed override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[ServerSession] [{endPoint}]에서 연결하였습니다.");
        }

        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[ServerSession] Receive 받고 할 일을 정하세요.");
            return 0;
        }
    }

    public class ClientSession : Session
    {
        public sealed override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[ClientSession] [{endPoint}]에서 연결하였습니다.");
        }

        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[ClientSession] Receive 받고 할 일을 정하세요.");
            return 0;
        }
    }
}
