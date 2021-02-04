﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorTrackerForm.Models
{
    class CaptureModel
    {
        public string Ip { get; set; }
        public string Pc { get; set; }
        public string UserName { get; set; }
        public string IdCompany { get; set; }
        public byte[] Image { get; set; }
        [Display(Name = "token")]
        [JsonProperty("token")]
        public string token { get; set; }
    }
}
