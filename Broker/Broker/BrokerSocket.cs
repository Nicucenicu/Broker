using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Broker
{
    class BrokerSocket
    {
        private const int CONNECTIONS_NR = 100;
        private Socket brokerSocket;
        private volatile ConcurrentDictionary<string, ConnectionInfo> subscribers = new ConcurrentDictionary<string, ConnectionInfo>();


        public BrokerSocket()
        {
            brokerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // crearea socketului
        }

        public void Bind(int port)
        {
            brokerSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port)); // binduim socketu 
        }

        public void Listen()
        {
            brokerSocket.Listen(CONNECTIONS_NR); // incepem sa ascultam 
        }

        public void Accept()
        {
            brokerSocket.BeginAccept(AcceptedCallback, null); // incepem sa acceptam conexiuni
        }

        private void AcceptedCallback(IAsyncResult result) // se apeleaza cind se accepta un pachet
        {
            try
            {
                ConnectionInfo connection = new ConnectionInfo(); // luam informatia de la conexiune

                connection.socket = brokerSocket.EndAccept(result); // terminam transmiterea datelor
                connection.data = new byte[ConnectionInfo.BUFF_SIZE]; // alocam memorie pentru pachetul care a venit
                connection.socket.BeginReceive(connection.data, 0, connection.data.Length, SocketFlags.None, ReceiveCallback, connection);
                // extragerea datelor - se apeleaza ReceiveCallback
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't accept, {e.Message}");
            }
            finally
            {
                Accept();
            }

        }

        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectionInfo connection = result.AsyncState as ConnectionInfo;

            try
            {
                Socket senderSocket = connection.socket; // socketul de la publisher
                SocketError response;
                int buffSize = senderSocket.EndReceive(result, out response); // terminam primirea datelor
                IPEndPoint remoteIpEndPoint = senderSocket.RemoteEndPoint as IPEndPoint;

                if (response == SocketError.Success)
                {
                    byte[] packet = new byte[buffSize];  // alocam memorie pentru packet
                    Array.Copy(connection.data, packet, packet.Length); // copiem datele din packetul transmis

                    var data = PacketHandler.Handle(packet); // aflam id-ul si mesajul
                    if (remoteIpEndPoint.Port != 9001)
                    {
                        connection.port = (ushort)remoteIpEndPoint.Port;

                        if (data.message == "subscribe")
                        {
                            string key = data.themeId + "#" + connection.port;
                            if (!subscribers.TryAdd(key, connection))
                            {
                                ConnectionInfo temp;
                                var j = subscribers.TryRemove(key, out temp);
                                subscribers.TryAdd(key, connection);
                                subscribers[key].lostMessages = temp.lostMessages;
                            }

                            if (subscribers[key].lostMessages != null && subscribers[key].lostMessages.Any())
                            {
                                while (subscribers[key].lostMessages.Any())
                                {
                                    string message = "";
                                    subscribers[key].lostMessages.TryDequeue(out message);

                                    try
                                    {
                                        subscribers[key].socket.Send(Encoding.ASCII.GetBytes(message));
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine($"Could not send lostMessage data to {key}. Contact Ernest x_X ...");
                                    }
                                }
                            }
                        }
                        else if (data.message == "unsubscribe")
                        {
                            ConnectionInfo temp;
                            subscribers.TryRemove(data.themeId + "#" + connection.port, out temp);
                        }
                    }
                    else
                    {
                        foreach (var x in subscribers)
                        {
                            if (x.Key.Split('#')[0] == data.themeId.ToString())
                            {
                                try
                                {
                                    x.Value.socket.Send(Encoding.ASCII.GetBytes(data.message));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Could not send data to {x.Key}. Adding to SendLater...");
                                    x.Value.lostMessages.Enqueue(data.message);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't receive data from Client, {e.Message}");
            }
            finally
            {
                try
                {
                    connection.socket.BeginReceive(connection.data, 0, connection.data.Length, SocketFlags.None, ReceiveCallback, connection);
                    //incepem sa primi datele in continuare
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                    //connection.socket.Close();
                }
            }
        }

    }
}
