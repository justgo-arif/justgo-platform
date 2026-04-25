using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Application.Features.EntitySchemas.Queries.GetEntityExtensionUi;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.MemberProfile.Application.Features.MemberNotes.Queries.GetNotesCategory;
using JustGo.MemberProfile.Application.Features.Members.Queries.MemberDetailsMenu;
using JustGo.MemberProfile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Queries.NotesCategory
{
    public class NotesCategoryHandler : IRequestHandler<NotesCategoryQuery, List<NoteCategories>>
    {
        private readonly LazyService<IReadRepository<NoteCategories>> _readRepository;

        public NotesCategoryHandler(
            LazyService<IReadRepository<NoteCategories>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<NoteCategories>> Handle(NotesCategoryQuery request, CancellationToken cancellationToken = default)
        {
            string sql = """
                         SELECT  [NoteCategoryId]
                             ,[NoteCategoryName]
                             ,[NoteCategoryStatus]
                             ,[IsActive]
                         FROM [dbo].[NoteCategories] where [IsActive] = 1
                         """;


            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, commandType: "text")).ToList();
            return result;
        }
    }
}
