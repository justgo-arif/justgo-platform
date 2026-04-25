using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class RecordInfo : BaseEntity
    {
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public RecordStatus RecordStatus { get; set; }
        public int RecordChangedBy { get; set; }
        public DateTime RecordChangedDate { get; set; }
        public string RecordRemarks { get; set; } = "";

        public void SetCreateInfo(int UserId)
        {
            var date = DateTime.UtcNow;
            this.RecordStatus = RecordStatus.Active;
            this.CreatedBy = UserId;
            this.CreatedDate = date;
            this.RecordChangedBy = UserId;
            this.RecordChangedDate = date;


        }

        public void SetUpdateInfo(int UserId)
        {
            var date = DateTime.UtcNow;
            this.RecordChangedBy = UserId;
            this.RecordChangedDate = date;

        }
    }
}
