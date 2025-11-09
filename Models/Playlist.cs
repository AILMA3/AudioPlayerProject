using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Playlist
{
    public string Name { get; set; }
    public ObservableCollection<string> Tracks { get; set; } = new ObservableCollection<string>();

    public void AddTrack(string fileName)
    {
        if (!Tracks.Contains(fileName))
            Tracks.Add(fileName);
    }

    public void RemoveTrack(string fileName)
    {
        Tracks.Remove(fileName);
    }
}
