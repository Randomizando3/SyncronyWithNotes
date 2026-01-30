using Plugin.Maui.Audio;

namespace SyncronyWithNotes.Services;

public sealed class NoteAudioService
{
    private readonly IAudioManager _audio;

    // cache de bytes por arquivo (c.mp3 etc.)
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public NoteAudioService(IAudioManager audio)
    {
        _audio = audio;
    }

    // Chame uma vez no começo (Init) para garantir que os assets estão ok
    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        // pré-carrega tudo
        await PreloadAsync("c.mp3");
        await PreloadAsync("d.mp3");
        await PreloadAsync("e.mp3");
        await PreloadAsync("f.mp3");
        await PreloadAsync("g.mp3");
        await PreloadAsync("a.mp3");
        await PreloadAsync("b.mp3");

        _loaded = true;
    }

    public async Task PlayLaneAsync(int lane)
    {
        var file = lane switch
        {
            0 => "c.mp3",
            1 => "d.mp3",
            2 => "e.mp3",
            3 => "f.mp3",
            4 => "g.mp3",
            5 => "a.mp3",
            6 => "b.mp3",
            _ => "c.mp3"
        };

        await EnsureLoadedAsync();

        if (!_cache.TryGetValue(file, out var bytes) || bytes.Length == 0)
            return;

        // cria stream novo sempre (evita stream fechado / posição incorreta)
        var ms = new MemoryStream(bytes);

        var player = _audio.CreatePlayer(ms);
        player.Play();

        // descarta depois de tocar um pouco
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            player.Dispose();
            ms.Dispose();
        });
    }

    private async Task PreloadAsync(string filename)
    {
        if (_cache.ContainsKey(filename))
            return;

        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(filename);
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem);

            _cache[filename] = mem.ToArray();
        }
        catch
        {
            // se não achou o arquivo, guarda vazio e não crasha
            _cache[filename] = Array.Empty<byte>();
        }
    }
}
