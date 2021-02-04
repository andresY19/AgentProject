using System.Collections.Generic;

namespace MonitorTrackerForm.Models
{
    class TrackerModel
    {
        public string token { get; set; }
        public List<AutomaticTakeTimeModel> AutomaticTakeTimeModel = new List<AutomaticTakeTimeModel>();
    }
}
