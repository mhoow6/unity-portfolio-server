using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerCore
{
    public abstract class Session
    {
        public int sessionId;

        protected int HEADERSIZE { get => 4; }
        protected int ERROR { get => -1; }

        // 연결된 소켓
        public Socket socket
        {
            get;
            private set;
        }

        SocketAsyncEventArgs sendAsyncArgs = new SocketAsyncEventArgs();
        Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> pendingList = new List<ArraySegment<byte>>();
        
        SocketAsyncEventArgs recvAsyncArgs = new SocketAsyncEventArgs();
        RecvBuffer recvBuffer = new RecvBuffer();
        
        // For Lock
        Object _lock = new Object();

        // For Disconnection flag
        int IsDisconnected;

        public abstract void OnConnected(EndPoint endPoint);
        public abstract void OnDisconnected(EndPoint endPoint);

        public abstract int OnRecv(ArraySegment<byte> buffer);

        /// <summary>
        /// 세션을 시작합니다.
        /// </summary>
        /// <param name="sock">상대방과 연결된 소켓을 넘겨주세요.</param>
        public void Start(Socket sock)
        {
            socket = sock;
            sendAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            recvAsyncArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);

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

        /// <summary>
        /// 소켓 연결 끊김 시 처리해주는 함수
        /// </summary>
        public void Disconnect()
        {
            const int CONNECTED = 0;
            const int DISCONNECTED = 1;

            // 연결이 끊기기 전에 끊겼으면 다른 스레드가 이미 종료처리가 완료된 것이므로 또 할 필요가 없다.
            if (Interlocked.Exchange(ref IsDisconnected, DISCONNECTED) == DISCONNECTED)
                return;

            // 콜백 함수
            OnDisconnected(socket.RemoteEndPoint);

            // 세션에서 킥
            SessionManager.Instance.Kick(this);

            // 소켓 처리
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            // 더 이상 버퍼를 보낼 수 없으므로 큐, 리스트 초기화
            lock (_lock)
            {
                sendQueue.Clear();
                pendingList.Clear();
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
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
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
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                if (recvBuffer.Used(args.BytesTransferred))
                {
                    OnRecv(recvBuffer.ReadSegment);
                    RegisterRecv();
                }
                else
                {
                    Console.WriteLine($"[Session] RecvBuffer 부족");
                }
            }
            else
            {
                Console.WriteLine($"[Session] OnRecvCompleted 도중 {args.SocketError} 이 발생했습니다.");

                Disconnect();
            }
        }
        #endregion
    }

    public class PacketSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;
            int packetCount = 0;

            while (true)
            {
                if (buffer.Count < HEADERSIZE)
                    break;

                // 헤더에 기록된 패킷 총 길이
                byte[] bSize = new byte[sizeof(int)];
                Array.Copy(buffer.Array, buffer.Offset, bSize, 0, bSize.Length);
                int size = BitConverter.ToInt32(bSize);

                // 패킷 매니저에게 패킷 처리 부탁
                byte[] bData = new byte[size];
                Array.Copy(buffer.Array, buffer.Offset, bData, 0, bData.Length);
                ArraySegment<byte> handleBuff = new ArraySegment<byte>(bData, 0, bData.Length);
                PacketManager.Instance.OnRecvPacket(this, handleBuff);

                // 처리한 패킷 수, 패킷 데이터 기록
                processLen += size;
                packetCount++;

                // 버퍼 위치 재 조정
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + size, buffer.Count - size);
            }

            return processLen;
        }
    }

    public class ServerSession : PacketSession
    {
        public FileRoom fileRoom = new FileRoom();

        public override void OnConnected(EndPoint endPoint)
        {
            try
            {
                SessionManager.Instance.ClientSendForEach();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Client] {e.ToString()}");
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[ServerSession] 서버와 연결이 끊겼습니다.");
        }
    }

    public class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"[ClientSession] {endPoint}가 연결하였습니다.");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"[ClientSession] {endPoint}와 연결이 끊겼습니다.");
        }
    }
}
