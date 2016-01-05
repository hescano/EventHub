using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventHub
{
        public partial class Rabbit
        {
            public Response Post_Event(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
            {
                try
                {
                    var request = jsSerializer.Deserialize<Dictionary<string, dynamic>>(json);

                    for (int i = 0; i < request["events"]["event"].Count; i++)
                    {
                        try
                        {
                            var bindings = GetBinding("ENTITIES", request["events"]["event"][i]["event_header"]["entity"], BindingMode.entity_find);
                            if (bindings.Count > 0) // look in the "ENTITIES" queue
                            {
                                if (!bindings[request["events"]["event"][i]["event_header"]["entity"]].arguments.entity_ega.Equals(headers["Queue"]))
                                {
                                    request["events"]["event"][i]["event_header"].Add("http_code", "401");
                                    continue;
                                }
                            }

                            String now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ff") + "Z";
                            now = now.Replace(" ", "T");

                            string guid = Guid.NewGuid().ToString();

                            request["events"]["event"][i]["event_header"].Add("event_dt", now);
                            request["events"]["event"][i]["event_header"].Add("event_id", guid);

                            string routing_key = request["events"]["event"][i]["event_header"]["domain"] + "." +
                                                    request["events"]["event"][i]["event_header"]["entity"] + "." +
                                                    request["events"]["event"][i]["event_header"]["event_type"];

                            json = "{\"event\":" + jsSerializer.Serialize(request["events"]["event"][i]) + "}";

                            ch.BasicPublish("edu.byu", routing_key, null, System.Text.Encoding.ASCII.GetBytes(json));

                            List<string> remove = new List<string>();

                            foreach (KeyValuePair<string, object> o in request["events"]["event"][i])
                            {
                                if (!o.Key.Equals("event_header"))
                                    remove.Add(o.Key);
                            }

                            foreach (string s in remove)
                            {
                                request["events"]["event"][i].Remove(s);
                            }

                            request["events"]["event"][i]["event_header"].Add("status", "200");

                        }
                        catch (Exception x)
                        {
                            request["events"]["event"][i]["event_header"].Add("http_code", "400");

                        }
                    }
                    json = jsSerializer.Serialize(request);

                }
                catch (Exception x)
                {
                    json = null;
                    return new Response() { code = "400 Bad Request", json = json };
                }

                return new Response() { code = "200 OK", json = json };
            }

            public Response Delete_Event(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
            {
                return new Response() { code = "405 Method Not Allowed", json = json };
            }
            public Response Get_Event(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
            {
                int count = 1;
                try
                {
                    count = Int32.Parse(parameters["count"]);
                }
                catch (Exception x)
                {
                    count = 1;
                }

                bool ack = true;
                try
                {
                    if (parameters["acknowledge"].Equals("true"))
                        ack = true;
                }
                catch (Exception x)
                {
                    ack = false;
                }

                json = "{\"events\": {\"event\": [";

                for (int i = 0; i < count; i++)
                {
                    var result = ch.BasicGet(headers["Queue"], ack);
                    if (result == null)
                        break;

                    if (ack == false)
                        ch.BasicRecover(true);
                    else
                        ch.BasicAck(result.DeliveryTag, false);


                    string str = System.Text.Encoding.Default.GetString(result.Body);
                    var message = jsSerializer.Deserialize<Dictionary<string, dynamic>>(str);
                    message["event"]["event_header"].Add("acknowledged", ack ? "true" : "false");

                    json += jsSerializer.Serialize(message["event"]);
                    json += ",";

                }

                if (json.Length > 22)
                {
                    json = json.Substring(0, json.Length - 1); // remove last comma
                    json += "]}}";
                    return new Response() { code = "200 OK", json = json };
                }

                json = null;
                return new Response() { code = "204 No Content", json = json };
            }

            public Response Put_Event(string json, Dictionary<string, string> headers, Dictionary<string, string> parameters)
            {
                while (true)
                {
                    try
                    {
                        var result = ch.BasicGet(headers["Queue"], false);
                        if (result == null)
                        {
                            ch.BasicRecover(true);
                            break;
                        }
                        
                        var message = jsSerializer.Deserialize<Dictionary<string, dynamic>>(System.Text.Encoding.ASCII.GetString(result.Body));

                        if (message["event"]["event_header"]["event_id"].Equals(parameters["1"])) 
                        { 
                            ch.BasicAck(result.DeliveryTag, true);

                            List<string> remove = new List<string>();
                            foreach (KeyValuePair<string, object> o in message["event"])
                            {
                                if (!o.Key.Equals("event_header"))
                                    remove.Add(o.Key);
                            }

                            foreach (string s in remove)
                            {
                                message["event"].Remove(s);
                            }

                            message["event"]["event_header"].Add("acknowledged", "true");

                            json = jsSerializer.Serialize(message);

                            return new Response() { code = "200 OK", json = json };
                        }
                    }
                    catch (Exception x) //OperationInterruptedException ex)
                    {
                        // The consumer was removed, either through
                        // channel or connection closure, or through the
                        // action of IModel.BasicCancel().
                        ch.BasicRecover(true);
                        return new Response() { code = "404 Not Found", json = null };
                    }
                }
                throw new Exception("404 Not Found");
            }
        }
}
