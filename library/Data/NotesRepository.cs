using Npgsql;
using library.Models;

namespace library.Data;

public class NoteRepository
{
    private readonly NpgsqlDataSource _db;

    public NoteRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    private static Note Read(NpgsqlDataReader r) => new Note
    {
        NoteId = r.GetInt64(0),
        Title = r.GetString(1),
        Body = r.IsDBNull(2) ? null : r.GetString(2),
        CreatedOn = r.GetFieldValue<DateOnly>(3)
    };

    public async Task<List<Note>> GetAllAsync()
    {
        var list = new List<Note>();
        await using var cmd = _db.CreateCommand(
            "SELECT note_id, title, body, created_on FROM lending.note ORDER BY created_on DESC, note_id DESC");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(Read(reader));
        }
        return list;
    }

    public async Task<Note?> GetByIdAsync(long id)
    {
        await using var cmd = _db.CreateCommand(
            "SELECT note_id, title, body, created_on FROM lending.note WHERE note_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
    }

    public async Task AddAsync(Note n)
    {
        await using var cmd = _db.CreateCommand(
            "INSERT INTO lending.note (title, body, created_on) VALUES (@title, @body, @created)");
        cmd.Parameters.AddWithValue("title", n.Title);
        cmd.Parameters.AddWithValue("body", (object?)n.Body ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created", n.CreatedOn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Note n)
    {
        await using var cmd = _db.CreateCommand(
            "UPDATE lending.note SET title = @title, body = @body, created_on = @created WHERE note_id = @id");
        cmd.Parameters.AddWithValue("id", n.NoteId);
        cmd.Parameters.AddWithValue("title", n.Title);
        cmd.Parameters.AddWithValue("body", (object?)n.Body ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created", n.CreatedOn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var cmd = _db.CreateCommand("DELETE FROM lending.note WHERE note_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}