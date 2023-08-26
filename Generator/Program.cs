using System.Text;
using System.Xml.Linq;

namespace Generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = "kotta/scores/moldvai/enekek/";
            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = false };
            var filePaths = Directory.GetFiles(path, "*.mscx", enumerationOptions).OrderBy(x => x).ToArray();

            var songs = filePaths.Select(x => GetSong(x)).OrderBy(x => x.Title).ToArray();

            string tableOfContents = GetTableOfContents(songs);

            var song = GetSong(filePaths.First());

            var songFragments = songs.Select(x => GetSongFragment(x)).ToArray();

            string songFragment = string.Join("\n\n", songFragments);

            string html = $"""
            <!doctype html>
            <html>
                <head>
                    <meta charset="utf-8"/>
                    <title>Kovi moldvai dalai</title>
                </head>
                <body>
                    <a id="top">
                    <h1>Kovi moldvai dalai</h1>
                    <div><a href="https://musescore.com/user/443/sets/4665831">[kották]</a></div><br />

            {tableOfContents}
            {songFragment}
                </body>
            </html>
            """;

            Directory.CreateDirectory("html");
            File.WriteAllText("html/index.html", html);
        }

        private static Song GetSong(string filePath)
        {
            string xml = File.ReadAllText(filePath);

            var document = XDocument.Parse(xml);

            var metaTags = document.Descendants("metaTag");

            string title = GetTitle(document);

            var texts = document.Descendants("TBox").SelectMany(x => x.Descendants("text")).Select(x => x.Value.Trim()).ToArray();

            string text = string.Join(string.Empty, texts);

            var sourceMetaTag = metaTags.SingleOrDefault(x => x.Attributes().Any(y => y.Name == "name" && y.Value == "source"));
            string? source = sourceMetaTag?.Value;

            string fileName = Path.GetFileNameWithoutExtension(filePath);

            var song = new Song
            {
                Title = title,
                Text = text,
                Url = source,
                FileName = fileName,
            };

            return song;
        }

        private static string GetTitle(XDocument document)
        {
            var metaTags = document.Descendants("metaTag");

            string? title = GetMetaTagValue(metaTags, "workTitle");

            if (string.IsNullOrEmpty(title))
            {
                title = GetMetaTagValue(metaTags, "movementTitle");
            }

            if (string.IsNullOrEmpty(title))
            {
                var texts = document.Descendants("VBox").SelectMany(x => x.Descendants("Text")).ToArray();

                var titleText = texts.Where(x => x.Descendants("style").Any(y => y.Value == "Title")).FirstOrDefault();

                if (titleText != null)
                {
                    title = titleText.Descendants("text").Select(x => x.Value).FirstOrDefault();
                }
            }

            return title ?? string.Empty;
        }


        private static string? GetMetaTagValue(IEnumerable<XElement> metaTags, string name)
        {
            var metaTag = metaTags.SingleOrDefault(x => x.Attributes().Any(y => y.Name == "name" && y.Value == name));
            string? metaTagValue = metaTag?.Value;

            return metaTagValue;
        }

        private static string GetSongFragment(Song song)
        {
            string html = $"""
                    <a id="{song.FileName}">
                    <h2>{song.Title}</h2>
                    <div><a href="{song.Url}" target="_blank">[kotta]</a> <a href="#top">[top]</a></div>
                    <div style="white-space: pre-wrap">
            {song.Text}</div>
            """;

            return html;
        }

        private static string GetTableOfContents(IEnumerable<Song> songs)
        {
            var stringBuilder = new StringBuilder();

            foreach (var song in songs)
            {
                stringBuilder.AppendLine($"""        <a href="#{song.FileName}">{song.Title}</a><br />""");
            }

            return stringBuilder.ToString();
        }
    }
}
