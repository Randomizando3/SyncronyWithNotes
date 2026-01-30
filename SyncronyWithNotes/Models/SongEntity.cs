using SQLite;

namespace SyncronyWithNotes.Models;

public class SongEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = "";
    public int DurationMs { get; set; }
    public int ApproachMs { get; set; }
    public int HitWindowMs { get; set; }
}
