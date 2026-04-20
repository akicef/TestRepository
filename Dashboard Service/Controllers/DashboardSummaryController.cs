using Microsoft.AspNetCore.Mvc;
using Dashboard_Service.Services;
using Dashboard_Service.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
namespace Dashboard_Service.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardSummaryController : ControllerBase
    {
        private readonly IDashboardSummaryService _service;

        public DashboardSummaryController(IDashboardSummaryService service)
        {
            _service = service;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return Ok(result);
        }
    }
}
