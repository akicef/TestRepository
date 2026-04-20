using Dashboard_Service.Models;
using System.Threading.Tasks;

namespace Dashboard_Service.Services
{
    public interface IDashboardSummaryService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync();
    }
}
