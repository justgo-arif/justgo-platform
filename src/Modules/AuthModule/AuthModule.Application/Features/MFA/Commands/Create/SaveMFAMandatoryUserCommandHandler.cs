using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json.Linq;

namespace AuthModule.Application.Features.MFA.Commands.Create
{
    public class SaveMFAMandatoryUserCommandHandler : IRequestHandler<SaveMFAMandatoryUserCommand, bool>
    {
        private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;

        public SaveMFAMandatoryUserCommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository)
        {
            _writeRepository = writeRepository;
        }
        public async Task<bool> Handle(SaveMFAMandatoryUserCommand request, CancellationToken cancellationToken)
        {
            string sql = @"if not exists (select UserId from UserMFA where UserId = @UserId)
                        begin
                        	insert into UserMFA (UserId,BypassForceSetup)
                        	values (@UserId,@updateFlag)
                        end
                        else
                        begin
                        	update UserMFA set BypassForceSetup = @updateFlag where UserId = @UserId
                        end";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            queryParameters.Add("@updateFlag", request.UpdateFlag);



            var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

            return result >= 0;
        }
    }
}
