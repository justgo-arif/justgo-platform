using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.FinanceGridViewDtos;
using JustGo.Finance.Application.Features.FinanceGridView.Commands.CreateUpdate;
using JustGo.Finance.Application.Features.FinanceGridView.Commands.DeleteView;
using JustGo.Finance.Application.Features.FinanceGridView.Commands.PinUnpin;
using JustGo.Finance.Application.Features.FinanceGridView.Commands.ShareView;
using JustGo.Finance.Application.Features.FinanceGridView.Queries.GetFinanceGridViewById;
using JustGo.Finance.Application.Features.FinanceGridView.Queries.GetFinanceGridViews;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Tags("Finance/Views")]
    [Route("api/v{version:apiVersion}/payments/views")]
    [ApiController]
    public class FinanceGridViewController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICustomError _error;

        public FinanceGridViewController(IMediator mediator, ICustomError error)
        {
            _mediator = mediator;
            _error = error;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetViews(
            [FromQuery] string merchantId,
            [FromQuery] int entityType,
            CancellationToken cancellationToken)
        {
            var query = new GetFinanceGridViewsQuery(merchantId, entityType);
            var views = await _mediator.Send(query, cancellationToken);

            return Ok(new ApiResponse<List<FinanceGridViewDto>, object>(views, "Fetched successfully."));
        }

        [HttpGet("view/{viewId}")]
        public async Task<IActionResult> GetView(int viewId, CancellationToken cancellationToken)
        {
            var query = new GetFinanceGridViewByIdQuery(viewId);
            var view = await _mediator.Send(query, cancellationToken);

            if (view == null)
                return NotFound(new ApiResponse<string, object>("View not found."));

            return Ok(new ApiResponse<FinanceGridViewDto, object>(view, "Fetched successfully."));
        }

        [HttpPost("saveUpdateView")]
        public async Task<IActionResult> SaveUpdateView(CreateUpdateFinanceGridViewCommand request, CancellationToken cancellationToken)
        {
            var isSaved = await _mediator.Send(request, cancellationToken);

            if (!isSaved)
            {
                _error.NotFound<object>("Save failed.");
                return new EmptyResult();
            }

            return Ok(new ApiResponse<string, object>("Saved successfully."));
        }
        [HttpPatch("pinUnpinView")]
        public async Task<IActionResult> PinUnpinView(PinUnpinFinanceGridViewCommand request, CancellationToken cancellationToken)
        {
            var isUpdated = await _mediator.Send(request, cancellationToken);

            if (!isUpdated)
            {
                _error.NotFound<object>("Pin/Unpin action failed.");
                return new EmptyResult();
            }
            var message = request.IsPinned ? "View pinned successfully." : "View unpinned successfully.";
            return Ok(new ApiResponse<string, object>(message));
        }
        [HttpPatch("shareView")]
        public async Task<IActionResult> ShareView(ShareFinanceGridViewCommand request, CancellationToken cancellationToken)
        {
            var isShared = await _mediator.Send(request, cancellationToken);

            if (!isShared)
            {
                _error.NotFound<object>("Share action failed.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<string, object>("View shared successfully."));
        }
        [HttpDelete("deleteView/{viewId}")]
        public async Task<IActionResult> DeleteView(int viewId, CancellationToken cancellationToken)
        {
            var command = new DeleteFinanceGridViewCommand(viewId);
            var isDeleted = await _mediator.Send(command, cancellationToken);
            if (!isDeleted)
            {
                _error.NotFound<object>("Delete action failed or view not found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<string, object>("View deleted successfully."));
        }

    }

}
