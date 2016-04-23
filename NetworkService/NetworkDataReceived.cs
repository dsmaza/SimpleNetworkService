using System;

namespace NetworkService
{
    public class NetworkDataReceived : EventArgs
    {
        public string IpAddress { get; internal set; }
        public int Port { get; internal set; }
        public NetworkData Data { get; internal set; }
        public bool IsLocal { get; internal set; }
    }
}
