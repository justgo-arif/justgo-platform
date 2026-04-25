using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberCommands
{
    public class DeleteMemberCommandHandler : IRequestHandler<DeleteMemberCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;

        public DeleteMemberCommandHandler(IWriteRepositoryFactory writeRepoFactory,
            IReadRepositoryFactory readRepository)
        {
            _writeRepository = writeRepoFactory;
            _readRepository = readRepository;
        }


        public async Task<Result<string>> Handle(DeleteMemberCommand request, CancellationToken cancellationToken)
        {
            const string getQuery = """
                                select um.UploadedMemberId from ResultUploadedMember um 
                                inner join ResultUploadedMemberData umd on umd.UploadedMemberId = um.UploadedMemberId 
                                where um.IsDeleted = 0 and umd.UploadedMemberDataId IN @Ids
                """;
            var members = await _readRepository.GetLazyRepository<ResultUploadedMember>().Value
                .GetListAsync(getQuery, cancellationToken, new { Ids = request.UploadedMemberIds }, null, "text");

            var uploadedMembers = members.ToList();

            if (uploadedMembers is { Count: 0 })
                return Result<string>.Failure("No active members found for the provided IDs.", ErrorType.NotFound);

            const string updateQuery = @"
                                UPDATE [dbo].[ResultUploadedMember]
                                SET [IsDeleted] = 1
                                WHERE UploadedMemberId IN @Ids
";
            var affected = await _writeRepository.GetLazyRepository<ResultUploadedMember>().Value
                .ExecuteAsync(updateQuery, cancellationToken, new { Ids = uploadedMembers.Select(m => m.UploadedMemberId).ToList() },
                    null,
                    QueryType.Text);

            return $"Deleted {affected} member(s).";
        }
    }
}
