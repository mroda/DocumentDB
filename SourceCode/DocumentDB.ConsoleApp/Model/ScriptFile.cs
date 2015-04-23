using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.ConsoleApp.Model
{
    public class ScriptFile
    {
        [JsonProperty(PropertyName="fileName")]
        public string fileName { get; set; }

        [JsonProperty(PropertyName = "link")]
        public string Link { get; set; }

        [JsonProperty(PropertyName = "fileContent")]
        public Object FileContent { get; set; }
    }
}
