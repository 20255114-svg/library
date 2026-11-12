using library.Models;
using Npgsql;

namespace library.Data;

// Every SQL statement about members lives in this one class.
public class MemberRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public MemberRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Copies the current row into a Member. Positions 0 to 4 match every member SELECT.
    private static Member ReadMember(NpgsqlDataReader reader)
    {
        return new Member
        {
            MemberId = reader.GetInt64(0),
            FullName = reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            JoinDate = reader.GetFieldValue<DateOnly>(3),
            MemberType = reader.GetString(4)
        };
    }

    // READ: every member.
    public async Task<List<Member>> GetAllAsync()
    {
        const string sql =
            "SELECT member_id, full_name, email, join_date, member_type " +
            "FROM lending.member ORDER BY member_id;";

        var members = new List<Member>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            members.Add(ReadMember(reader));
        }
        return members;
    }

    // READ: one member, or null when there is no such member.
    public async Task<Member?> GetByIdAsync(long id)
    {
        const string sql =
            "SELECT member_id, full_name, email, join_date, member_type " +
            "FROM lending.member WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadMember(reader) : null;
    }

    // CREATE: add a member.
    public async Task AddAsync(Member member)
    {
        const string sql =
            "INSERT INTO lending.member (full_name, email, join_date, member_type) " +
            "VALUES (@full_name, @email, @join_date, @member_type);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("full_name", member.FullName);
        command.Parameters.AddWithValue("email", (object?)member.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("join_date", member.JoinDate);
        command.Parameters.AddWithValue("member_type", member.MemberType);
        await command.ExecuteNonQueryAsync();
    }

    // UPDATE: change a member.
    public async Task UpdateAsync(Member member)
    {
        const string sql =
            "UPDATE lending.member " +
            "SET full_name = @full_name, email = @email, join_date = @join_date, member_type = @member_type " +
            "WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", member.MemberId);
        command.Parameters.AddWithValue("full_name", member.FullName);
        command.Parameters.AddWithValue("email", (object?)member.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("join_date", member.JoinDate);
        command.Parameters.AddWithValue("member_type", member.MemberType);
        await command.ExecuteNonQueryAsync();
    }

    // DELETE: remove a member. A member with loans is refused by the foreign key.
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM lending.member WHERE member_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }
}