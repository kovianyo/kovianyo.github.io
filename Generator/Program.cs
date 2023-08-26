namespace Generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Directory.CreateDirectory("html");
            File.WriteAllText("html/index.html", "Hello World3");
        }
    }
}
