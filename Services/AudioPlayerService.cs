using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace AudioPlayerProject.Services
{
    internal class AudioPlayerService
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _positionTimer;
        private List<AudioTrack> _playlist;
        private int _currentTrackIndex = -1;
        private bool _isPlaying = false;

        public AudioTrack CurrentTrack => _playlist != null && _currentTrackIndex >= 0 && _currentTrackIndex < _playlist.Count
            ? _playlist[_currentTrackIndex]
            : null;

        public event Action<AudioTrack> TrackChanged;
        public event Action PlaybackStarted;
        public event Action PlaybackPaused;
        public event Action PlaybackStopped;
        public event Action<TimeSpan> PositionChanged;

        public AudioPlayerService()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaOpened += OnMediaOpened;
            _mediaPlayer.MediaEnded += OnMediaEnded;

            _positionTimer = new DispatcherTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _positionTimer.Tick += OnPositionTimerTick;
        }

        public void SetPlaylist(List<AudioTrack> playlist)
        {
            _playlist = playlist;
            _currentTrackIndex = -1;
        }

        public void SetVolume(double volume)
        {
            _mediaPlayer.Volume = volume;
        }

        public void Play()
        {
            if (CurrentTrack != null)
            {
                _mediaPlayer.Play();
                _isPlaying = true;
                _positionTimer.Start();
                PlaybackStarted?.Invoke();
            }
        }

        public void Pause()
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
            _positionTimer.Stop();
            PlaybackPaused?.Invoke();
        }

        public void Stop()
        {
            _mediaPlayer.Stop();
            _isPlaying = false;
            _positionTimer.Stop();
            PlaybackStopped?.Invoke();
        }

        public void PlayTrack(AudioTrack track)
        {
            if (track == null || _playlist == null) return;

            var trackIndex = _playlist.IndexOf(track);
            if (trackIndex >= 0)
            {
                PlayTrackByIndex(trackIndex);
            }
        }

        public void PlayNext()
        {
            if (_playlist == null || _playlist.Count == 0) return;

            var nextIndex = (_currentTrackIndex + 1) % _playlist.Count;
            PlayTrackByIndex(nextIndex);
        }

        public void PlayPrevious()
        {
            if (_playlist == null || _playlist.Count == 0) return;

            // Если текущая песня играет больше 5 секунд - начинаем сначала
            if (_mediaPlayer.Position.TotalSeconds > 5)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                Play();
            }
            else
            {
                // Иначе переключаем на предыдущую песню
                _currentTrackIndex = (_currentTrackIndex - 1 + _playlist.Count) % _playlist.Count;
                PlayTrackByIndex(_currentTrackIndex);
            }
        }

        private void PlayTrackByIndex(int index)
        {
            if (_playlist == null || index < 0 || index >= _playlist.Count) return;

            var track = _playlist[index];
            var audioService = new AudioLibraryService();
            var filePath = audioService.GetTrackPath(track.FileName);

            _mediaPlayer.Close();
            _mediaPlayer.Open(new Uri(filePath));
            _currentTrackIndex = index;
            _isPlaying = true;
            _mediaPlayer.Play();
            _positionTimer.Start();

            TrackChanged?.Invoke(track);
            PlaybackStarted?.Invoke();
        }

        private void OnMediaOpened(object sender, EventArgs e)
        {
            // Можно добавить логику при открытии медиа
            PositionChanged?.Invoke(TimeSpan.Zero);
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            _isPlaying = false;
            _positionTimer.Stop();
            // Автоматическое переключение на следующий трек
            PlayNext();
        }

        private void OnPositionTimerTick(object sender, EventArgs e)
        {
            PositionChanged?.Invoke(_mediaPlayer.Position);
        }

        public TimeSpan GetCurrentPosition()
        {
            return _mediaPlayer.Position;
        }

        public TimeSpan GetTotalDuration()
        {
            return _mediaPlayer.NaturalDuration.HasTimeSpan ? _mediaPlayer.NaturalDuration.TimeSpan : TimeSpan.Zero;
        }

        public bool IsPlaying => _isPlaying;
    }
}
