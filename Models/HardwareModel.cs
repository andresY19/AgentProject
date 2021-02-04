using System.Collections.Generic;

namespace MonitorTrackerForm.Models
{
    class HardwareModel
    {
        public string token { get; set; }
        public List<InstalledHardwareViewModel> InstalledHardwareViewModel = new List<InstalledHardwareViewModel>();
    }
}
