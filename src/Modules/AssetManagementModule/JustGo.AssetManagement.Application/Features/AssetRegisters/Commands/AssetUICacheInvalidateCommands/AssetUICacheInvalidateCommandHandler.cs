using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetUICacheInvalidateCommands
{
    public class AssetUICacheInvalidateCommandHandler : IRequestHandler<AssetUICacheInvalidateCommand, bool>
    {

        private readonly IHybridCacheService _cache;
        public AssetUICacheInvalidateCommandHandler(
            IHybridCacheService cache)
        {
            _cache = cache;
        }

        public async Task<bool> Handle(AssetUICacheInvalidateCommand command, CancellationToken cancellationToken)
        {
            await _cache.RemoveByTagAsync("policy_ext_asset_allow_ui_detail");
            await _cache.RemoveByTagAsync("policy_ext_asset_basic_fields");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1259");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1313");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1334");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1366");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1728");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1735");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1742");
            await _cache.RemoveByTagAsync("policy_ext_asset_fields_14_1773");

            return true;
        }

    }
}
