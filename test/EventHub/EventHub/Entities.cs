using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventHub
{
    public partial class Rabbit
    {
        public Response Put_Entity(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            json = null;
            return new Response() { code = "405 Method Not Allowed", json = json };

        }
        public Response Post_Entity(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                var request = jsSerializer.Deserialize<Dictionary<string, dynamic>>(json);

                parameters["1"] = request["entity_definition"]["domain"];
                parameters["2"] = request["entity_definition"]["entity"];
                Dictionary<string, object> arguments = new Dictionary<string, object>();

                arguments.Add("entity_ega", headers["Queue"]);
                arguments.Add("domain", request["entity_definition"]["domain"]);
                arguments.Add("description", request["entity_definition"]["description"]);

                Delete_Entity(json, headers, parameters);
                
                ch.QueueDeclare("ENTITIES", true, false, false, null);
                ch.QueueBind("ENTITIES", request["entity_definition"]["domain"], request["entity_definition"]["entity"], arguments);

                request["entity_definition"].Add("entity_ega", headers["Queue"]);
                json = jsSerializer.Serialize(request);
            }
            catch (Exception x)
            {
                throw new Exception("400 Bad Request");
            }

            return  new Response() { code = "200 OK", json = json };
        }

        public Response Delete_Entity(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                json = null;

                if (parameters["1"] == null || parameters["2"] == null)
                    return new Response() { code = "400 Bad Request", json = json };

                var binding = GetBinding("ENTITIES", parameters["2"], BindingMode.entity_post);
                if (binding.Count == 0)
                    return new Response() { code = "404 Not Found", json = json };

                ch.QueueUnbind("ENTITIES", parameters["1"], parameters["2"], binding);
            }
            catch (Exception x)
            {
                throw new Exception("400 Bad Request");
            }
            return new Response() { code = "200 OK", json = json };
        }

        public Response Get_Entity(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
        {
            try
            {
                json = null;

                var bindings = GetBinding("ENTITIES", headers["Queue"], BindingMode.entity_get);
                if (bindings.Count == 0)
                    return new Response() { code = "404 Not Found", json = json };


                json = "{\"entities\": {\"entity_definition\": [";

                foreach (KeyValuePair<string, object> o in bindings)
                {

                    Binding binding = o.Value as Binding;

                    json += "{" +
                            "\"entity\": \"" + binding.routing_key + "\"," +
                            "\"description\": \"" + binding.arguments.description + "\"," +
                            "\"domain\": \"" + binding.arguments.domain + "\"," +
                            "\"entity_ega\": \"" + binding.arguments.entity_ega + "\"" +
                          "},";
                }

                json = json.Substring(0, json.Length - 1);  // get rid of last comma
                json += "]}}";
            }
            catch (Exception x)
            {
                throw new Exception("400 Bad Request");
            }

            return new Response() { code = "200 OK", json = json };
        }
    }
}
