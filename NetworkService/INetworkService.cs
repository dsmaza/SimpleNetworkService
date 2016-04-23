using System;

namespace NetworkService
{
    public interface INetworkService : IDisposable
    {
        string NetworkName { get; set; }
        string ServiceName { get; set; }
        string HostName { get; }
        string Name { get; set; }
        int Port { get; set; }
        bool AllowLocal { get; set; }
        bool Enabled { get; }
        event EventHandler<NetworkDataReceived> NetworkDataReceived;
        void Start();
        void Stop();
        void SendMessage(string message);
    }
}
