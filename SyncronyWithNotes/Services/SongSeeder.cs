using SyncronyWithNotes.Data;
using SyncronyWithNotes.Models;

namespace SyncronyWithNotes.Services;

public static class SongSeeder
{
    public static async Task SeedAsync(AppDatabase db)
    {
        // Seed só uma vez
        if (await db.SongsCountAsync() > 0)
            return;

        var song = new SongEntity
        {
            Title = "Brilha Brilha Estrelinha",
            DurationMs = 90_000,   // 1:30
            ApproachMs = 3000,     // queda lenta
            HitWindowMs = 300      // tolerância grande
        };

        await db.AddSongAsync(song); // song.Id é preenchido aqui

        int t = 2000;
        int step = 1200;

        var notes = new List<NoteEntity>();

        // C C G G A A G
        notes.AddRange(Line(song.Id, t, step, 0, 0, 4, 4, 5, 5, 4)); t += step * 8;

        // F F E E D D C
        notes.AddRange(Line(song.Id, t, step, 3, 3, 2, 2, 1, 1, 0)); t += step * 8;

        // G G F F E E D
        notes.AddRange(Line(song.Id, t, step, 4, 4, 3, 3, 2, 2, 1)); t += step * 8;

        // G G F F E E D
        notes.AddRange(Line(song.Id, t, step, 4, 4, 3, 3, 2, 2, 1)); t += step * 8;

        // C C G G A A G
        notes.AddRange(Line(song.Id, t, step, 0, 0, 4, 4, 5, 5, 4)); t += step * 8;

        // F F E E D D C
        notes.AddRange(Line(song.Id, t, step, 3, 3, 2, 2, 1, 1, 0));

        await db.AddNotesAsync(notes);
    }

    private static IEnumerable<NoteEntity> Line(int songId, int start, int step, params int[] lanes)
    {
        int time = start;
        foreach (var lane in lanes)
        {
            yield return new NoteEntity
            {
                SongId = songId,
                TimeMs = time,
                Lane = lane
            };
            time += step;
        }
    }
}
