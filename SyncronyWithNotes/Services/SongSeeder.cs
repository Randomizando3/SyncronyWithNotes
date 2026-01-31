using System.Linq;
using SyncronyWithNotes.Data;
using SyncronyWithNotes.Models;

namespace SyncronyWithNotes.Services;

public static class SongSeeder
{
    public static async Task SeedAsync(AppDatabase db)
    {
        // ======================================================
        // ✅ 1) BRILHA BRILHA ESTRELINHA (com pausas)
        // ======================================================
        await UpsertSongWithNotesAsync(
            db,
            title: "Brilha Brilha Estrelinha",
            approachMs: 1800,
            hitWindowMs: 250,
            durationMs: 95_000,
            spacingMs: 600,
            lanes: new[]
            {
                // C C G G A A G | (pausa)
                0,0,4,4,5,5,4,  -1,

                // F F E E D D C | (pausa)
                3,3,2,2,1,1,0,  -1,

                // G G F F E E D | (pausa)
                4,4,3,3,2,2,1,  -1,

                // G G F F E E D | (pausa)
                4,4,3,3,2,2,1,  -1,

                // C C G G A A G | (pausa)
                0,0,4,4,5,5,4,  -1,

                // F F E E D D C | (segura final)
                3,3,2,2,1,1,0,  -1,-1
            },
            loop2x: false
        );

        // ======================================================
        // ✅ 2) BATE O SINO 2x (loop) (com pausas)
        // ======================================================
        await UpsertSongWithNotesAsync(
            db,
            title: "Bate o Sino",
            approachMs: 1600,
            hitWindowMs: 230,
            durationMs: 160_000,
            spacingMs: 520,
            lanes: new[]
            {
                // --- Frase 1 ---
                // mi mi mi | mi mi mi | mi sol do re mi | (pausa)
                2,2,2,  2,2,2,  2,4,0,1,2,  -1,

                // --- Frase 2 ---
                // fa fa fa fa | mi mi mi mi | (pausa)
                3,3,3,3,  2,2,2,2,  -1,

                // --- Frase 3 ---
                // re re mi re sol | (pausa)
                1,1,2,1,4,  -1,

                // --- Frase 4 ---
                // mi mi mi | (pausa)
                2,2,2,  -1,

                // --- Frase 5 ---
                // mi sol do re mi | (pausa)
                2,4,0,1,2,  -1,

                // --- Frase 6 ---
                // fa fa fa fa | mi mi mi mi | (pausa)
                3,3,3,3,  2,2,2,2,  -1,

                // --- Fecho ---
                // sol sol fa re do | (segura)
                4,4,3,1,0,  -1,-1
            },
            loop2x: true
        );

        // ======================================================
        // ✅ 3) BABY SHARK 2x (loop) (com pausas)
        // ======================================================
        await UpsertSongWithNotesAsync(
            db,
            title: "Baby Shark (Didático)",
            approachMs: 1400,
            hitWindowMs: 220,
            durationMs: 125_000,
            spacingMs: 420,
            lanes: new[]
            {
                // Baby shark doo doo doo doo doo doo (pausa)
                0,1,3,3,3,3,3,3,  -1,

                0,1,3,3,3,3,3,3,  -1,

                0,1,3,3,3,3,3,3,  -1,

                // Baby shark! (segura)
                3,3,2,  -1,-1
            },
            loop2x: true
        );

        // ======================================================
        // ✅ 4) OLD MACDONALD (com pausas)
        // ======================================================
        await UpsertSongWithNotesAsync(
            db,
            title: "Old MacDonald Had a Farm",
            approachMs: 1700,
            hitWindowMs: 230,
            durationMs: 105_000,
            spacingMs: 480,
            lanes: new[]
            {
                // Old MacDonald had a farm, E-I-E-I-O (pausa)
                0,0,0,4,5,5,4,  -1,
                2,2,1,1,0,      -1,

                // And on his farm he had some chicks (pausa)
                4,0,0,0,4,5,5,4,  -1,
                2,2,1,1,0,        -1,

                // With a chick, chick here (pausa)
                4,4,0,0,0,        -1,

                // And a chick, chick there (pausa)
                4,4,0,0,0,        -1,

                // Here a chick, there a chick (pausa)
                0,0,0,0,0,        -1,

                // Everywhere a chick, chick (pausa)
                0,0,0,0,0,        -1,

                // Old MacDonald had a farm, E-I-E-I-O (segura final)
                0,0,0,4,5,5,4,    -1,
                2,2,1,1,0,        -1,-1
            },
            loop2x: false
        );
    }

    private static async Task UpsertSongWithNotesAsync(
        AppDatabase db,
        string title,
        int approachMs,
        int hitWindowMs,
        int durationMs,
        int spacingMs,
        int[] lanes,
        bool loop2x)
    {
        var song = await db.GetSongByTitleAsync(title);

        if (song == null)
        {
            song = new SongEntity
            {
                Title = title,
                ApproachMs = approachMs,
                HitWindowMs = hitWindowMs,
                DurationMs = durationMs
            };

            await db.AddSongAsync(song);
            song = await db.GetSongByTitleAsync(title);
            if (song == null) return;
        }
        else
        {
            song.ApproachMs = approachMs;
            song.HitWindowMs = hitWindowMs;
            song.DurationMs = durationMs;
            await db.UpdateSongAsync(song);
        }

        await db.DeleteNotesForSongAsync(song.Id);

        var finalLanes = loop2x ? lanes.Concat(lanes).ToArray() : lanes;

        await db.AddNotesAsync(Build(song.Id, spacingMs, finalLanes));
    }

    /// <summary>
    /// Build aceita "-1" como pausa:
    /// -1 => não cria nota, apenas avança o tempo em spacingMs.
    /// </summary>
    private static IEnumerable<NoteEntity> Build(int songId, int spacingMs, int[] lanes)
    {
        var t = 0;

        foreach (var lane in lanes)
        {
            if (lane < 0)
            {
                t += spacingMs;
                continue;
            }

            yield return new NoteEntity
            {
                SongId = songId,
                Lane = lane,
                TimeMs = t
            };

            t += spacingMs;
        }
    }
}
