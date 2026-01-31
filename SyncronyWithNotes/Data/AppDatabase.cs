using SQLite;
using SyncronyWithNotes.Models;

namespace SyncronyWithNotes.Data;

public sealed class AppDatabase
{
    private readonly SQLiteAsyncConnection _db;

    public AppDatabase(string path)
    {
        _db = new SQLiteAsyncConnection(path);
    }

    public async Task InitAsync()
    {
        await _db.CreateTableAsync<SongEntity>();
        await _db.CreateTableAsync<NoteEntity>();
    }

    public Task<int> SongsCountAsync()
        => _db.Table<SongEntity>().CountAsync();

    public Task<int> AddSongAsync(SongEntity song)
        => _db.InsertAsync(song);

    public Task<int> UpdateSongAsync(SongEntity song)
        => _db.UpdateAsync(song);

    public Task AddNotesAsync(IEnumerable<NoteEntity> notes)
        => _db.InsertAllAsync(notes);

    public Task<List<SongEntity>> GetSongsAsync()
        => _db.Table<SongEntity>().OrderBy(s => s.Title).ToListAsync();

    public Task<SongEntity?> GetSongByIdAsync(int songId)
        => _db.Table<SongEntity>().FirstOrDefaultAsync(s => s.Id == songId);

    public Task<List<NoteEntity>> GetNotesAsync(int songId)
        => _db.Table<NoteEntity>()
              .Where(n => n.SongId == songId)
              .OrderBy(n => n.TimeMs)
              .ToListAsync();

    public Task<SongEntity?> GetSongByTitleAsync(string title)
        => _db.Table<SongEntity>().FirstOrDefaultAsync(s => s.Title == title);

    public Task<int> NotesCountAsync(int songId)
        => _db.Table<NoteEntity>().Where(n => n.SongId == songId).CountAsync();

    // ✅ NOVO: apagar notas de uma música (para re-seed / atualizar)
    public Task<int> DeleteNotesForSongAsync(int songId)
        => _db.ExecuteAsync("DELETE FROM NoteEntity WHERE SongId = ?", songId);
}
