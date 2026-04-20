using Dashboard_Service.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_Service.Repositories
{
    public interface INoteRepository
    {
        Task<NoteDto> GetByIdAsync(long id);
        Task<IEnumerable<NoteDto>> GetAllAsync();
        Task<IEnumerable<NoteDto>> GetByCreatorIdAsync(long creatorId);
        Task<long> CreateAsync(NoteDto note);
        Task<bool> UpdateAsync(NoteDto note);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }
}
