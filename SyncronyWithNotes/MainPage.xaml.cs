using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Layouts;
using SyncronyWithNotes.Data;
using SyncronyWithNotes.Models;
using SyncronyWithNotes.Services;

namespace SyncronyWithNotes;

public partial class MainPage : ContentPage
{
    private AppDatabase _db = null!;
    private NoteAudioService _audio = null!;

    private List<SongEntity> _songs = new();
    private SongEntity? _song;
    private List<NoteEntity> _notes = new();

    private readonly List<ActiveNote> _active = new();
    private int _index;

    private DateTime _startUtc;
    private IDispatcherTimer? _timer;

    private bool _isPlaying;

    // Velocidades permitidas
    private readonly double[] _speeds = { 1.0, 1.25, 1.5, 2.0 };
    private double _speed = 1.0;

    // Y final: base da área NotesLayer
    private double _hitY;

    private readonly Color[] _laneColors =
    {
        Color.FromArgb("#FF3B30"), // C
        Color.FromArgb("#FF9500"), // D
        Color.FromArgb("#FFCC00"), // E
        Color.FromArgb("#34C759"), // F
        Color.FromArgb("#00C7BE"), // G
        Color.FromArgb("#007AFF"), // A
        Color.FromArgb("#AF52DE"), // B
    };

    public MainPage()
    {
        InitializeComponent();

        Loaded += async (_, __) => await InitAsync();

        SizeChanged += (_, __) =>
        {
            _hitY = Math.Max(0, NotesLayer.Height - 6);
        };
    }

    // ✅ tempo do jogo escalado pela velocidade
    private int NowMs
    {
        get
        {
            var raw = (DateTime.UtcNow - _startUtc).TotalMilliseconds;
            var scaled = raw * _speed;
            return (int)scaled;
        }
    }

    private async Task InitAsync()
    {
        _audio = AppServices.Provider.GetRequiredService<NoteAudioService>();
        await _audio.EnsureLoadedAsync();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "sync.db");

        _db = new AppDatabase(dbPath);
        await _db.InitAsync();

        await SongSeeder.SeedAsync(_db);

        _songs = await _db.GetSongsAsync();
        SongPicker.ItemsSource = _songs;
        SongPicker.ItemDisplayBinding = new Binding(nameof(SongEntity.Title));

        // Speed picker
        SpeedPicker.ItemsSource = _speeds.Select(s => $"{s:0.##}x").ToList();
        SpeedPicker.SelectedIndex = 0; // 1.0x

        if (_songs.Count > 0)
        {
            SongPicker.SelectedIndex = 0;
            await LoadSongAsync(_songs[0].Id);
        }

        BuildLaneSeparators();
        UpdateUiState(isPlaying: false);
    }

    private async Task LoadSongAsync(int songId)
    {
        _song = await _db.GetSongByIdAsync(songId);
        if (_song is null)
            return;

        _notes = await _db.GetNotesAsync(_song.Id);

        StopInternal(resetVisual: true);
        UpdateUiState(isPlaying: false);
    }

    private void UpdateUiState(bool isPlaying)
    {
        _isPlaying = isPlaying;

        PlayButton.IsEnabled = !isPlaying && _song != null;
        StopButton.IsEnabled = isPlaying;

        SongPicker.IsEnabled = !isPlaying;
        SpeedPicker.IsEnabled = !isPlaying; // ✅ velocidade só altera antes de dar Play
    }

    private void Start()
    {
        if (_song is null) return;

        _index = 0;
        _active.Clear();
        NotesLayer.Children.Clear();

        _startUtc = DateTime.UtcNow;

        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, __) => Tick();
        _timer.Start();

        UpdateUiState(isPlaying: true);
    }

    private void StopInternal(bool resetVisual)
    {
        _timer?.Stop();
        _timer = null;

        if (resetVisual)
        {
            _index = 0;
            _active.Clear();
            NotesLayer.Children.Clear();
        }
    }

    private void Stop()
    {
        StopInternal(resetVisual: true);
        UpdateUiState(isPlaying: false);
    }

    private void Tick()
    {
        if (!_isPlaying || _song is null) return;

        if (NotesLayer.Width <= 0 || NotesLayer.Height <= 0)
            return;

        _hitY = Math.Max(0, NotesLayer.Height - 6);

        var now = NowMs;

        // spawn
        while (_index < _notes.Count && _notes[_index].TimeMs - _song.ApproachMs <= now)
        {
            Spawn(_notes[_index]);
            _index++;
        }

        // move
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var a = _active[i];

            double progress = (now - (a.Note.TimeMs - _song.ApproachMs)) / (double)_song.ApproachMs;
            double y = progress * _hitY;
            a.View.TranslationY = y;

            // toca automático quando chega no final
            if (!a.Played && progress >= 1.0)
            {
                a.Played = true;
                _ = _audio.PlayLaneAsync(a.Note.Lane);
            }

            if (now > a.Note.TimeMs + _song.HitWindowMs)
            {
                NotesLayer.Children.Remove(a.View);
                _active.RemoveAt(i);
            }
        }

        // encerra ao final (escala já está no now)
        if (now > _song.DurationMs + 1000)
            Stop();
    }

    private void Spawn(NoteEntity note)
    {
        double laneW = NotesLayer.Width / 7.0;
        double w = Math.Max(10, laneW - 14);
        double h = 22;

        var box = new BoxView
        {
            Color = _laneColors[note.Lane],
            HeightRequest = h,
            WidthRequest = w,
            CornerRadius = 10,
            Opacity = 0.95
        };

        double x = note.Lane * laneW + (laneW - w) / 2.0;

        AbsoluteLayout.SetLayoutBounds(box, new Rect(x, 0, w, h));
        AbsoluteLayout.SetLayoutFlags(box, AbsoluteLayoutFlags.None);

        NotesLayer.Children.Add(box);
        _active.Add(new ActiveNote(note, box));
    }

    private void BuildLaneSeparators()
    {
        LanesLayer.Children.Clear();

        for (int i = 0; i < 7; i++)
        {
            var cell = new Grid { BackgroundColor = Colors.Transparent };
            Grid.SetColumn(cell, i);

            if (i < 6)
            {
                var sep = new BoxView
                {
                    Color = Colors.White.WithAlpha(0.10f),
                    WidthRequest = 2,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Fill
                };
                cell.Children.Add(sep);
            }

            LanesLayer.Children.Add(cell);
        }

        var hitLine = new BoxView
        {
            Color = Colors.White.WithAlpha(0.25f),
            HeightRequest = 2,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 2)
        };

        Grid.SetRow(hitLine, 0);
        Grid.SetColumnSpan(hitLine, 7);
        LanesLayer.Children.Add(hitLine);
    }

    private void Hit(int lane)
    {
        if (!_isPlaying || _song is null) return;

        var now = NowMs;

        var best = _active
            .Where(n => n.Note.Lane == lane)
            .OrderBy(n => Math.Abs(n.Note.TimeMs - now))
            .FirstOrDefault();

        // som ao apertar
        _ = _audio.PlayLaneAsync(lane);

        if (best is null)
            return;

        if (Math.Abs(best.Note.TimeMs - now) <= _song.HitWindowMs)
        {
            NotesLayer.Children.Remove(best.View);
            _active.Remove(best);
        }
    }

    // ===== Eventos UI =====

    async void SongPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isPlaying) return;
        if (SongPicker.SelectedIndex < 0 || SongPicker.SelectedIndex >= _songs.Count) return;
        await LoadSongAsync(_songs[SongPicker.SelectedIndex].Id);
    }

    void SpeedPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isPlaying) return;
        if (SpeedPicker.SelectedIndex < 0 || SpeedPicker.SelectedIndex >= _speeds.Length) return;

        _speed = _speeds[SpeedPicker.SelectedIndex];
    }

    void PlayButton_Clicked(object sender, EventArgs e) => Start();
    void StopButton_Clicked(object sender, EventArgs e) => Stop();

    // Pads
    void Pad0(object s, EventArgs e) => Hit(0);
    void Pad1(object s, EventArgs e) => Hit(1);
    void Pad2(object s, EventArgs e) => Hit(2);
    void Pad3(object s, EventArgs e) => Hit(3);
    void Pad4(object s, EventArgs e) => Hit(4);
    void Pad5(object s, EventArgs e) => Hit(5);
    void Pad6(object s, EventArgs e) => Hit(6);

    private sealed class ActiveNote
    {
        public NoteEntity Note { get; }
        public BoxView View { get; }
        public bool Played { get; set; }

        public ActiveNote(NoteEntity note, BoxView view)
        {
            Note = note;
            View = view;
        }
    }
}
