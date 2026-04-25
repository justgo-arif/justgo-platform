using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V2.Classes
{
    public class NoteModel
    {
        public int AttendeeDetailNoteId { get; set; }
        public int AttendeeDetailsId { get; set; }
        public string Note { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; } 
    }
}
