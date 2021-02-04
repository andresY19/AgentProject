using System;

namespace MonitorTrackerForm
{
    public class AutomaticTakeTimeModel
    {
        public AutomaticTakeTimeModel(string aplication, string title, double activity, double inactivity, string ip, string pc, string user)
        {
            this.Application = aplication;
            this.Title = title;
            this.Activity = activity;
            this.Inactivity = inactivity;
            this.Ip = ip;
            this.Pc = pc;
            this.UserName = user;
            this.Date = DateTime.Now;
        }


        public double Activity { get; set; }
        public double Inactivity { get; set; }
        public string user { get; set; }
        public System.Guid Id { get; set; }
        public string Application { get; set; }
        public string Title { get; set; }
        public Nullable<int> Time { get; set; }
        public string Ip { get; set; }
        public string Pc { get; set; }
        public string UserName { get; set; }
        public string IdEmpresa { get; set; }
        public DateTime Date { get; set; }
        public string token { get; set; }
    }
}
