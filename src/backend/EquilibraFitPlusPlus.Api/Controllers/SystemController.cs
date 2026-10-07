using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// System metadata endpoints.
/// </summary>
[Route("api/v1/system")]
public sealed class SystemController : ApiControllerBase
{
    /// <summary>
    /// Returns API metadata.
    /// </summary>
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult Info()
    {
        return Ok(new
        {
            name = "EquilibraFit++ API",
            version = "1.0.0",
            runtime = ".NET 10",
            philosophy = "Sua saúde. Seu ritmo. Seu equilíbrio."
        });
    }
}
