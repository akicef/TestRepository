using Dashboard_Service.Models;
using Dashboard_Service.Repositories;
using Dashboard_Service.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_Service.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly ICurrentUserProvider _currentUserProvider;

        public NoteService(INoteRepository noteRepository, ICurrentUserProvider currentUserProvider)
        {
            _noteRepository = noteRepository;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<NoteDto> GetByIdAsync(long id)
        {
            return await _noteRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<NoteDto>> GetAllAsync()
        {
            return await _noteRepository.GetAllAsync();
        }

        public async Task<IEnumerable<NoteDto>> GetByCreatorIdAsync(long creatorId)
        {
            return await _noteRepository.GetByCreatorIdAsync(creatorId);
        }

        public async Task<NoteDto> CreateAsync(NoteDto note)
        {
            var currentUser = _currentUserProvider.GetCurrentUser();
            note.UserId = currentUser.Id;
            note.CreatorId = currentUser.Id;

            var id = await _noteRepository.CreateAsync(note);
            note.Id = id;
            return note;
        }

        public async Task<NoteDto> UpdateAsync(NoteDto note)
        {
            var existingNote = await _noteRepository.GetByIdAsync(note.Id);
            if (existingNote == null)
                return null;

            if (!existingNote.Editable)
                return null;

            await _noteRepository.UpdateAsync(note);
            return await _noteRepository.GetByIdAsync(note.Id);
        }

        public async Task<NoteDto> SendNoteAsync(NoteDto note, long recipientUserId)
        {
            var currentUser = _currentUserProvider.GetCurrentUser();
            note.UserId = recipientUserId;
            note.CreatorId = currentUser.Id;
            note.Editable = false;

            var id = await _noteRepository.CreateAsync(note);
            note.Id = id;
            return note;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _noteRepository.DeleteAsync(id);
        }
    }
}
