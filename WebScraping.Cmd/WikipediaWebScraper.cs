using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebScraping.Cmd
{
    public class WikipediaWebScraper
    {
        private const string CommonWordsEN =
            " the be to of and a in that have I it for not on with he as you do at this but his by from" +
            " they we say her she or an will my one all would there their what so up out if about who get" +
            " which go me when make can like time no just him know take people into year your good some" +
            " could them see other than then now look only come its over think also back after use two how" +
            " our work first well way even new want because any these give day most us ";

        private const string CommonWordsSV =
            ",i,och,att,det,som,en,på,är,av,för,med,till,den,har,de,inte,om,ett,han,men,var,jag,sig,från,vi,så,kan,man," +
            "när,år,säger,hon,under,också,efter,eller,nu,sin,där,vid,mot,ska,skulle,kommer,ut,får,finns,vara,hade,alla," +
            "andra,mycket,än,här,då,sedan,över,bara,in,blir,upp,även,vad,få,två,vill,ha,många,hur,mer,";

        private readonly string baseArticle;
        private readonly string language;
        private readonly int limit;
        private readonly string outputFolder;
        private readonly string commonWords;

        public WikipediaWebScraper(
            string baseArticle,
            string language,
            int limit,
            string outputFolder)
        {
            this.baseArticle = baseArticle;
            this.language = language;
            this.limit = limit;
            this.outputFolder = outputFolder;

            switch (language)
            {
                case "en":
                    commonWords = CommonWordsEN;
                    break;
                case "sv":
                    commonWords = CommonWordsSV;
                    break;
                default:
                    commonWords = "";
                    break;
            }
        }

        public event EventHandler<WikipediaWebScraperEventArgs> ArticleProcessed;

        public int Run()
        {
            var linkPageUrl = $"https://{this.language}.wikipedia.org/w/index.php?title=Special:WhatLinksHere/{this.baseArticle}&limit={this.limit}&hideredirs=1&hidetrans=1";
            var web = new HtmlWeb();
            var document = web.Load(linkPageUrl);

            if (document == null)
            {
                throw new Exception($"Unable to load base article page: '{linkPageUrl}'");
            }

            var pageLinks = new List<string>();
            var nodes = document.DocumentNode.SelectNodes("//ul[@id='mw-whatlinkshere-list']//a[starts-with(@href, '/wiki/')]");

            if (nodes == null || nodes.Count == 0)
            {
                throw new Exception($"Unable find links on base article page: '{linkPageUrl}'");
            }

            pageLinks = nodes
                .Select(n => n.Attributes["href"].Value)
                .Where(l => !l.Contains(":"))
                .ToList();

            foreach (var pageLink in pageLinks)
            {
                var articlePageUrl = $"https://{this.language}.wikipedia.org" + pageLink;
                document = web.Load(articlePageUrl);
                var heading = document.DocumentNode.SelectSingleNode("//h1[@id='firstHeading']").InnerText;
                var node = document.DocumentNode.SelectSingleNode("//div[@id='mw-content-text']");

                var article = pageLink.Substring(pageLink.LastIndexOf("/") + 1);

                var numberOfLinks = CreateLinksFile(article, node);
                var numberOfWords = CreateWordsFile(article, heading, node);

                OnArticleProcessed(new WikipediaWebScraperEventArgs(article, numberOfLinks, numberOfWords));
            }

            return pageLinks.Count;
        }

        private int CreateLinksFile(string article, HtmlNode node)
        {
            var links = new List<string>();
            var linkNodes = node.SelectNodes("//div[@id='mw-content-text']//a[starts-with(@href, '/wiki/')]");

            foreach (var linkNode in linkNodes)
            {
                if (!linkNode.HasClass("mw-disambig"))
                {
                    var href = linkNode.Attributes["href"].Value;
                    if (!href.Contains(":") && !links.Contains(href))
                    {
                        links.Add(href);
                    }
                }
            }

            var linkFolder = Path.Combine(this.outputFolder, "Links\\" + this.baseArticle);
            if (!Directory.Exists(linkFolder))
            {
                Directory.CreateDirectory(linkFolder);
            }

            var linkFilePath = Path.Combine(linkFolder, article);

            File.WriteAllLines(linkFilePath, links);

            return links.Count;
        }

        private int CreateWordsFile(string article, string heading, HtmlNode node)
        {
            var content = string.Join(" ", node.Descendants().Select(n => n.InnerText));
            var bagOfWords = GetBagOfWords(heading + content);
            
            var wordFolder = Path.Combine(this.outputFolder, "Words\\" + this.baseArticle);
            if (!Directory.Exists(wordFolder))
            {
                Directory.CreateDirectory(wordFolder);
            }

            var wordFilePath = Path.Combine(wordFolder, article);

            File.WriteAllText(wordFilePath, string.Join(" ", bagOfWords));

            return bagOfWords.Count;
        }

        public List<string> GetBagOfWords(string text)
        {
            var bagOfWords = new List<string>();

            if (!string.IsNullOrWhiteSpace(text))
            {
                foreach (Match match in Regex.Matches(text, @"\w+"))
                {
                    var word = match.Groups[0].Value.ToLower();

                    if (!bagOfWords.Contains(word) && !commonWords.Contains($" {word} "))
                    {
                        bagOfWords.Add(word);
                    }
                }
            }

            return bagOfWords;
        }

        protected virtual void OnArticleProcessed(WikipediaWebScraperEventArgs eventArgs)
        {
            ArticleProcessed?.Invoke(this, eventArgs);
        }
    }
}
