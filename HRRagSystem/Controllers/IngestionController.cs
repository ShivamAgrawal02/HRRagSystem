using HRRagSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRRagSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngestionController : ControllerBase
    {
        private readonly IIngestionService _ingestionService;
        public IngestionController(IIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }
        [HttpPost]
        public async Task<IActionResult> IngestDocument(CancellationToken cancellationToken)
        {
            await _ingestionService.PerformIngestAsync(cancellationToken);
            return Ok();
        }
    }
}
