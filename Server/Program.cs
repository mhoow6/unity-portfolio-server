using ServerCore;
using System;
using System.Threading;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Container container = Container.Instance;
            Listener listener = new Listener();

            listener.Init(container.host, () => { return SessionManager.Instance.MakeClientSession(); });

            while (true)
            {
                ;
            }
        }
    }
}
