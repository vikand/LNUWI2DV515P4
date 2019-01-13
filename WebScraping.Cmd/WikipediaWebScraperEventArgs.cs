namespace WebScraping.Cmd
{
    public class WikipediaWebScraperEventArgs
    {
        public WikipediaWebScraperEventArgs(string article, int numberOfLinks, int numberOfWords)
        {
            Article = article;
            NumberOfLinks = numberOfLinks;
            NumberOfWords = numberOfWords;
        }

        public string Article { get; private set; }
        public int NumberOfLinks { get; private set; }
        public int NumberOfWords { get; private set; }
    }
}
