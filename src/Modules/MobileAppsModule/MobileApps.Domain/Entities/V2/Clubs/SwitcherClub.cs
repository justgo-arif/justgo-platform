using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2
{
    public  class SwitcherClub
    {
        public int DocId { get; set; }
        public string Name { get; set; } = default!;
        public string Image { get; set; } = default!;
        public string SyncGuid { get; set; } = default!;
        public string Reference { get; set; } = default!;
        public string MerchantGuid { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public bool IsEventExist { get; set; } = false;
        public bool IsClassExist { get; set; } = false;


        [NotMapped]
        public string ImagePath { get; set; } = default!;
     
    }

    public class SwitcherLazyClub
    {
        public int DocId { get; set; }
        public string Name { get; set; } = default!;
        public string Image { get; set; } = default!;
        public string SyncGuid { get; set; } = default!;
        public string Reference { get; set; } = default!;
        public string MerchantGuid { get; set; } = default!;
        public string EntityType { get; set; } = default!;
        public string EmailAddress { get; set; } = default!;
        public bool IsEventExist { get; set; } = false;
        public bool IsClassExist { get; set; } = false;


        [NotMapped]
        public string ImagePath { get; set; } = default!;

    }
}
