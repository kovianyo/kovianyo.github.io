using System.Text;
using System.Xml.Linq;

namespace Generator
{
    public class HtmlGenerator
    {
        public static void GenerateHtml(string path)
        {
            var scoreFolder = GetScoreFolder(path);

            string tableOfContents = GetTableOfContents(scoreFolder);

            string songFragment = GetSongTexts(scoreFolder);

            string html = GetHtml(tableOfContents, songFragment);

            WriteHtml(html);
        }

        private static ScoreFolder GetScoreFolder(string path)
        {
            path = path.TrimEnd('/'); // TODO Find a better way

            string? folderName = Path.GetFileName(path) ?? string.Empty;

            string? title = null;

            string titleFilePath = Path.Combine(path, ".title");

            if (File.Exists(titleFilePath))
            {
                title = File.ReadAllText(titleFilePath);
            }

            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = false };
            var filePaths = Directory.GetFiles(path, "*.mscx", enumerationOptions).OrderBy(x => x).ToArray();

            var songs = filePaths.Select(x => GetSong(x)).Where(x => !string.IsNullOrEmpty(x.Text)).ToArray();

            var subdirectories = Directory.GetDirectories(path);

            var scoreFolders = subdirectories
                .Select(GetScoreFolder)
                .Where(x => x.Songs.Any() || x.SubFolders.Any())
                .OrderBy(x => x.FolderName)
                .ToArray() ?? Enumerable.Empty<ScoreFolder>();

            var scoreFolder = new ScoreFolder
            {
                FolderName = folderName,
                Title = title ?? folderName,
                Songs = songs,
                SubFolders = scoreFolders,
            };

            songs.ToList().ForEach(x => x.ScoreFolder = scoreFolder);

            return scoreFolder;
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

        private static string GetTableOfContents(ScoreFolder scoreFolder, int level = 1, string? sectionTitle = null)
        {
            var stringBuilder = new StringBuilder();

            if (sectionTitle != null)
            {
                stringBuilder.AppendLine($"""        <a id="{scoreFolder.FolderName}"></a>""");
                stringBuilder.AppendLine($"""        <h{level}>{sectionTitle}</h{level}>""");
            }

            if (scoreFolder.Songs.Any())
            {
                stringBuilder.AppendLine("""        <div style="padding-bottom: 1em">""");

                foreach (var song in scoreFolder.Songs)
                {
                    stringBuilder.AppendLine($"""          <a href="#{song.FileName}">{song.Title}</a><br />""");
                }

                stringBuilder.AppendLine("        </div>");
            }

            var subSegments = scoreFolder.SubFolders.Select(x => GetTableOfContents(x, level + 1, x.Title)).ToList();

            subSegments.ForEach(x => stringBuilder.AppendLine(x));

            string html = stringBuilder.ToString();

            return html;
        }

        private static string GetSongTexts(ScoreFolder scoreFolder, int level = 1, string? sectionTitle = null)
        {
            var stringBuilder = new StringBuilder();

            if (sectionTitle != null)
            {
                stringBuilder.AppendLine($"<h{level}>{sectionTitle}</h{level}>");
            }

            var songs = scoreFolder.Songs.OrderBy(x => x.Title).GroupBy(x => $"{x.Title}_{x.Text}").ToArray();

            var songFragments = songs.Select(x => GetSongFragment(x, level + 1)).ToList();

            songFragments.ForEach(x => stringBuilder.AppendLine(x));

            var subSegments = scoreFolder.SubFolders.Select(x => GetSongTexts(x, level + 1, x.Title)).ToList();

            subSegments.ForEach(x => stringBuilder.AppendLine(x));

            string html = stringBuilder.ToString();

            return html;
        }

        private static string GetSongFragment(IGrouping<string, Song> songs, int level = 2)
        {
            var song = songs.First();

            var urls = songs.Select(x => x.Url).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).ToArray();

            var links = urls.Select(x => $"""<a href="{x}" target="_blank">[kotta]</a> """);

            string scoreLinkText = string.Join(" ", links);

            string? categoryLink = song.ScoreFolder != null ? $"""<a href="#{song.ScoreFolder.FolderName}">[kategória]</a>""" : null;

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"""        <a id="{song.FileName}"></a>""");
            stringBuilder.AppendLine($"""        <h{level}>{song.Title}</h{level}>""");
            stringBuilder.AppendLine($"""        <div>""");
            stringBuilder.AppendLine($"""          {scoreLinkText}""");

            if (!string.IsNullOrEmpty(categoryLink))
            {
                stringBuilder.AppendLine($"""          {categoryLink}""");
            }

            stringBuilder.AppendLine($"""        </div>""");

            stringBuilder.AppendLine($"""        <div class="text">""");
            stringBuilder.AppendLine($"""{song.Text}</div>""");

            string html = stringBuilder.ToString();

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
