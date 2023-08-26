using System.Text;
using System.Xml.Linq;

namespace Generator
{
    public class HtmlGenerator
    {
        public static void GenerateHtml(string path)
        {
            var filePaths = GetFilePaths(path);

            var songs = filePaths.Select(x => GetSong(x)).OrderBy(x => x.Title).ToArray();

            string tableOfContents = GetTableOfContents(songs);

            var songFragments = songs.Select(x => GetSongFragment(x)).ToArray();

            string songFragment = string.Join("\n\n", songFragments);

            string html = GetHtml(tableOfContents, songFragment);

            WriteHtml(html);
        }

        private static IEnumerable<string> GetFilePaths(string path)
        {
            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = false };
            var filePaths = Directory.GetFiles(path, "*.mscx", enumerationOptions).OrderBy(x => x).ToArray();

            return filePaths;
        }

        private static Song GetSong(string filePath)
        {
            string xml = File.ReadAllText(filePath);

            var document = XDocument.Parse(xml);

            var metaTags = document.Descendants("metaTag");

            string title = GetTitle(document);

            string text = GetText(document);

            string? source = GetMetaTagValue(metaTags, "source");

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

        private static string GetText(XDocument document)
        {
            var texts = document.Descendants("Text")
                .Where(x => x.Descendants("style").Any(y => y.Value == "Frame" || y.Value == "frame"))
                .SelectMany(x => x.Descendants("text"))
                .Select(x => x.Value.Trim())
                .ToArray();

            string text = string.Join("\n\n", texts);

            return text;
        }

        private static string GetTableOfContents(IEnumerable<Song> songs)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("""        <div style="padding-bottom: 1em">""");

            foreach (var song in songs)
            {
                stringBuilder.AppendLine($"""          <a href="#{song.FileName}">{song.Title}</a><br />""");
            }

            stringBuilder.AppendLine("        </div>");

            string html = stringBuilder.ToString();

            return html;
        }

        private static string GetSongFragment(Song song)
        {
            string html = $"""
                    <a id="{song.FileName}"></a>
                    <h2>{song.Title}</h2>
                    <div><a href="{song.Url}" target="_blank">[kotta]</a> <a href="#top">[top]</a></div>
                    <div class="text">
            {song.Text}</div>
            """;

            return html;
        }

        private static string GetHtml(string tableOfContents, string songFragment)
        {
            string html = $$"""
            <!doctype html>
            <html>
                <head>
                    <meta charset="utf-8"/>
                    <title>Kovi moldvai dalai</title>
                    <style>
                    body 
                    { 
                        color: rgb(230, 237, 243);
                        background-color: rgb(22, 27, 34);
                    }
                    a 
                    {
                        color: hsl(215, 93%, 78%);
                    }
                    div.toc 
                    {
                        padding-bottom: 0.3em;
                    }
                    div.text
                    {
                        white-space: pre-wrap; 
                        padding-bottom: 2em;
                    }
                    </style>
                </head>
                <body style="padding: 1em">
                    <a id="top"></a>
                    <h1>Kovi moldvai dalai</h1>
                    <div><a href="https://musescore.com/user/443/sets/4665831">[kották]</a></div><br />

            {{tableOfContents}}
            {{songFragment}}
                </body>
            </html>
            """;

            return html;
        }

        private static void WriteHtml(string html)
        {
            Directory.CreateDirectory("html");
            File.WriteAllText("html/index.html", html);
        }
    }
}
