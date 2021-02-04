using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MonitorTrackerForm
{
    class Response
    {
        public HttpStatusCode status { get; set; }
        public string response_code { get; set; }
        public string message { get; set; }
        public int cantidad { get; set; }
        public int pagina { get; set; }
        public string rute { get; set; }
        public data data = new data();
        public WebExceptionStatus WebException { get; set; }
        //public object data = new object();
        public List<object> error = new List<object>();
    }

    class data
    {
        public string token { get; set; }
        public bool IsLogged { get; set; }
        public int InactivityPeriod { get; set; }
        public int UploadFrecuency { get; set; }
        public int CaptureFrecuency { get; set; }
    }
}
