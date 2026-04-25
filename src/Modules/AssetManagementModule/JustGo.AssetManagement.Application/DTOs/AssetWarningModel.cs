using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetNotificationModel
    {
        public SectionNotification BasicDetails { get; set; }
        public SectionNotification AdditionalDetails { get; set; }
        public SectionNotification License { get; set; }
        public SectionNotification Credential { get; set; }
        public SectionNotification Lease { get; set; }
        public SectionNotification Transfer { get; set; }
        public SectionNotification Retention { get; set; }
    }

    public class SectionWarning
    {
        public bool HasWarning { get; set; }
    }

    public class SectionNotification
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
