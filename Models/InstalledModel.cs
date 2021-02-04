using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorTrackerForm.Models
{
    class InstalledModel
    {
        public string token { get; set; }
        public List<InstalledProgramsViewModel> InstalledProgramsViewModel = new List<InstalledProgramsViewModel>();
    }
}
