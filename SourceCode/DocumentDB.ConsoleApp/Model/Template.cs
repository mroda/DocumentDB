using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.ConsoleApp.Model
{
    public class Template
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "templateUpdated")]
        public string TemplateUpdated { get; set; }

        [JsonProperty(PropertyName = "link")]
        public string Link { get; set; }

        [JsonProperty(PropertyName = "gitHubPictureProfileLink")]
        public string GitHubPictureProfileLink { get; set; }

        [JsonProperty(PropertyName = "author")]
        public string Author { get; set; }

        [JsonProperty(PropertyName = "readmeLink")]
        public string ReadmeLink { get; set; }

        [JsonProperty(PropertyName="scriptFiles")]
        public ScriptFile[] ScriptFiles { get; set; }
    }
}
