using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Publisher
{
    class Program
    {
        private static PublisherSocket socket = new PublisherSocket();
        static void Main(string[] args)
        {
            ushort forwardId;

            socket.Bind(9001);
            socket.Connect("127.0.0.1", 9000); // conectare la broker
            Console.WriteLine("Publisher");

            string text = "";
            while (true) 
            {
                if (socket.isConnected)
                {
                    string tempId = "";
                    Console.Write("Enter thematic id: "); // id-ul la care sa transmita brokerul mesajele
                    tempId = Console.ReadLine();

                    if (String.IsNullOrWhiteSpace(tempId))
                        continue;

                    try
                    {
                        forwardId = Convert.ToUInt16(tempId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }

                    Console.WriteLine("Please enter a message");
                    text = Console.ReadLine();
                    if (String.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    byte[] message = Message.CreateMessage(text, forwardId); // aici pregatim datele in format binar

                    socket.Send(message); // transmite mesaj la broker.
                }
            }
        }
    }
}
