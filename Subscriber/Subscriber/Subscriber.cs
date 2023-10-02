using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Subscriber
{
    class SubscriberSocket
    {
        private Socket subscriberSocket;
        private byte[] buffer;
        private const int BUFF_SIZE = 1024;
        private string ip;
        private int port;
        public bool isConnected = false;

        public SubscriberSocket()
        {
            subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Bind(int port)
        {
            subscriberSocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port)); // binduim socketu 
        }

        public void Connect(string ipAddres, int port)
        {
            ip = ipAddres;
            this.port = port;
            subscriberSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ipAddres), port), ConnectCallback, null);
        }

        private void Reconnect()
        {
            this.isConnected = false;
            subscriberSocket.Close();
            subscriberSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            subscriberSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), port), ConnectCallback, null);
        }

        public void Listen()
        {
            subscriberSocket.Listen(1); // incepem sa ascultam 
        }

        public void Accept()
        {
            subscriberSocket.BeginAccept(AcceptedCallback, null); // incepem sa acceptam conexiuni
        }

        private void AcceptedCallback(IAsyncResult result) // se apeleaza cind se accepta un pachet
        {
            try
            {
                ConnectionInfo connection = new ConnectionInfo(); // luam informatia de la conexiune

                connection.socket = subscriberSocket.EndAccept(result); // terminam transmiterea datelor
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

        public void Send(byte[] data)
        {
            try
            {
                subscriberSocket.Send(data);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not Send data, {e.Message}");
            }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            if (subscriberSocket.Connected)
            {
                isConnected = true;
                try
                {
                    Console.WriteLine("Connection estabilished");
                    buffer = new byte[BUFF_SIZE];
                    subscriberSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't estabilish a connection, {e.Message}");
                }
            }
            else
            {
                Console.WriteLine("Subscriber Socket could not connect, trying to reconnect in 5 seconds");
                Thread.Sleep(5000);
                this.Reconnect();
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int buffLength = subscriberSocket.EndReceive(result);
                byte[] packet = new byte[buffLength];
                Array.Copy(buffer, packet, packet.Length);
                string res = Encoding.Default.GetString(packet);
                Console.WriteLine(res);

                buffer = new byte[BUFF_SIZE];
                subscriberSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Can't receive data from broker {e.Message}");
                Console.WriteLine("Trying to recconect in 5 seconds");
                Thread.Sleep(5000);
                this.Reconnect();
            }
        }
    }
}
