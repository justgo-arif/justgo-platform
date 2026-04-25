using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2;
using MobileApps.Domain.Entities.V2.Classes;
using MobileApps.Domain.Entities.V4;

namespace MobileApps.Application.Features.Class.V3.Command.MemberNoteUpsert
{
    [AtLeastOneRequired("Details", "MemberNoteTitle")]
    public class MemberNoteUpsertCommand : IRequest<bool>
    {
        public int MemberNoteId { get; set; }
        public Guid? MemberNoteGuid { get; set; }=Guid.NewGuid();
        public string? EntityType { get; set; } = "User";
        public int UserId { get; set; }
        public string? Details { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string MemberNoteTitle { get; set; }=string.Empty;
        public int OwnerId { get; set; } //ClubDocId
        public int NoteCategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsHide { get; set; } = false;
    }
    
}
