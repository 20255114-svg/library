using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace library.Controllers;

// Answers every address that starts with /Loans.
public class LoansController : Controller
{
    private readonly LoanRepository _loans;
    private readonly MemberRepository _members;
    private readonly BookRepository _books;

    public LoansController(LoanRepository loans, MemberRepository members, BookRepository books)
    {
        _loans = loans;
        _members = members;
        _books = books;
    }

    // GET /Loans : every loan, newest first.
    public async Task<IActionResult> Index()
    {
        var loans = await _loans.GetAllAsync();
        return View(loans);
    }

    // Fills the Borrow form's two dropdowns from the database.
    private async Task FillDropdownsAsync()
    {
        ViewBag.Members = new SelectList(await _members.GetAllAsync(), "MemberId", "FullName");
        ViewBag.Books = new SelectList(await _books.GetAllAsync(), "BookId", "Title");
    }

    // GET /Loans/Borrow : the empty Borrow form.
    public async Task<IActionResult> Borrow()
    {
        await FillDropdownsAsync();
        return View(new BorrowForm());
    }

    // POST /Loans/Borrow : the form was submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(BorrowForm form)
    {
        if (ModelState.IsValid) // 1. every rule on the form was kept
        {
            // 2. Try to borrow the book, and keep the answer.
            bool saved = await _loans.BorrowAsync(form.MemberId!.Value, form.BookId!.Value, form.DaysAllowed);
            if (saved) // 3. the loan was saved
            {
                // 4. Go back to the list of loans.
                return RedirectToAction(nameof(Index));
            }
            // 5. Explain why nothing was saved.
            ModelState.AddModelError("", $"This member already has {LoanRepository.MaxOpenLoans} books out. Nothing was saved.");
        }

        // 6. Fill both dropdowns again.
        await FillDropdownsAsync();
        // 7. Show the form again, with what was chosen.
        return View(form);
    }

    // POST /Loans/Return/5 : the Return button was pressed.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(long id)
    {
        await _loans.ReturnAsync(id);
        return RedirectToAction(nameof(Index));
    }
}