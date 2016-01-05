using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventHub
{
    public class Arguments
    {
        public string address { get; set; }
        public string acknowledge { get; set; }
        public string method { get; set; }
        public int port { get; set; }
        public string security_key { get; set; }
        public string security_option { get; set; }
        public string last_attempted { get; set; }
        public string description { get; set; }
        public string entity_ega { get; set; }
        public string domain { get; set; }
    }

    public class Binding
    {
        public string source { get; set; }
        public string vhost { get; set; }
        public string destination { get; set; }
        public string destination_type { get; set; }
        public string routing_key { get; set; }
        public Arguments arguments { get; set; }
        public string properties_key { get; set; }
    }

    public class Bindings
    {
        public List<Binding> binding { get; set; }
    }

    public class B
    {
        public Bindings bindings { get; set; }
    }
}
