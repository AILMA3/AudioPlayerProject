using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioPlayerProject.Services
{
    internal class AudioLibraryService
    {
        private readonly string _audioFolder;
        private List<string> _previousTracks = new List<string>();

        public event Action<string> TrackAdded;
        public event Action<string> TrackRemoved;

        public AudioLibraryService()
        {
            _audioFolder = FindAudioFolder();
            EnsureAudioFolderExists();
        }

        public string FindAudioFolder()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;

            return Path.Combine(projectDirectory, "Audio");
        }

        private void EnsureAudioFolderExists()
        {
            if (!Directory.Exists(_audioFolder))
                Directory.CreateDirectory(_audioFolder);
        }

        private List<string> GetAudioFiles()
        {
            var files = Directory.GetFiles(_audioFolder, ".", SearchOption.AllDirectories)
                .Where(file => file.ToLower().EndsWith(".mp3") || file.ToLower().EndsWith(".wav"))
                .ToList();
            return files;
        }

        public async Task ScanAudioFolderAsync()
        {
            var currentTracks = GetAudioFiles();
            var addedTracks = currentTracks.Except(_previousTracks).ToList();
            var removedTracks = _previousTracks.Except(currentTracks).ToList();

            foreach (var added in addedTracks)
            {
                TrackAdded?.Invoke(Path.GetFileName(added));
            }

            foreach (var removed in removedTracks)
            {
                TrackRemoved?.Invoke(Path.GetFileName(removed));
            }

            _previousTracks = currentTracks;
        }

        public List<AudioTrack> GetAudioTracks()
        {
            var tracks = new List<AudioTrack>();
            var files = GetAudioFiles();

            foreach (var file in files)
            {
                try
                {
                    var track = CreateAudioTrack(file);
                    tracks.Add(track);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки трека {file}: {ex.Message}");
                }
            }

            return tracks;
        }

        private AudioTrack CreateAudioTrack(string filePath)
        {
            var tfile = TagLib.File.Create(filePath);

            return new AudioTrack
            {
                Title = tfile.Tag.Title,
                Author = String.Join(", ", tfile.Tag.Performers),
                Duration = tfile.Properties.Duration,
                FileName = Path.GetFileName(filePath)
            };
        }

        public bool CopyTrackToLibrary(string sourcePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var fileName = Path.GetFileName(sourcePath);
                var destinationPath = Path.Combine(_audioFolder, fileName);

                if (File.Exists(destinationPath))
                {
                    errorMessage = $"Файл с именем '{fileName}' уже существует в библиотеке";
                    return false;
                }

                File.Copy(sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Ошибка копирования файла: {ex.Message}";
                return false;
            }
        }

        public string GetTrackPath(string fileName)
        {
            return Path.Combine(_audioFolder, fileName);
        }

        public bool TrackExists(string filePath)
        {
            var path = Path.Combine(_audioFolder, filePath);
            return File.Exists(path);
        }
    }
}
