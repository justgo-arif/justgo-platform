using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Data;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Commands.UpdateMemberFamilyName
{
    public class UpdateMemberFamilyNameHandler : IRequestHandler<UpdateMemberFamilyNameCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMemberFamilyNameHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(UpdateMemberFamilyNameCommand request, CancellationToken cancellationToken)
        {

            var repo = _writeRepositoryFactory.GetLazyRepository<Family_Default>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            string updateSql = """
                UPDATE fd SET fd.Familyname = @Familyname FROM [Family_Default] fd
                INNER JOIN Document d on d.DocId=fd.DocId WHERE d.SyncGuid = @FamilySyncGuid

                UPDATE [dbo].[Families] SET FamilyName= @Familyname WHERE RecordGuid=@FamilySyncGuid
                """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@FamilySyncGuid", request.FamilySyncGuid, DbType.Guid);
            queryParameters.Add("@Familyname", request.FamilyName, DbType.String);

            var affectedRows = await repo.ExecuteAsync(updateSql, cancellationToken, queryParameters, transaction, "Text");
            await _unitOfWork.CommitAsync(transaction);


            return affectedRows;


        }

    }
}
