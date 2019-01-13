using System;
using System.Reflection;

namespace WebScraping.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                Console.WriteLine($"WebScraping.Cmd v.{assembly.GetName().Version}");

                if (args.Length != 4)
                {
                    Console.WriteLine("Syntax: webScraping.cmd.exe <article> <language> <limt> <output path>");
                }
                else
                {
                    var article = args[0];
                    var language = args[1];
                    var limit = int.Parse(args[2]);
                    var outputFolder = args[3];

                    var wikipediaWebScraper = new WikipediaWebScraper(
                        article,
                        language,
                        limit,
                        outputFolder);

                    wikipediaWebScraper.ArticleProcessed += WikipediaWebScraper_ArticleProcessed;
                    var startTime = DateTime.Now;

                    var numberOfProcessedArticles = wikipediaWebScraper.Run();

                    var duration = DateTime.Now - startTime;

                    Console.WriteLine($"Number of processed articles: {numberOfProcessedArticles}");
                    Console.WriteLine($"Execution time: {duration.Minutes} minutes and {duration.Seconds} seconds");
                }
            }
            catch (Exception ex)
            {
                ConsoleColor foregroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Oops! Something went wrong: " + ex.Message);
                Console.ForegroundColor = foregroundColor;
            }
        }

        private static void WikipediaWebScraper_ArticleProcessed(object sender, WikipediaWebScraperEventArgs e)
        {
            Console.WriteLine(e.Article);
            Console.WriteLine($"\tNumber of links: {e.NumberOfLinks}");
            Console.WriteLine($"\tNumber of words: {e.NumberOfWords}");
        }
    }
}
