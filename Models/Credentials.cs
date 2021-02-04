using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace MonitorTrackerForm
{
    internal class Credentials
    {
        internal Credentials()
        {

        }
        public string user { get; set; }
        public string password { get; set; }
        public string companyid { get; set; }
        public bool isactive { get; set; }
        public string token { get; set; }
        public string error { get; set; }
        public string status { get; set; }
        public data data = new data();
    }    
}
