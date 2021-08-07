using ServerCore;
using System;
using System.Threading;

namespace DummyClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Container container = Container.Instance;
            Connector connector = new Connector();
            connector.Connect(container.host, () => { return SessionManager.Instance.MakeServerSession(); }, 1);

            while (true)
            {
                try
                {
                    SessionManager.Instance.ClientSendForEach();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Client] {e.ToString()}");
                }

                Thread.Sleep(1000);
            }
        }
    }
}
