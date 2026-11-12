using Npgsql;
using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;

namespace library.Controllers;

public class MembersController : Controller
{
    private readonly MemberRepository _members;
    private readonly LoanRepository _loans;

    // ASP.NET Core hands in the repositories, because Program.cs registered them.
    public MembersController(MemberRepository members, LoanRepository loans)
    {
        _members = members;
        _loans = loans;
    }

    // GET /Members : the list of members.
    public async Task<IActionResult> Index()
    {
        return View(await _members.GetAllAsync());
    }

    // GET /Members/Create : an empty form.
    public IActionResult Create()
    {
        return View(new Member { JoinDate = DateOnly.FromDateTime(DateTime.Today) });
    }

    // POST /Members/Create : the form was submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Member member)
    {
        if (!ModelState.IsValid) // 1. a rule on the form was broken
        {
            // 2. Show the form again, with what was typed.
            return View(member);
        }

        try
        {
            // 3. Save the new member.
            await _members.AddAsync(member);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) // 4. another member already uses this email
        {
            // 5. Put a message under the Email box.
            ModelState.AddModelError("Email", "Another member already uses this email.");
            // 6. Show the form again, with what was typed.
            return View(member);
        }

        // 7. The member was saved: go back to the list.
        return RedirectToAction(nameof(Index));
    }

    // GET /Members/Edit/5 : the form filled with the current values.
    public async Task<IActionResult> Edit(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null)
        {
            return NotFound();
        }
        return View(member);
    }

    // POST /Members/Edit/5 : the changed form was submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Member member)
    {
        if (!ModelState.IsValid) // 1. a rule on the form was broken
        {
            // 2. Show the form again, with what was typed.
            return View(member);
        }

        try
        {
            // 3. Save the changes.
            await _members.UpdateAsync(member);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation) // 4. another member already uses this email
        {
            // 5. Put a message under the Email box.
            ModelState.AddModelError("Email", "Another member already uses this email.");
            // 6. Show the form again, with what was typed.
            return View(member);
        }

        // 7. The changes were saved: go back to the list.
        return RedirectToAction(nameof(Index));
    }

    // GET /Members/Delete/5 : ask "are you sure?"
    public async Task<IActionResult> Delete(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null)
        {
            return NotFound();
        }
        return View(member);
    }

    // POST /Members/Delete/5 : the delete was confirmed.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            // 1. Delete the member.
            await _members.DeleteAsync(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation) // 2. the database refused because of a foreign key
        {
            // 3. Load the member again, to show them on the page.
            var member = await _members.GetByIdAsync(id);
            if (member == null) // 4. there is no such member
            {
                // 5. Answer "not found".
                return NotFound();
            }
            // 6. Set the message the page will show.
            ViewBag.Error = "This member has loans on record, so the database will not delete them.";
            // 7. Show the Delete page again, with the member.
            return View("Delete", member);
        }

        // 8. The member was deleted: go back to the list.
        return RedirectToAction(nameof(Index));
    }

    // GET /Members/Details/5 : one member and their loans.
    public async Task<IActionResult> Details(long id)
    {
        // 1. Load the member.
        var member = await _members.GetByIdAsync(id);

        if (member == null) // 2. there is no such member
        {
            // 3. Answer "not found".
            return NotFound();
        }

        // 4. Put the member and their loans together for the page.
        var details = new MemberDetails
        {
            Member = member,
            Loans = await _loans.GetByMemberAsync(id)
        };

        // 5. Show the page.
        return View(details);
    }
}