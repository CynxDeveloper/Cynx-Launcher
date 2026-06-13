using System.IO;
using System.Threading;
using NAudio.Wave;

namespace SusanooLauncher.Services
{
    internal static class LauncherSoundService
    {
        private const int MaxPlaybackMs = 500;

        private static readonly object Gate = new();
        private static WaveOutEvent? _output;
        private static MediaFoundationReader? _reader;
        private static Timer? _stopTimer;

        private static string SoundPath =>
            Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", "navigation.mp4");

        internal static void PlayNavigation()
        {
            if (!Settings.Default.soundEffects)
                return;

            if (!File.Exists(SoundPath))
                return;

            lock (Gate)
            {
                StopPlayback();

                try
                {
                    _reader = new MediaFoundationReader(SoundPath);
                    _output = new WaveOutEvent();
                    _output.Init(_reader);
                    _output.PlaybackStopped += OnPlaybackStopped;
                    _output.Play();

                    _stopTimer = new Timer(_ => StopPlayback(), null, MaxPlaybackMs, Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Launcher sound failed: {ex}");
                    StopPlayback();
                }
            }
        }

        private static void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            lock (Gate)
                StopPlayback();
        }

        private static void StopPlayback()
        {
            _stopTimer?.Dispose();
            _stopTimer = null;

            if (_output != null)
            {
                _output.PlaybackStopped -= OnPlaybackStopped;
                _output.Stop();
                _output.Dispose();
                _output = null;
            }

            _reader?.Dispose();
            _reader = null;
        }
    }
}
