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
    class Program
    {
        private static SubscriberSocket socket = new SubscriberSocket();
        static void Main(string[] args)
        {
            ushort id;
            ushort port;
            string msg;

            while (true)
            {
                try
                {
                    Console.Write("Enter port: "); // protul de ascultare
                    port = Convert.ToUInt16(Console.ReadLine());
                    Console.Write("Enter thematic id: "); // id-ul la care sa transmita brokerul mesajele
                    id = Convert.ToUInt16(Console.ReadLine());

                    Console.Write("s - subscribe. another key - unsubscribe : ");
                    if (Console.ReadLine().ToLower() == "s")
                        msg = "subscribe";
                    else
                        msg = "unsubscribe";

                    var message = Message.CreateMessage(msg, id);

                    socket.Bind(port);
                    socket.Connect("127.0.0.1", 9000);

                    while (true)
                    {
                        if (socket.isConnected)
                        {
                            socket.Send(message);
                            break;
                        }
                        Thread.Sleep(400);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                Console.WriteLine("Action performed. Press enter to exit.");
                break;
            }

            Console.ReadLine();

        }
    }
}
