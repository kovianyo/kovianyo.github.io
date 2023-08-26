namespace Generator
{
    public class Song
    {
        public required string Title { get; init; }

        public required string Text { get; init; }

        public string? Url { get; init; }

        public required string FileName { get; init; }
    }
}