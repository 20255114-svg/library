using Npgsql;
using library.Models;

namespace library.Data;

public class MemberRepository
{
    private readonly NpgsqlDataSource _db;

    public MemberRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    private static Member Read(NpgsqlDataReader r) => new Member
    {
        MemberId = r.GetInt64(0),
        FullName = r.GetString(1),
        Email = r.IsDBNull(2) ? null : r.GetString(2),
        JoinDate = r.GetFieldValue<DateOnly>(3),
        MemberType = r.GetString(4)
    };

    public async Task<List<Member>> GetAllAsync()
    {
        var list = new List<Member>();
        await using var cmd = _db.CreateCommand(
            "SELECT member_id, full_name, email, join_date, member_type FROM lending.member ORDER BY full_name");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(Read(reader));
        }
        return list;
    }

    public async Task<Member?> GetByIdAsync(long id)
    {
        await using var cmd = _db.CreateCommand(
            "SELECT member_id, full_name, email, join_date, member_type FROM lending.member WHERE member_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Read(reader) : null;
    }

    public async Task AddAsync(Member m)
    {
        await using var cmd = _db.CreateCommand(
            "INSERT INTO lending.member (full_name, email, join_date, member_type) VALUES (@name, @email, @joined, @type)");
        cmd.Parameters.AddWithValue("name", m.FullName);
        cmd.Parameters.AddWithValue("email", (object?)m.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("joined", m.JoinDate);
        cmd.Parameters.AddWithValue("type", m.MemberType);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(Member m)
    {
        await using var cmd = _db.CreateCommand(
            "UPDATE lending.member SET full_name = @name, email = @email, join_date = @joined, member_type = @type WHERE member_id = @id");
        cmd.Parameters.AddWithValue("id", m.MemberId);
        cmd.Parameters.AddWithValue("name", m.FullName);
        cmd.Parameters.AddWithValue("email", (object?)m.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("joined", m.JoinDate);
        cmd.Parameters.AddWithValue("type", m.MemberType);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        await using var cmd = _db.CreateCommand("DELETE FROM lending.member WHERE member_id = @id");
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}