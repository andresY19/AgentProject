using System;

namespace MonitorTrackerForm.Models
{
    public class InstalledProgramsViewModel
    {

        public string Name { get; set; }
        public string Vertion { get; set; }
        public DateTime? InstalledDate { get; set; }
        public string Size { get; set; }
        public string User { get; set; }
        public string Pc { get; set; }
        public Guid Id_Empresa { get; set; }
    }
}
