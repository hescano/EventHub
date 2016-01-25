using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace EventHub
{
    class HttpRequest 
    {
        private TcpListener _server;

        public HttpRequest (TcpListener server)
        {
            _server = server;
        }

      
        public void Listen(Rabbit mq)
        {
              // Buffer for reading data
                Byte[] bytes = new Byte[256];
                int i;
                string header_text;
                string response;
                byte[] hdr;
                Rabbit.Response msg = null;
                string json = null;
                string old_json;

                JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

                Dictionary<string, Func<string , Dictionary<string, string> ,Dictionary<string, string>, Rabbit.Response>> functions = new Dictionary<string, Func<string , Dictionary<string, string> ,Dictionary<string, string>, Rabbit.Response>>();
                functions["POST_entities"] = mq.Post_Entity;
                functions["PUT_entities"] = mq.Put_Entity;
                functions["GET_entities"] = mq.Get_Entity;
                functions["DELETE_entities"] = mq.Delete_Entity;
                functions["POST_webhooks"] = mq.Post_Webhook;
                functions["PUT_webhooks"] = mq.Put_Webhook;
                functions["GET_webhooks"] = mq.Get_Webhook;
                functions["DELETE_webhooks"] = mq.Delete_Webhook;
                functions["POST_subscriptions"] = mq.Post_Subscription;
                functions["PUT_subscriptions"] = mq.Put_Subscription;
                functions["GET_subscriptions"] = mq.Get_Subscription;
                functions["DELETE_subscriptions"] = mq.Delete_Subscription;
                functions["POST_events"] = mq.Post_Event;
                functions["PUT_events"] = mq.Put_Event;
                functions["GET_events"] = mq.Get_Event;
                functions["DELETE_events"] = mq.Delete_Event;



                // Enter the listening loop.
                while(true) 
                {
                    Console.Write("Waiting for Connection...");
                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    TcpClient client = _server.AcceptTcpClient();      
                    Console.WriteLine("Connected! " + DateTime.Now.ToString());

                    json = null;
                    old_json = "";

                    StringBuilder data = new StringBuilder("");

                    

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    // Loop to receive all the data sent by the client.
                    do
                    {
                        try
                        {
                            i = stream.Read(bytes, 0, bytes.Length);
                            // Translate data bytes to a ASCII string.
                            data.Append(System.Text.Encoding.ASCII.GetString(bytes, 0, i));
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine("PROBLEM WITH READ");
                            break;
                        }
                    } while (i == 256);

                    Console.WriteLine("DATA=" + data.ToString());

                    try
                    {

                        int payload_start = data.ToString().IndexOf("\r\n\r\n");
                        Console.WriteLine("PAYLOAD START AT:" + payload_start.ToString());

                        if (payload_start == -1)
                            header_text = data.ToString();
                        else
                            header_text = data.ToString().Substring(0, payload_start);
                        

                        string[] lines = header_text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                        Dictionary<string, string> headers = new Dictionary<string, string>();
                        Dictionary<string, string> parameters = new Dictionary<string, string>();

                        foreach (string s in lines)
                        {
                            if (s == null || s == "" || (int)s[0] == 13)
                                break;
                            string str = s.Replace("\r", "").Replace("\n", "");

                            char[] separator1 = new char[] { ':' };
                            char[] separator2 = new char[] { '&' };
                            char[] separator3 = new char[] { '/' };
                            try
                            {
                                string[] header = str.Split(separator1, 2, StringSplitOptions.RemoveEmptyEntries);
                                switch (header.Length)
                                {
                                    case 1:
                                        string[] request = str.Split(' ');
                                        headers.Add("Method", request[0]);
                                        headers.Add("Protocol", request[2]);

                                        int query = request[1].IndexOf("?");                                  
                                        if (query != -1)
                                        {
                                            headers.Add("Path", request[1].Substring(0,query));

                                            string p = request[1].Substring(query+1);
                                            string[] parms = p.Split(separator2, StringSplitOptions.RemoveEmptyEntries);
                                            foreach (string st in parms)
                                            {
                                                string[] parm = st.Split('=');
                                                parameters.Add(parm[0],parm[1]);
                                            }
                                        }
                                        else
                                        {
                                            string[] parms = request[1].Substring(1).Split(separator3, StringSplitOptions.RemoveEmptyEntries);

                                            headers.Add("Path", "/" + parms[0]);
                                            for (int j=1; j< parms.Length; j++)
                                            {
                                                parameters.Add(j.ToString(), HttpUtility.UrlDecode(parms[j]));
                                            }
                                        }

                                        break;
                                    case 2:
                                        if (header[1][0] == ' ')
                                            header[1] = header[1].Substring(1);
                                        headers.Add(header[0], header[1]);

                                        break;
                                    default:
                                        break;
                                }

                            }
                            catch (Exception x)
                            {

                            }
                        }

                        Console.WriteLine("REQUEST:" + headers["Method"] + " " + headers["Path"]);

                        try
                        {
                            Console.WriteLine("DEBUG 1");
                            JWT jwt = new JWT(headers["X-JWT-Assertion"]);
                            if (jwt.isValid)
                            {
                                Console.WriteLine("DEBUG 1.1");
                                headers.Add("Entity", jwt.entity);
                                headers.Add("Queue", jwt.queue);
                            }
                            else
                            {
                                Console.WriteLine("DEBUG 1.2");
                                throw new Exception("401 Not Authorized");
                            }
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine("DEBUG 1.2");
                            if (payload_start != 999999)
                            {
                                Console.WriteLine("DEBUG 1.3");
                                throw new Exception("401 Not Authorized");
                            }
                            else
                            {
                                Console.WriteLine("DEBUG 1.4");
                                //                                headers.Add("Entity", "PRO");
                                //                                headers.Add("Queue", "415212202");
                                headers.Add("Entity", "Hannig");
                                headers.Add("Queue", "389206472");
                            }
                        }

                        try
                        {
                            Console.WriteLine("DEBUG 2.0");

                            if (headers["Transfer-Encoding"].Equals("chunked"))
                            {
                                Console.WriteLine("DEBUG 2.1");
                                json = "";
                                int length;
                                payload_start += 2;

                                do
                                {
                                    Console.WriteLine("DEBUG 2.2");
                                    string size = data.ToString().Substring(payload_start + 2);

                                    size = size.Substring(0, size.IndexOf("\r\n"));
                                    Int32.TryParse(size, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out length);

                                    //Console.WriteLine("CHUNK SIZE=" + length.ToString());
                                    if (length > 0)
                                    {
                                        Console.WriteLine("DEBUG 2.3");
                                        payload_start += size.Length + 4;
                                        json += data.ToString().Substring(payload_start, length);
                                        payload_start += length;
                                    }
                                    //Console.WriteLine("REMAINING LENGTH:" + length.ToString());
                                } while (length > 0);
                            }
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine("DEBUG 2.4");
                            //Console.WriteLine("NOT CHUNKED");
                        }

                        Console.WriteLine("DEBUG 3.0");
                        if (json == null)
                        {
                            Console.WriteLine("DEBUG 3.1");
                            if (payload_start > 1)
                            {
                                Console.WriteLine("DEBUG 3.2");
                                json = data.ToString().Substring(payload_start + 4);

                                if (json.Length == 0)
                                    json = null;
                                else if (json.Length != Int32.Parse(headers["Content-Length"]))
                                    throw new Exception("400 Bad Request");
                            }

                            Console.WriteLine("DEBUG 3.3");
                            //Console.WriteLine("PAYLOAD:\r\n" + json);
                        }


                        Console.WriteLine("DEBUG 3.4");

                        if (json != null)
                        {
                            Console.WriteLine("DEBUG 3.5");
                            if (json.Length == 0)
                            {
                                Console.WriteLine("DEBUG 3.6");
                                json = null;
                            }
                            else
                            {
                                try
                                {
                                    Console.WriteLine("DEBUG 3.7");
                                    if (!headers["Content-Type"].Contains("json"))
                                    {
                                        Console.WriteLine("DEBUG 3.8");
                                        throw new Exception("400 Bad Request");
                                    }
                                }
                                catch (Exception x)
                                {
                                    Console.WriteLine("DEBUG 3.9");
                                    throw new Exception("400 Bad Request");
                                }
                            }
                        }

                        Console.WriteLine("DEBUG 4.0");
                        old_json = json;

                        string function = headers["Method"] + headers["Path"].Replace("/", "_");
                        if (!functions.ContainsKey(function))
                            throw new Exception("404 Not Found");
                        msg = functions[function](json, headers, parameters);

                        string reply;
                        if (msg.json == null)
                            reply = string.Format("HTTP/1.1 {0}\n\n", msg.code);
                        else
                            reply = string.Format("HTTP/1.1 {0}\nContent-Type: application/json; charset=UTF-8\nContent-Length:{1}\n\n", msg.code, msg.json.Length);

                        Console.WriteLine("RESPONSE-" + reply);
                        hdr = System.Text.Encoding.ASCII.GetBytes(reply);
                        stream.Write(hdr, 0, hdr.Length);

                        if (msg.json != null)
                            stream.Write(System.Text.Encoding.ASCII.GetBytes(msg.json), 0, msg.json.Length);
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine("ERROR:" + x.Message);
                        Console.WriteLine("JSON=\r\n" + old_json);
                        response = string.Format("HTTP/1.1 500 {0}\nDate: {1}\n\n", x.Message, DateTime.Now.ToString());
                        hdr = System.Text.Encoding.ASCII.GetBytes(response);
                        stream.Write(hdr, 0, hdr.Length);
                    }

                    // Shutdown and end connection
                    client.Close();
                }
        }
    }
}
