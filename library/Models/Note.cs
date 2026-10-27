using System.ComponentModel.DataAnnotations;

namespace library.Models;

// One Note object holds one row of lending.note.
public class Note
{
    public long NoteId { get; set; }            // note_id, the primary key

    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(100, ErrorMessage = "The title can be at most 100 characters.")]
    public string Title { get; set; } = "";     // title NOT NULL, VARCHAR(100)

    public string? Body { get; set; }           // body, may be NULL
    public DateOnly CreatedOn { get; set; }     // created_on
}