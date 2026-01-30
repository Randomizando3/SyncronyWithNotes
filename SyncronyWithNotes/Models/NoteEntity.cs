using SQLite;

namespace SyncronyWithNotes.Models;

public class NoteEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SongId { get; set; }

    public int TimeMs { get; set; }

    // 0..6 => C D E F G A B
    public int Lane { get; set; }
}
