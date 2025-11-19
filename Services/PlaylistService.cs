using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AudioPlayerProject.Services
{
    internal class PlaylistService
    {
        private readonly string _playlistsFolder;
        private readonly AudioLibraryService _audioLibrary;

        public PlaylistService(AudioLibraryService audioLibrary)
        {
            _audioLibrary = audioLibrary;
            _playlistsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Playlists");
            EnsurePlaylistsFolderExists();
        }

        private void EnsurePlaylistsFolderExists()
        {
            if (!Directory.Exists(_playlistsFolder))
                Directory.CreateDirectory(_playlistsFolder);
        }

        public List<Playlist> LoadPlaylists()
        {
            var playlists = new List<Playlist>();

            if (!Directory.Exists(_playlistsFolder))
                return playlists;

            var files = Directory.GetFiles(_playlistsFolder, ".json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var playlist = JsonSerializer.Deserialize<Playlist>(json);
                    if (playlist != null)
                    {
                        CleanupMissingTracks(playlist);
                        playlists.Add(playlist);
                    }
                }
                catch
                {
                    // Пропускаем поврежденные файлы
                }
            }

            return playlists;
        }

        private void CleanupMissingTracks(Playlist playlist)
        {
            var missingTracks = playlist.Tracks
                .Where(fileName => !_audioLibrary.TrackExists(fileName))
                .ToList();

            foreach (var missingTrack in missingTracks)
            {
                playlist.RemoveTrack(missingTrack);
            }
        }

        public void SavePlaylist(Playlist playlist)
        {
            var filePath = Path.Combine(_playlistsFolder, $"{playlist.Name}.json");
            var json = JsonSerializer.Serialize(playlist, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void DeletePlaylist(string playlistName)
        {
            var filePath = Path.Combine(_playlistsFolder, $"{playlistName}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public bool PlayListExists(string playlistName)
        {
            var filePath = Path.Combine(_playlistsFolder, $"{playlistName}.json");
            return File.Exists(filePath);
        }
    }
}
