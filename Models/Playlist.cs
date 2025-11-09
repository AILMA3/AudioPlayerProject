using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Playlist
{
    public string Name { get; set; }
    public ObservableCollection<AudioTrack> Tracks { get; set; }
}
