using System;
using NetworkService;

namespace NetworkServiceConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Name: ");
            string name = Console.ReadLine();
            Console.WriteLine("Started: {0}", DateTime.Now);
            Console.WriteLine();

            INetworkService networkService = new UdpNetworkService();
            networkService.NetworkDataReceived += OnNetworkDataReceived;
            networkService.Name = name;
            networkService.AllowLocal = true;
            networkService.Start();

            string message;
            do
            {
                message = Console.ReadLine();
                networkService.SendMessage(message);
            } while (message != "quit");
        }

        private static void OnNetworkDataReceived(object sender, NetworkDataReceived e)
        {
            var networkService = (INetworkService)sender;
            if (!e.IsLocal || e.Data.Name != networkService.Name)
                Console.WriteLine(e.Data);
        }
    }
}
