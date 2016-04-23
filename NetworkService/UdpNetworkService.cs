using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkService
{
    public class UdpNetworkService : INetworkService
    {
        #region Fields

        public const int DefaultPort = 2000;
        public const string DefaultNetworkName = "SimpleNetwork";
        public const string DefaultServiceName = "UdpNetworkService";
        private static readonly object _serviceSync = new object();
        private static readonly Guid _serviceId = Guid.NewGuid();

        private UdpClient _udpClient;

        private string _networkName;
        public string NetworkName
        {
            get { return _networkName ?? DefaultNetworkName; }
            set { if (!Enabled) _networkName = value; }
        }

        private string _serviceName;
        public string ServiceName
        {
            get { return _serviceName ?? DefaultServiceName; }
            set { if (!Enabled) _serviceName = value; }
        }

        private string _hostName;
        public string HostName
        {
            get
            {
                if (_hostName == null)
                {
                    _hostName = Dns.GetHostName();
                }
                return _hostName;
            }
        }

        private string _name;
        public string Name
        {
            get { return !string.IsNullOrEmpty(_name) ? _name : _serviceId.ToString(); }
            set { if (!Enabled) _name = value; }
        }

        private IPAddress _localIpAddress;
        public IPAddress LocalIpAddress
        {
            get
            {
                if (_localIpAddress == null)
                {
                    var host = Dns.GetHostEntry(HostName);
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            _localIpAddress = ip;
                        }
                    }
                }
                return _localIpAddress;
            }
        }

        private int? _port;
        public int Port
        {
            get { return _port.HasValue ? _port.Value : DefaultPort; }
            set { if (!Enabled) _port = value; }
        }

        public bool AllowLocal
        {
            get;
            set;
        }

        public bool Enabled
        {
            get { return _udpClient != null; }
        }

        public event EventHandler<NetworkDataReceived> NetworkDataReceived;

        #endregion

        #region Constructors

        public UdpNetworkService()
        {

        } 

        #endregion

        #region Methods

        public void Start()
        {
            if (!Enabled)
            {
                lock (_serviceSync)
                {
                    if (!Enabled)
                    {
                        _udpClient = new UdpClient { EnableBroadcast = true, ExclusiveAddressUse = false };
                        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
                        _udpClient.BeginReceive(OnUdpDataReceived, _udpClient);
                    }
                }
            }
        }

        public void SendMessage(string message)
        {
            if (Enabled)
            {
                lock (_serviceSync)
                {
                    if (Enabled)
                    {
                        var networkData = new NetworkData(this, message);
                        var datagram = Encoding.UTF8.GetBytes(networkData.ToBroadcastMessage());
                        _udpClient.Send(datagram, datagram.Length, new IPEndPoint(IPAddress.Broadcast, Port));
                    }
                }
            }
        }

        private void OnUdpDataReceived(IAsyncResult asyncResult)
        {
            if (Enabled)
            {
                lock (_serviceSync)
                {
                    if (Enabled)
                    {
                        var ipEndPoint = new IPEndPoint(IPAddress.Any, Port);
                        var udpClient = (UdpClient)asyncResult.AsyncState;
                        byte[] data = udpClient.EndReceive(asyncResult, ref ipEndPoint);
                        ProcessDataReceived(data, ipEndPoint);
                        _udpClient.BeginReceive(OnUdpDataReceived, _udpClient);
                    }
                }
            }
        }

        private void ProcessDataReceived(byte[] data, IPEndPoint ipEndPoint)
        {
            bool isLocal = LocalIpAddress.Equals(ipEndPoint.Address);
            if (!isLocal || AllowLocal)
            {
                var networkData = new NetworkData(Encoding.UTF8.GetString(data));
                if (networkData.NetworkName != NetworkName) return;
                if (networkData.ServiceName != ServiceName) return;
                if (NetworkDataReceived != null)
                {
                    NetworkDataReceived(this, new NetworkDataReceived
                    {
                        Data = networkData,
                        IpAddress = ipEndPoint.Address.ToString(),
                        Port = ipEndPoint.Port,
                        IsLocal = isLocal
                    });
                }
            }
        } 

        #endregion

        #region Dispose

        ~UdpNetworkService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);          
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (Enabled)
            {
                lock (_serviceSync)
                {
                    if (Enabled)
                    {
                        try
                        {
                            _udpClient.Client.Shutdown(SocketShutdown.Both);
                        }
                        catch { }

                        try
                        {
                            _udpClient.Close();
                            _udpClient = null;
                        }
                        catch { }
                    }
                }
            }
        }

        #endregion
    }
}
