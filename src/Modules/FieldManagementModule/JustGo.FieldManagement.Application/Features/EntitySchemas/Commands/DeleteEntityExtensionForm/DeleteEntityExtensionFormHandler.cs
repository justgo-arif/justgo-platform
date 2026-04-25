using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.DeleteEntityExtensionForm
{
    public class DeleteEntityExtensionFormHandler : IRequestHandler<DeleteEntityExtensionFormCommand, EntityExtensionSchema>
    {
        string modulePrefix = "EntityExtensionDataService";
        private readonly IWriteRepositoryFactory _writeRepository;
        IUnitOfWork _unitOfWork;
        public DeleteEntityExtensionFormHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<EntityExtensionSchema> Handle(DeleteEntityExtensionFormCommand request, CancellationToken cancellationToken)
        {
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TabItemId", request.tabItemId);
            await _writeRepository.GetLazyRepository<EntityExtensionSchema>().Value.ExecuteAsync("DeleteEntityExtensionFormByTabId", cancellationToken, queryParameters, dbTransaction);
            await _unitOfWork.CommitAsync(dbTransaction);
            //RedisCacheHandler.DeleteGlobal(schema.ExId.ToString(), modulePrefix);
            return request;
        }
    }
}
