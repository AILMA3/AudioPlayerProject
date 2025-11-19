using AudioPlayerProject.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AudioPlayerProject
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AudioLibraryService _audioLibrary;
        private readonly AudioPlayerService _audioPlayer;
        private string _statusMessage = "Загрузка...";
        private string _playPauseText = "▶";
        private string _currentTime = "00:00";
        private AudioTrack _currentTrack;
        private Visibility _volumeVisibility = Visibility.Collapsed;
        private double _volume = 0.5;

        public ObservableCollection<AudioTrack> AudioTracks { get; } = new ObservableCollection<AudioTrack>();

        public AudioTrack CurrentTrack
        {
            get => _currentTrack;
            set
            {
                if (_currentTrack == value) return;

                _currentTrack = value;
                OnPropertyChanged(nameof(CurrentTrack));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string PlayPauseText
        {
            get => _playPauseText;
            set
            {
                _playPauseText = value;
                OnPropertyChanged(nameof(PlayPauseText));
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged(nameof(CurrentTime));
            }
        }

        public Visibility VolumeVisibility
        {
            get => _volumeVisibility;
            set 
            {
                _volumeVisibility = value;
                OnPropertyChanged(nameof(VolumeVisibility));
            }
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged(nameof(Volume));
                    OnPropertyChanged(nameof(VolumePercent));

                    _audioPlayer.SetVolume(_volume);
                }
            }
        }

        public string VolumePercent => $"{(_volume * 100):F0}%";

        public ICommand AddTrackCommand { get; }
        public ICommand DeleteTrackCommand { get; }
        public ICommand PlayPauseCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand ToggleVolumeCommand { get; }

        public MainViewModel()
        {
            _audioLibrary = new AudioLibraryService();
            _audioPlayer = new AudioPlayerService();

            _audioPlayer.TrackChanged += OnTrackChanged;
            _audioPlayer.PlaybackStarted += OnPlaybackStarted;
            _audioPlayer.PlaybackPaused += OnPlaybackPaused;
            _audioPlayer.PositionChanged += OnPositionChanged;

            AddTrackCommand = new RelayCommand(async (param) => await AddTrackAsync());
            DeleteTrackCommand = new RelayCommand(async (param) => await DeleteTrackAsync(param as AudioTrack));
            PlayPauseCommand = new RelayCommand((param) => TogglePlayPause());
            NextCommand = new RelayCommand((param) => PlayNext());
            PreviousCommand = new RelayCommand((param) => PlayPrevious());
            ToggleVolumeCommand = new RelayCommand((param) => ToggleVolume());

            _ = LoadAudioTracksAsync();
        }

        private void ToggleVolume()
        {
            VolumeVisibility = VolumeVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async Task LoadAudioTracksAsync()
        {
            StatusMessage = "Сканирую папку Audio";

            await _audioLibrary.ScanAudioFolderAsync();
            var tracks = _audioLibrary.GetAudioTracks();

            AudioTracks.Clear();
            foreach (var track in tracks)
            {
                AudioTracks.Add(track);
            }

            _audioPlayer.SetPlaylist(tracks.ToList());

            StatusMessage = $"Загружено треков: {AudioTracks.Count}";
        }

        private async Task AddTrackAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    if (_audioLibrary.CopyTrackToLibrary(filePath, out string errorMessage))
                    {
                        var tfile = TagLib.File.Create(filePath);

                        var newTrack = new AudioTrack
                        {
                            Title = tfile.Tag.Title,
                            Author = String.Join(", ", tfile.Tag.Performers),
                            Duration = tfile.Properties.Duration,
                            FileName = Path.GetFileName(filePath)
                        };

                        AudioTracks.Add(newTrack);
                        StatusMessage = $"Добавлен трек: {tfile.Tag.Title}";
                    }
                    else
                    {
                        StatusMessage = $"Ошибка: {errorMessage}";
                    }
                }

                _audioPlayer.SetPlaylist(AudioTracks.ToList());

                StatusMessage = $"Добавление завершено. Всего треков: {AudioTracks.Count}";
            }
        }

        private async Task DeleteTrackAsync(AudioTrack track)
        {
            if (track == null) return;

            var result = MessageBox.Show(
                $"Удалить трек '{track.Title}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var filePath = _audioLibrary.GetTrackPath(track.FileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        AudioTracks.Remove(track);

                        _audioPlayer.SetPlaylist(AudioTracks.ToList());

                        StatusMessage = $"Удален трек: {track.Title}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Ошибка удаления: {ex.Message}";
                }
            }
        }

        private void TogglePlayPause()
        {
            if (_audioPlayer.IsPlaying)
            {
                _audioPlayer.Pause();
                PlayPauseText = "▶";
            }
            else
            {
                if (CurrentTrack == null && AudioTracks.Count > 0)
                {
                    var firstTrack = AudioTracks.First();
                    _audioPlayer.PlayTrack(firstTrack);
                }
                else
                {
                    _audioPlayer.Play();
                }
                PlayPauseText = "⏸";
            }
        }

        private void PlayNext()
        {
            _audioPlayer.PlayNext();
        }

        private void PlayPrevious()
        {
            _audioPlayer.PlayPrevious();
        }

        private void OnTrackChanged(AudioTrack track)
        {
            CurrentTrack = track;
        }

        private void OnPlaybackStarted()
        {
            PlayPauseText = "⏸";
        }

        private void OnPlaybackPaused()
        {
            PlayPauseText = "▶";
        }

        private void OnPositionChanged(TimeSpan position)
        {
            // Обновляем отображение времени
            CurrentTime = $"{position:mm\\:ss} / {_audioPlayer.GetTotalDuration():mm\\:ss}";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
