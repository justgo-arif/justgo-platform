using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    //public class AssetMetadataInfo
    //{
    //    public string AllowedMultiOwner { get; set; }
    //    public List<Dictionary<string, List<string>>> CoreFieldLabelConfig { get; set; }
    //    public List<Dictionary<string, List<string>>> CoreFieldValueConfig { get; set; }
    //    public List<string> AssetOwnerTiers { get; set; }
    //    public dynamic Permission { get; set; }
    //}

    public class AssetMetadataInfo
    {
        public string AllowedMultiOwner { get; set; }
        public List<CoreFieldLabelConfigItem> CoreFieldLabelConfig { get; set; }
        public List<CoreFieldValueConfigItem> CoreFieldValueConfig { get; set; }
        public List<string> AssetOwnerTiers { get; set; }
        public Permission Permission { get; set; }
    }

    public class CoreFieldLabelConfigItem
    {
        public string Group { get; set; }
        public string IssueDate { get; set; }
    }

    public class CoreFieldValueConfigItem
    {
        public Dictionary<string, List<string>> Values { get; set; }
    }

    public class Permission
    {
        public List<string> View { get; set; }
        public List<string> Delete { get; set; }
        public List<string> Create { get; set; }
        public List<string> Update { get; set; }
        public List<string> Approve { get; set; }
    }
}
