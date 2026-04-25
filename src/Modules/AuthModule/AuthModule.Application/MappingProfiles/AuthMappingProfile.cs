using AuthModule.Application.DTOs.Attachments;
using AuthModule.Application.DTOs.Notes;
using AuthModule.Application.Features.Files.Commands.CreateAttachment;
using AuthModule.Application.Features.Notes.Commands.CreateNotes;
using AuthModule.Application.Features.Notes.Commands.EditNotes;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Files;
using JustGo.Authentication.Infrastructure.Notes;
using JustGoAPI.Shared.CustomAutoMapper;
using Mapster;
using System.Reflection;

namespace AuthModule.Application.MappingProfiles
{
    public class AuthMappingProfile : CustomMapsterProfile
    {
        public override void Register(TypeAdapterConfig config)
        {
            CreateAutoMaps(config,
                Assembly.Load("AuthModule.Domain"),
                Assembly.Load("AuthModule.Application"));

            config.NewConfig<Note, NoteDTO>().TwoWays();
            config.NewConfig<PagedResult<Note>, PagedResult<NoteDTO>>().TwoWays();
            config.NewConfig<KeysetPagedResult<Note>, KeysetPagedResult<NoteDTO>>().TwoWays();
            config.NewConfig<Note, CreateNotesCommand>().TwoWays();
            config.NewConfig<Note, EditNotesCommand>().TwoWays();

            config.NewConfig<Attachment, AttachmentDto>().TwoWays();
            config.NewConfig<PagedResult<Attachment>, PagedResult<AttachmentDto>>().TwoWays();
            config.NewConfig<KeysetPagedResult<Attachment>, KeysetPagedResult<AttachmentDto>>().TwoWays();
        }
    }
}
