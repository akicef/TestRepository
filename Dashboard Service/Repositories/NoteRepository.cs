using Dapper;
using Npgsql;
using Dashboard_Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Dashboard_Service.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly string _connectionString;

        public NoteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<NoteDto> GetByIdAsync(long id)
        {
            using var db = CreateConnection();
            return await db.QueryFirstOrDefaultAsync<NoteDto>(
                "SELECT id, title, text, editable, user_id as UserId, creator_id as CreatorId, last_modified as LastModified, disabled FROM notes WHERE id = @Id",
                new { Id = id });
        }

        public async Task<IEnumerable<NoteDto>> GetAllAsync()
        {
            using var db = CreateConnection();
            return await db.QueryAsync<NoteDto>(
                "SELECT id, title, text, editable, user_id as UserId, creator_id as CreatorId, last_modified as LastModified, disabled FROM notes WHERE disabled = false ORDER BY last_modified DESC");
        }

        public async Task<IEnumerable<NoteDto>> GetByCreatorIdAsync(long creatorId)
        {
            using var db = CreateConnection();
            return await db.QueryAsync<NoteDto>(
                "SELECT id, title, text, editable, user_id as UserId, creator_id as CreatorId, last_modified as LastModified, disabled FROM notes WHERE creator_id = @CreatorId AND disabled = false ORDER BY last_modified DESC",
                new { CreatorId = creatorId });
        }

        public async Task<long> CreateAsync(NoteDto note)
        {
            using var db = CreateConnection();
            var id = await db.ExecuteScalarAsync<long>(
                "INSERT INTO notes (title, text, editable, user_id, creator_id, last_modified, disabled) " +
                "VALUES (@Title, @Text, @Editable, @UserId, @CreatorId, @LastModified, @Disabled) " +
                "RETURNING id",
                new
                {
                    Title = note.Title,
                    Text = note.Text,
                    Editable = note.Editable,
                    UserId = note.UserId,
                    CreatorId = note.CreatorId,
                    LastModified = DateTime.UtcNow,
                    Disabled = false
                });
            return id;
        }

        public async Task<bool> UpdateAsync(NoteDto note)
        {
            using var db = CreateConnection();
            var result = await db.ExecuteAsync(
                "UPDATE notes SET title = @Title, text = @Text, editable = @Editable, user_id = @UserId, last_modified = @LastModified " +
                "WHERE id = @Id",
                new
                {
                    Id = note.Id,
                    Title = note.Title,
                    Text = note.Text,
                    Editable = note.Editable,
                    UserId = note.UserId,
                    LastModified = DateTime.UtcNow
                });
            return result > 0;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            using var db = CreateConnection();
            var result = await db.ExecuteAsync(
                "UPDATE notes SET disabled = true, last_modified = @LastModified WHERE id = @Id",
                new { Id = id, LastModified = DateTime.UtcNow });
            return result > 0;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            using var db = CreateConnection();
            var count = await db.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM notes WHERE id = @Id",
                new { Id = id });
            return count > 0;
        }
    }
}
