using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentProduct;
using JustGo.Finance.Application.Features.Products.Queries.GetProducts;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/products")]
    [ApiController]


    [Tags("Finance/Products")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Tags("Finance/Products")]
        [HttpGet("list")]
        public async Task<IActionResult> GetPaymentProduct([FromQuery] GetProductsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
