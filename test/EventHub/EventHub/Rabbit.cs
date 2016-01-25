using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Dynamic;
using System.Collections;
using System.Net;
using System.IO;
using System.Reflection;
using System.Web;



namespace EventHub
{
    public partial class Rabbit
    {
        private ConnectionFactory cf = null;
        private IConnection conn = null;
        private IModel ch = null;
        private JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
        private string _address;
        private int _api_port = 15672;
        private int _data_port = 5672;

        enum BindingMode { entity_get, entity_post, entity_find, subscription_find };
        public Rabbit(string address)
        {
            _address = address;
            cf = new ConnectionFactory();
            //cf.UserName = "eventHub";
            //cf.Password = "18daigaku75";

            cf.UserName = "guest";
            cf.Password = "guest";

            cf.HostName = _address;
            cf.Port = _data_port;
            conn = cf.CreateConnection();
            ch = conn.CreateModel();
            conn.AutoClose = false;
        }

        public class Response
        {
            public string code {get;set;}
            public string json { get; set; }
        }

        private List<string> GetQueues()
        {
            Uri url = new Uri("http://" + _address + ":" + _api_port.ToString() + "/api/queues");
            ForceCanonicalPathAndQuery(url);

            List<string> queues = new List<string>();
            Q q=null;

            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Credentials = new System.Net.NetworkCredential(cf.UserName, cf.Password);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream respStream = response.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respStream))
                            {
                                string json = "{\"queues\": {\"queue\":";
                                json += streamReader.ReadToEnd();
                                json += "}}";
                                q = jsSerializer.Deserialize<Q>(json);
                            }
                        }
                    }
                    else
                    {
                        //Console.Write("ERROR: {0}", response.StatusCode);
                        return null;
                    }
                }
            }
            catch(Exception x)
            {

            }

            foreach (Queue queue in q.queues.queue)
            {
                queues.Add(queue.name);

            }
            return queues;
        }

        private Dictionary<string, object> GetBinding(string queue, string binding, BindingMode mode)
        {
            Uri url = new Uri("http://" + _address + ":" + _api_port.ToString() + "/api/queues/%2F/" + queue + "/bindings");
            ForceCanonicalPathAndQuery(url);
            B b = null;

            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Credentials = new System.Net.NetworkCredential(cf.UserName, cf.Password);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream respStream = response.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respStream))
                            {
                                string json = "{\"bindings\": {\"binding\":";
                                json += streamReader.ReadToEnd();
                                json += "}}";
                                b = jsSerializer.Deserialize<B>(json);
                            }
                        }
                    }
                    else
                    {
                        //Console.Write("ERROR: {0}", response.StatusCode);
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                //Console.Write("ERROR: {0}", e.Message);
                return null;
            }

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            foreach (Binding a in b.bindings.binding)
            {
                switch (mode)
                {
                    case BindingMode.entity_post:
                        if (a.routing_key.StartsWith(binding))
                        {
                            arguments.Add("description", a.arguments.description);
                            arguments.Add("domain", a.arguments.domain);
                            arguments.Add("entity_ega", a.arguments.entity_ega);
                        }
                        break;
                    case BindingMode.entity_get:
                        if (a.arguments.entity_ega != null  && a.arguments.entity_ega.Equals(binding))
                        {
                            arguments.Add(a.routing_key, a);
                        }
                        break;
                    case BindingMode.entity_find:
                        if (a.routing_key.Equals(binding))
                        {
                            arguments.Add(a.routing_key, a);
                        }
                        break;
                    case BindingMode.subscription_find:
                        if (binding != null)
                        {
                            if (a.routing_key.StartsWith(binding))
                            {
                                arguments.Add(a.routing_key, a);
                            }
                        }
                        else
                        {
                            if (!(a.routing_key.Equals(queue) || a.routing_key.StartsWith("_")))
                                arguments.Add(a.routing_key, a);
                        }
                        break;
                }
            }

            return arguments;
        }
        void ForceCanonicalPathAndQuery(Uri uri)
        {
            if (Type.GetType("Mono.Runtime") == null)
            {
                string paq = uri.PathAndQuery; // need to access PathAndQuery
                FieldInfo flagsFieldInfo = typeof(Uri).GetField("m_Flags", BindingFlags.Instance | BindingFlags.NonPublic);
                ulong flags = (ulong)flagsFieldInfo.GetValue(uri);
                flags &= ~((ulong)0x30); // Flags.PathNotCanonical|Flags.QueryNotCanonical
                flagsFieldInfo.SetValue(uri, flags);
            }
        }
    }    
}
