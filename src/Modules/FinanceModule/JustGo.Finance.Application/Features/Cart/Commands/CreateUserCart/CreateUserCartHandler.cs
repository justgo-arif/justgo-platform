using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.Cart.Commands.CreateUserCart
{

    public class CreateUserCartHandler : IRequestHandler<CreateUserCartCommand, int>
    {

        private readonly IWriteRepositoryFactory _writeRepository;

        public CreateUserCartHandler(IWriteRepositoryFactory writeRepository)
        {
            _writeRepository = writeRepository;
        }

        public async Task<int> Handle(CreateUserCartCommand command, CancellationToken cancellationToken)
        {
            const string sql = @" Declare @NewId int
                                insert into Document (
                                  RepositoryId, Type, Title, RegisterDate, 
                                  Location, IsLocked, Status, Tag, Version, 
                                  UserId
                                ) 
                                values 
                                  (
                                    10, 
                                    'Electronic', 
                                    'ShoppingCart Document', 
                                    getdate(), 
                                    'Virtual', 
                                    0, 
                                    0, 
                                    0, 
                                    1, 
                                    @OwnerUserId
                                  ) 
                                set @NewId =SCOPE_IDENTITY() 
                                insert into Document_10_58 (DocId, Version) 
                                values (@NewId, 1) 

                                update 
                                  Shoppingcart_Default 
                                set 
                                  OwnerUserId = @OwnerUserId, 
                                  Isactive = 1
                                where 
                                  DocId = @NewId
                                select @NewId as MerchantId
                                ";
            var parameters = new
            {
                OwnerUserId = command.UserId
            };
            var cartid = await _writeRepository.GetLazyRepository<MerchantLookup>().Value.ExecuteScalarAsync<int>(sql, cancellationToken, parameters, null, "text");
            if (cartid <= 0)
                throw new InvalidOperationException($"Cart can't be created for User Id: {command.UserId}");
            return cartid;
        }
    }

}
