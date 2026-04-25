using Asp.Versioning;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/cache")]
    [ApiController]
    [Tags("Authentication/Cache")]    
    public class CacheInvalidationController : ControllerBase
    {
        private readonly IHybridCacheService _cache;

        public CacheInvalidationController(IHybridCacheService cache)
        {
            _cache = cache;
        }

        [CustomAuthorize]
        [HttpPost("invalidate/by-key/{key}")]
        public async Task<IActionResult> InvalidateByKey(string key)
        {
            await _cache.RemoveAsync(key);
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("invalidate/by-tag/{tag}")]
        public async Task<IActionResult> InvalidateByTag(string tag)
        {
            await _cache.RemoveByTagAsync(tag);
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("invalidate/pattern")]
        public async Task<IActionResult> InvalidateByPattern([FromBody] CacheInvalidationRequest request)
        {
            // For multiple keys or patterns
            foreach (var key in request.Keys)
            {
                await _cache.RemoveAsync(key);
            }
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
        [TenantFromHeader]
        [HttpPost("invalidate/by-key-public/{key}")]
        public async Task<IActionResult> InvalidateByKeyPublic(string key)
        {
            await _cache.RemoveAsync(key);
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
        [TenantFromHeader]
        [HttpPost("invalidate/by-tag-public/{tag}")]
        public async Task<IActionResult> InvalidateByTagPublic(string tag)
        {
            await _cache.RemoveByTagAsync(tag);
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
        [TenantFromHeader]
        [HttpPost("invalidate/pattern-public")]
        public async Task<IActionResult> InvalidateByPatternPublic([FromBody] CacheInvalidationRequest request)
        {
            // For multiple keys or patterns
            foreach (var key in request.Keys)
            {
                await _cache.RemoveAsync(key);
            }
            var result = true;
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
