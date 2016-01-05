using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventHub
{
    public partial class Rabbit
    {
        public Response Post_Webhook(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                var request = jsSerializer.Deserialize<Dictionary<string, dynamic>>(json);
                Dictionary<string, object> arguments = new Dictionary<string, object>();

                arguments.Add("address", request["webhook"]["address"]);
                arguments.Add("method", request["webhook"]["method"]);
                arguments.Add("port", request["webhook"]["port"]);

                try
                {
                    arguments.Add("acknowledge", request["webhook"]["acknowledge"]);
                }
                catch (Exception x)
                {
                    arguments.Add("acknowledge", "false");
                }

                Delete_Webhook(json, headers, parameters);
                
                ch.QueueDeclare(headers["Queue"], true, false, false, null);
                ch.QueueBind(headers["Queue"], "edu.byu", "_webhook", arguments);

                request["webhook"].Add("identity_id", headers["Queue"]);
                request["webhook"].Add("identity_name", headers["Entity"]);
            }
            catch (Exception x)
            {
                throw new Exception("406 Not Acceptable");
            }

            return new Response() { code = "200 OK", json = json };

        }
        public Response Put_Webhook(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                var request = jsSerializer.Deserialize<Dictionary<string, dynamic>>(json);

                var webhook = GetBinding(headers["Queue"], "_webhook", BindingMode.entity_find);
                if (webhook != null && webhook.Count > 0)
                {
                    Binding binding = webhook["_webhook"] as Binding;

                    Dictionary<string, object> arguments = new Dictionary<string, object>();

                    try
                    {
                        arguments.Add("acknowledge", request["webhook"]["acknowledge"]);
                    }
                    catch (Exception x1)
                    {
                        arguments.Add("acknowledge", binding.arguments.acknowledge);
                    }
                    try
                    {
                        arguments.Add("address", request["webhook"]["address"]);
                    }
                    catch (Exception x1)
                    {
                        arguments.Add("address", binding.arguments.address);
                    }
                    try
                    {
                        arguments.Add("method", request["webhook"]["push_option"]);
                    }
                    catch (Exception x1)
                    {
                        arguments.Add("method", binding.arguments.method);
                    }
                    try
                    {
                        arguments.Add("port", request["webhook"]["port"]);
                    }
                    catch (Exception x1)
                    {
                        arguments.Add("port", binding.arguments.port);
                    }

                    Delete_Webhook(null, headers, parameters);

                    ch.QueueDeclare(headers["Queue"], true, false, false, null);
                    ch.QueueBind(headers["Queue"], "edu.byu", "_webhook", arguments);

                    return Get_Webhook(null, headers, parameters);
                }
                else
                {
                    return new Response() { code = "404 Not Found", json = null };
                }
            }
            catch (Exception x)
            {
                return new Response() { code = "406 Not Acceptable", json = null };
            }
        }
        public Response Get_Webhook(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            var webhook = GetBinding(headers["Queue"], "_webhook", BindingMode.entity_find);
            if (webhook != null)
            {
                Binding binding = webhook["_webhook"] as Binding;
                json = "{\"webhook\" : { \"address\": \"" + binding.arguments.address + "\", \"port\": " + binding.arguments.port + ",\"push_option\": \"" + binding.arguments.method + "\",\"acknowledge\": \"" + binding.arguments.acknowledge + "\"}}";
                return new Response() { code = "200 OK", json = json };
            }
            return new Response() { code = "204 No Content", json = null };
        }
        public Response Delete_Webhook(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            Dictionary<string, object> arguments = new Dictionary<string, object>();

            var webhook = GetBinding(headers["Queue"], "_webhook", BindingMode.entity_find);
            if (webhook != null && webhook.Count > 0)
            {
                Binding binding = webhook["_webhook"] as Binding;

                arguments.Add("address", binding.arguments.address);
                arguments.Add("method", binding.arguments.method);
                arguments.Add("acknowledge", binding.arguments.acknowledge);
                arguments.Add("port", binding.arguments.port);

                ch.QueueUnbind(headers["Queue"], "edu.byu", "_webhook", arguments);
                return new Response() { code = "200 OK", json = json };
            }

            return new Response() { code = "404 Not Found", json = null };
        }
    }
    public class Webhook
    {
        public string address { get; set; }
        public string push_option { get; set; }
        public string acknowledge { get; set; }
        public int port { get; set; }
        public string identity_id { get; set; }
        public string identity_name { get; set; }
    }

    public class _Webhook
    {
        public Webhook webhook { get; set; }
    }
}
