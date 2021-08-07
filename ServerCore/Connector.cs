using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> sessionFactory;

        /// <summary>
        /// 클라이언트가 서버에 연결할 때 쓰이는 함수.
        /// </summary>
        /// <param name="endPoint">연결하고 싶은 IPEndPoint를 입력합니다.</param>
        /// <param name="sessionFactory">연결하고 나서 만들 세션 객체를 Func로 정합니다.</param>
        /// <param name="count">생성할 세션 수를 결정합니다.</param>
        public void Connect(IPEndPoint endPoint, Func<Session> factory, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sessionFactory = factory;

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnConnectCompleted;
                args.RemoteEndPoint = endPoint;
                args.UserToken = socket;

                RegisterConnect(args);
            }
        }

        #region Connect 과정
        public void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;

            if (socket == null)
                return;

            bool pending = socket.ConnectAsync(args);

            if (pending == false)
                OnConnectCompleted(null, args);
        }

        public void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            Socket sock = args.UserToken as Socket;

            if (args.SocketError == SocketError.Success)
            {
                Session session = sessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }

            else
                Console.WriteLine($"[Connector] OnConnectCompleted 도중 {args.SocketError} 이 발생했습니다.");
        }
        #endregion
    }
}
