using System;
using System.Text;
using System.Text.RegularExpressions;

namespace NetworkService
{
    public class NetworkData
    {
        private const char Separator = ';';

        public string NetworkName { get; private set; }
        public string ServiceName { get; private set; }
        public string HostName { get; private set; }
        public string Name { get; private set; }
        public string Message { get; private set; }
        public string Error { get; private set; }

        public NetworkData(INetworkService networkService, string message)
        {
            if (networkService == null)
                throw new ArgumentNullException("networkService");
            if (string.IsNullOrEmpty(networkService.NetworkName))
                throw new ArgumentNullException("networkName");

            NetworkName = networkService.NetworkName;
            ServiceName = networkService.ServiceName;
            HostName = networkService.HostName;
            Name = networkService.Name;
            Message = message;
        }

        public NetworkData(string broadcastMessage)
        {
            FromBroadcastMessage(broadcastMessage);
        }

        public override string ToString()
        {
            string sender = !string.IsNullOrEmpty(Name) ? Name : HostName;
            if (string.IsNullOrEmpty(sender)) return Message;
            return string.Format("{0}: {1}", sender, Message);
        }

        public string ToBroadcastMessage()
        {
            var message = string.Concat(ServiceName, Separator, HostName, Separator, Name, Separator, Message);
            var encodedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Concat(message.Length, Separator, message)));
            return string.Format("{0}:{1}", NetworkName, encodedMessage);
        }

        public void FromBroadcastMessage(string broadcastMessage)
        {
            if (string.IsNullOrEmpty(broadcastMessage))
                return;

            try
            {
                string broadcastMessageRegex = "^(?<NetworkName>.*?):(?<Base64>(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?)$";
                string messageRegex = string.Format(@"^(?<Length>\d+){0}(?<ServiceName>.*?){0}(?<HostName>.*?){0}(?<Name>.*?){0}(?<Message>.*)?$", Separator);

                var broadcastMessageMatch = Regex.Match(broadcastMessage, broadcastMessageRegex);
                if (broadcastMessageMatch.Success)
                {
                    NetworkName = broadcastMessageMatch.Groups["NetworkName"].Value;
                    var message = Encoding.UTF8.GetString(Convert.FromBase64String(broadcastMessageMatch.Groups["Base64"].Value));
                    var messageMatch = Regex.Match(message, messageRegex);
                    if (messageMatch.Success)
                    {
                        var length = messageMatch.Groups["Length"].Value;
                        if (message.Length == (int.Parse(length) + length.Length + 1))
                        {
                            ServiceName = messageMatch.Groups["ServiceName"].Value;
                            HostName = messageMatch.Groups["HostName"].Value;
                            Name = messageMatch.Groups["Name"].Value;
                            Message = messageMatch.Groups["Message"].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
