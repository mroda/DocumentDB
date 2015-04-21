﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DocumentDB.ConsoleApp.Model
{
    public class ARMTemplateFile
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Folder { get; set; }

        public string FileName { get; set; }

        public Object FileContent { get; set; }
    }
}
