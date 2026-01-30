using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SyncronyWithNotes.Services;

namespace SyncronyWithNotes;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ✅ Registra como IAudioManager explicitamente (mais compatível)
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);

        // ✅ Serviço de áudio
        builder.Services.AddSingleton<NoteAudioService>();

        var app = builder.Build();

        AppServices.Provider = app.Services;

        return app;
    }
}
