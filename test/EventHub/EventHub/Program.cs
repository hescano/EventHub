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
            Console.WriteLine("STARTING EVENT HUB NOW");

            try
            {
		Console.WriteLine("TEST");
                Int32 port = 80;
                
		string address = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(o => o.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First().ToString();
                //string mq_address = System.Environment.GetEnvironmentVariable("RABBIT");
		//string mq_address = "eventhub-rabbit-tst";
		string mq_address = "ab93e749bc07d11e5aca9023200705d6-1500000672.us-west-2.elb.amazonaws.com"; //AWS external DNS address
        //string mq_address = "10.0.252.174"; // AWS internal address (THIS MAY CHANGE!)
		//string mq_address = "event-hub.devops.byu.edu";
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
