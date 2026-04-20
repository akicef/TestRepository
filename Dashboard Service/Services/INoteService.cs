using Dashboard_Service.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_Service.Services
{
    public interface INoteService
    {
        Task<NoteDto> GetByIdAsync(long id);
        Task<IEnumerable<NoteDto>> GetAllAsync();
        Task<IEnumerable<NoteDto>> GetByCreatorIdAsync(long creatorId);
        Task<NoteDto> CreateAsync(NoteDto note);
        Task<NoteDto> UpdateAsync(NoteDto note);
        Task<NoteDto> SendNoteAsync(NoteDto note, long recipientUserId);
        Task<bool> DeleteAsync(long id);
    }
}
