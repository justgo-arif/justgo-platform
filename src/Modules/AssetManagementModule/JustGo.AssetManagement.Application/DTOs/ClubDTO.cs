using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{

    public class ClubDTO
    {
        public string ClubId { get; set; }             
        public int ClubDocId { get; set; }            
        public string ClubName { get; set; }          
        public string ClubImage { get; set; }         
        public string ClubReferenceId { get; set; }     
    }
    public class ClubWithCountDTO : ClubDTO
    {
        public int TotalRows { get; set; }
    }

}
