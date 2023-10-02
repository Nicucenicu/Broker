using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Broker
{
        class ConnectionInfo
        {
            public const int BUFF_SIZE = 1024;
            public byte[] data = new byte[BUFF_SIZE];
            public ushort port;
            public Socket socket;
            public volatile ConcurrentQueue<string> lostMessages = new ConcurrentQueue<string>();
        }
}