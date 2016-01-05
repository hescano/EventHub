using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace EventHub
{
    class JWT
    {
        public bool isValid { get; private set; }
        public string entity { get; private set; }
        public string queue { get; private set; }
        private object header{get; set;}
        private dynamic payload { get; set; }
        private object signature { get; set; }

        private JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
        public JWT(string b64)
        {
            try
            {
                string[] parts = b64.Split('.');
                header = ConvertToJson(parts[0]);
                payload = ConvertToJson(parts[1]);
                //signature= ConvertToJson(parts[2]);
                entity = payload["http://byu.edu/claims/client_surname"];
                queue = payload["http://byu.edu/claims/client_person_id"];

                isValid = true;
            }
            catch (Exception x)
            {
                isValid = false;
            }

        }

        private object ConvertToJson(string b64)
        {
            int padding = b64.Length % 4;
            if (padding > 0)
            {
                for (int i=4 - padding; i > 0; i--)
                {
                    b64 += "=";
                }
            }
            byte[] bytes = Convert.FromBase64String(b64);
            object json = jsSerializer.Deserialize<Dictionary<string, dynamic>>(Encoding.UTF8.GetString(bytes));
            return json;
        }
    }
}
