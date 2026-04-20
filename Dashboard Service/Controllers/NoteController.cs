using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dashboard_Service.Services;
using Dashboard_Service.Models;
using Dashboard_Service.Security;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dashboard_Service.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notes")]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ICurrentUserProvider _currentUserProvider;

        public NoteController(INoteService noteService, ICurrentUserProvider currentUserProvider)
        {
            _noteService = noteService;
            _currentUserProvider = currentUserProvider;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoteDto>>> GetAllNotes()
        {
            var notes = await _noteService.GetAllAsync();
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NoteDto>> GetNoteById(long id)
        {
            var note = await _noteService.GetByIdAsync(id);
            if (note == null)
                return NotFound();

            return Ok(note);
        }

        [HttpGet("creator/{creatorId}")]
        public async Task<ActionResult<IEnumerable<NoteDto>>> GetNotesByCreatorId(long creatorId)
        {
            var notes = await _noteService.GetByCreatorIdAsync(creatorId);
            return Ok(notes);
        }

        [HttpGet("my-notes")]
        public async Task<ActionResult<IEnumerable<NoteDto>>> GetMyNotes()
        {
            var currentUser = _currentUserProvider.GetCurrentUser();
            var notes = await _noteService.GetByCreatorIdAsync(currentUser.Id);
            return Ok(notes);
        }

        [HttpPost]
        public async Task<ActionResult<NoteDto>> CreateNote([FromBody] NoteDto noteDto)
        {
            if (noteDto == null)
                return BadRequest("Note cannot be null");

            var createdNote = await _noteService.CreateAsync(noteDto);
            return CreatedAtAction(nameof(GetNoteById), new { id = createdNote.Id }, createdNote);
        }

        [HttpPost("send/{id}")]
        public async Task<ActionResult<NoteDto>> SendNote(long id, [FromBody] NoteDto noteDto)
        {
            if (noteDto == null)
                return BadRequest("Note cannot be null");

            if (id <= 0)
                return BadRequest("Invalid recipient user ID");

            var sentNote = await _noteService.SendNoteAsync(noteDto, id);
            return CreatedAtAction(nameof(GetNoteById), new { id = sentNote.Id }, sentNote);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<NoteDto>> UpdateNote(long id, [FromBody] NoteDto noteDto)
        {
            if (noteDto == null)
                return BadRequest("Note cannot be null");

            if (id != noteDto.Id)
                return BadRequest("ID mismatch");

            var existingNote = await _noteService.GetByIdAsync(id);
            if (existingNote == null)
                return NotFound();

            if (!existingNote.Editable)
                return StatusCode(403, "This note cannot be edited. The editable flag is set to false.");

            var updatedNote = await _noteService.UpdateAsync(noteDto);
            if (updatedNote == null)
                return NotFound();

            return Ok(updatedNote);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNote(long id)
        {
            var result = await _noteService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Note not found or already deleted." });

            return Ok(new { message = "Note successfully deleted." });
        }
    }
}
