using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.DeleteFamilyMember
{
    public class DeleteFamilyMemberHandler : IRequestHandler<DeleteFamilyMemberCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFamilyMemberHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(DeleteFamilyMemberCommand request, CancellationToken cancellationToken)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            const string query = @"
                DELETE FROM [dbo].[Family_Links] WHERE DocId = @FamilyDocId AND Entityid = @MemberDocId;
                DELETE FROM [dbo].[Members_Links] WHERE DocId = @MemberDocId AND Entityid = @FamilyDocId;
            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@FamilyDocId", request.FamilyDocId, dbType: DbType.Int32);
            queryParameters.Add("@MemberDocId", request.MemberDocId, dbType: DbType.Int32);

            var affectedRows = await repo.ExecuteAsync(query, cancellationToken, queryParameters, transaction, "text");

            await _unitOfWork.CommitAsync(transaction);

            return affectedRows;
        }
    }
}
