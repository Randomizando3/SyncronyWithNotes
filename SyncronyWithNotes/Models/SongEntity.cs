using SQLite;

namespace SyncronyWithNotes.Models;

public sealed class SongEntity
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed, NotNull]
    public string Title { get; set; } = "";

    public int ApproachMs { get; set; } = 1600;
    public int HitWindowMs { get; set; } = 230;
    public int DurationMs { get; set; } = 60_000;
}
