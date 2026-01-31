using SQLite;

namespace SyncronyWithNotes.Models;

public sealed class NoteEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int SongId { get; set; }

    // 0..6 = C..B
    public int Lane { get; set; }

    // tempo em ms dentro da música
    [Indexed]
    public int TimeMs { get; set; }
}
