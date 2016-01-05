using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace EventHub
{
    public partial class Rabbit
    {
        public Response Post_Subscription(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                var request = jsSerializer.Deserialize<Dictionary<string, dynamic>>(json);
                ch.QueueDeclare(headers["Queue"], true, false, false, null);

                string routing_key = request["subscription"]["domain"] + "." +
                                     request["subscription"]["entity"] + "." +
                                     request["subscription"]["event_type"];
                ch.QueueBind(headers["Queue"], "edu.byu", routing_key, null);

                request["subscription"].Add("identity_id", headers["Queue"]);
                request["subscription"].Add("identity_name", headers["Entity"]);

                json = jsSerializer.Serialize(request);
            }
            catch (Exception x)
            {
                throw new Exception("400 Bad Request");
            }
            return new Response() { code = "200 OK", json = json };
        }

        public Response Delete_Subscription(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                var check = Get_Subscription(json, headers, parameters);
                if (check.code.StartsWith("404"))
                    return check;

                string routing_key = (parameters["1"] + "." + parameters["2"] + "." + parameters["3"]);
                json = null;

                ch.QueueUnbind(headers["Queue"], "edu.byu", routing_key, null);
            }
            catch (Exception x)
            {
                throw new Exception("400 Bad Request");
            }
            return new Response() { code = "200 OK", json = json };
        }

        public Response Get_Subscription(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            List<string> queues;
            json = "{\"subscriptions\": {\"subscription\": [";
            string routing_key=null;
            BindingMode mode;

            try
            {
                routing_key = HttpUtility.UrlDecode(parameters["domain"]);
                routing_key += ".";
                routing_key += HttpUtility.UrlDecode(parameters["entity"]);
                routing_key += ".";
                routing_key += HttpUtility.UrlDecode(parameters["event_type"]);
                routing_key += ".";
            }
            catch (Exception x)
            {

            }

            if (routing_key != null)
            {
                routing_key = routing_key.Substring(0, routing_key.Length - 1);
                queues = GetQueues();
                mode = BindingMode.subscription_find;
            }
            else 
            {
                queues = new List<string>();
                try
                {
                    queues.Add(parameters["eca_identity_id"]);
                    mode = BindingMode.subscription_find;
                }
                catch (Exception x)
                {
                    queues.Add(headers["Queue"]);
                    mode = BindingMode.subscription_find;
                }
            }

            foreach (string q in queues)
            {
                //var bindings = GetBinding(parameters["eca_identity_id"], "edu.byu.", BindingMode.subscription_find);
                var bindings = GetBinding(q, routing_key, mode);

                if (bindings.Count == 0)
                {
                    return new Response() { code = "404 Not Found", json = null };
                }

                foreach (KeyValuePair<string, object> o in bindings)
                {
                    Binding binding = o.Value as Binding;

                    string[] parts = binding.routing_key.Split('.');

                    json += "{" +
                            "\"eca_identity_id\": \"" + binding.destination + "\"," +
                            "\"domain\": \"" + parts[0] + "." + parts[1] + "\"," +
                            "\"entity\": \"" + parts[2] + "\"," +
                            "\"event_type\": \"" + parts[3] + "\"" +
                            "},";
                }
            }

            if (json.Length > 36)
                json = json.Substring(0, json.Length - 1);  // get rid of last comma
            json += "]}}";
           
            return new Response() { code = "200 OK", json = json };
        }
        public Response Put_Subscription(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            json = null;
            return new Response() { code = "405 Method Not Allowed", json = json };
        }
    }
}
