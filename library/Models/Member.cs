namespace library.Models;

// One Member object holds one row of lending.member.
public class Member
{
    public long MemberId { get; set; }          // member_id, the primary key
    public string FullName { get; set; } = "";  // full_name NOT NULL
    public string? Email { get; set; }          // email, may be NULL
    public DateOnly JoinDate { get; set; }      // join_date
    public string MemberType { get; set; } = "Student"; // Student / Faculty / Regular
}