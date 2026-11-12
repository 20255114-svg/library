using library.Models;
using Npgsql;

namespace library.Data;

// Every SQL statement about loans lives in this one class.
public class LoanRepository
{
    public const int MaxOpenLoans = 3; // the most books one member may have out at once

    private readonly NpgsqlDataSource _dataSource;

    public LoanRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Copies the current row into a LoanView. Positions 0 to 7 match every loan SELECT.
    private static LoanView ReadLoan(NpgsqlDataReader reader)
    {
        return new LoanView
        {
            LoanId = reader.GetInt64(0),
            MemberId = reader.GetInt64(1),
            MemberName = reader.GetString(2),
            BookTitle = reader.GetString(3),
            LoanedOn = reader.GetFieldValue<DateOnly>(4),
            ReturnedOn = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
            DaysAllowed = reader.GetInt32(6),
            Status = reader.GetString(7)
        };
    }

    // READ: every loan with its member and book, newest first.
    public async Task<List<LoanView>> GetAllAsync()
    {
        // 1. The SQL: three tables joined, and each loan's status worked out.
        const string sql =
            "SELECT l.loan_id, l.member_id, m.full_name, b.title, l.loaned_on, l.returned_on, l.days_allowed, " +
            "       CASE WHEN l.returned_on IS NOT NULL THEN 'Returned' " +
            "            WHEN l.loaned_on + l.days_allowed < CURRENT_DATE THEN 'Overdue' " +
            "            ELSE 'Out' END " +
            "FROM lending.loan l " +
            "JOIN lending.member m ON l.member_id = m.member_id " +
            "JOIN lending.book b ON l.book_id = b.book_id " +
            "ORDER BY l.loaned_on DESC, l.loan_id DESC;";

        // 2. An empty list, to fill.
        var loans = new List<LoanView>();

        // 3. A command that will run the SQL.
        await using var command = _dataSource.CreateCommand(sql);

        // 4. Run it, and get a reader for the rows.
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync()) // 5. there is another row to read
        {
            // 6. Turn the row into a LoanView and add it to the list.
            loans.Add(ReadLoan(reader));
        }

        // 7. Give the list back.
        return loans;
    }

    // BORROW: lend one book, unless the member already has MaxOpenLoans books out.
    // The check and the INSERT run inside one transaction: both happen, or neither does.
    public async Task<bool> BorrowAsync(long memberId, long bookId, int daysAllowed)
    {
        // 1. Open one connection to the database.
        await using var connection = await _dataSource.OpenConnectionAsync();

        // 2. Start a transaction on that connection.
        await using var transaction = await connection.BeginTransactionAsync();

        // 3. Lock this member's row: a second borrow for the same member waits until this one ends.
        await using (var lockMember = new NpgsqlCommand(
            "SELECT member_id FROM lending.member WHERE member_id = @member_id FOR UPDATE;", connection, transaction))
        {
            lockMember.Parameters.AddWithValue("member_id", memberId);
            await lockMember.ExecuteNonQueryAsync();
        }

        // 4. Count the books this member has out.
        long openLoans;
        await using (var count = new NpgsqlCommand(
            "SELECT COUNT(*) FROM lending.loan WHERE member_id = @member_id AND returned_on IS NULL;",
            connection, transaction))
        {
            count.Parameters.AddWithValue("member_id", memberId);
            openLoans = Convert.ToInt64(await count.ExecuteScalarAsync());
        }

        if (openLoans >= MaxOpenLoans) // 5. the member is already at the limit
        {
            // 6. Undo the transaction: nothing is saved.
            await transaction.RollbackAsync();
            // 7. Report that nothing was borrowed.
            return false;
        }

        // 8. Record the loan. The database fills loaned_on with today's date.
        await using (var insert = new NpgsqlCommand(
            "INSERT INTO lending.loan (member_id, book_id, days_allowed) VALUES (@member_id, @book_id, @days_allowed);",
            connection, transaction))
        {
            insert.Parameters.AddWithValue("member_id", memberId);
            insert.Parameters.AddWithValue("book_id", bookId);
            insert.Parameters.AddWithValue("days_allowed", daysAllowed);
            await insert.ExecuteNonQueryAsync();
        }

        // 9. Make the transaction permanent.
        await transaction.CommitAsync();

        // 10. Report that the book was borrowed.
        return true;
    }

    // RETURN: mark a loan as returned today. A loan already returned is left alone.
    public async Task ReturnAsync(long loanId)
    {
        // 1. The SQL: today's date, set only on a loan not returned yet.
        const string sql = "UPDATE lending.loan SET returned_on = CURRENT_DATE " +
                           "WHERE loan_id = @id AND returned_on IS NULL;";

        // 2. A command that will run the SQL.
        await using var command = _dataSource.CreateCommand(sql);

        // 3. Give the command the loan's id.
        command.Parameters.AddWithValue("id", loanId);

        // 4. Run it.
        await command.ExecuteNonQueryAsync();
    }
}