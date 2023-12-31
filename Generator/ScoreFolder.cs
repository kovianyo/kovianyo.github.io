using System.Diagnostics;

namespace Generator
{
    [DebuggerDisplay("Title: {Title}, FolderName: {FolderName}")]
    public class ScoreFolder
    {
        public required string FolderName { get; init; }

        public required string Title { get; init; }

        public required IEnumerable<IGrouping<string, Song>> SongGroups { get; init; }

        public required IEnumerable<ScoreFolder> SubFolders { get; init; }
    }
}