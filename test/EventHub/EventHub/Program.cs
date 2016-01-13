using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace EventHub
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("STARTING NOTIFY_ME_API");

            try
            {
                Int32 port = 5000;

                string address = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(o => o.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First().ToString();
                //string mq_address = System.Environment.GetEnvironmentVariable("RABBIT");
                string mq_address = "eventhub-rabbit-tst:5672";

                if (mq_address == null)
                {
                    if (Type.GetType("Mono.Runtime") != null)
                    {
                        Console.WriteLine("Running on Mono");
                        mq_address = "rabbitmq";
                    }
                    else
                    {
                        Console.WriteLine("NOT Running on Mono");
                        mq_address = "dry2-dev.byu.edu";
                    }
                }

                Console.WriteLine("RABBIT:" + mq_address);

                Rabbit mq = new Rabbit(mq_address);


                Console.WriteLine("IP ADDRESS:" + address);
                IPAddress localAddr = IPAddress.Parse(address);

                // TcpListener server = new TcpListener(port);
                TcpListener server = new TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                HttpRequest handler = new HttpRequest(server);
                handler.Listen(mq);
            }
            catch (Exception x)
            {
                Console.WriteLine("Cannot Connet to RabbitMQ [" + x.Message + "]");
            }

        }
    }
}
