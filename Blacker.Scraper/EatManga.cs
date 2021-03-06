﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Common.Models;
using Blacker.MangaScraper.Common.Utils;
using Blacker.Scraper.Models;
using Blacker.Scraper.Helpers;
using Blacker.Scraper.Exceptions;
using log4net;

namespace Blacker.Scraper
{
    public class EatManga : BaseScraper, IScraper, IImmediateSearchProvider, IPreload
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EatManga));

        private const string DictionaryUrl = "http://eatmanga.com/Manga-Scan";
        private const string EatMangaUrl = "http://eatmanga.com";

        private readonly Cache<string, object> _cache;

        private const string MangasCacheKey = "Mangas";
        private const string ChapterCacheKey = "Chapters";

        private readonly object _syncRoot = new object();

        public EatManga()
        {
            _cache = new Cache<string, object>();
            HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("option");
        }

        protected override string BaseUrl
        {
            get { return EatMangaUrl; }
        }

        #region IScraper implementation

        public string Name { get { return "EatManga.com"; } }

        public Guid ScraperGuid
        {
            get { return Guid.Parse("0fca9b5c-db34-4bbf-90d5-79a3edabfcd4"); }
        }

        public IEnumerable<IChapterRecord> GetAvailableChapters(IMangaRecord manga)
        {
            if (manga == null)
                throw new ArgumentNullException("manga");
            if (manga.Scraper != ScraperGuid)
                throw new ArgumentException("Manga record is not for " + Name, "manga");

            var cacheKey = ChapterCacheKey + manga.MangaName + manga.Url;

            var cached = _cache[cacheKey] as IEnumerable<ChapterRecord>;
            if (cached != null) // if chapters are already in cache return them
                return cached;

            var records = new List<ChapterRecord>();
            var document = WebHelper.GetHtmlDocument(manga.Url);

            var chapters = document.SelectNodes(@"//table[@id=""updates""]//th/a");
            if (chapters == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var chapter in chapters)
            {
                var sibling = chapter.ParentNode.NextSibling;
                if (sibling != null)
                {
                    // skip "in xx days" nodes
                    if (sibling.InnerText.IndexOf("ago", StringComparison.InvariantCultureIgnoreCase) == -1)
                        continue;
                }

                var url = GetFullUrl(chapter.Attributes["href"].Value);

                records.Add(new ChapterRecord(ScraperGuid, url)
                {
                    ChapterName = CleanupText(chapter.InnerText),
                    Url = url,
                    MangaRecord = manga
                });
            }

            // save to cache
            _cache[cacheKey] = records;

            return records;
        }

        public IEnumerable<IMangaRecord> GetAvailableMangas(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        public IDownloader GetDownloader()
        {
            return new Downloader(GetPages, @"//img[starts-with(@id, ""eatmanga_image"")]");
        }

        #endregion // IScraper implementation

        #region IImmediateSearchProvider implementation

        public IEnumerable<IMangaRecord> GetAvailableMangasImmediate(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");

            return Mangas.Where(mr => mr.MangaName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1);
        }

        #endregion // IImediateSearchProvider implementation

        #region IPreload implementation

        public void PreloadDirectory()
        {
            lock (_syncRoot)
            {
                // load mangas to cache
                var mangas = _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                if (mangas == null)
                    _cache[MangasCacheKey] = LoadAllMangas();
            }
        }

        #endregion // IPreload implementation

        #region Private properties

        private IEnumerable<MangaRecord> Mangas
        {
            get
            {
                lock (_syncRoot)
                {
                    var mangas = _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                    if (mangas == null)
                        _cache[MangasCacheKey] = LoadAllMangas();

                    return _cache[MangasCacheKey] as IEnumerable<MangaRecord>;
                }
            }
        }

        #endregion // Private properties

        #region Private methods

        private IEnumerable<MangaRecord> LoadAllMangas()
        {
            var records = new List<MangaRecord>();
            var document = WebHelper.GetHtmlDocument(DictionaryUrl);

            var mangas = document.SelectNodes(@"//table[@id=""updates""]//th/a");
            if (mangas == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var manga in mangas)
            {
                if (string.IsNullOrEmpty(manga.InnerText))
                    continue;

                var url = GetFullUrl(manga.Attributes["href"].Value);

                records.Add(new MangaRecord(ScraperGuid, url)
                {
                    MangaName = CleanupText(manga.InnerText),
                    Url = url
                });
            }

            return records;
        }

        private IDictionary<int, string> GetPages(IChapterRecord chapter)
        {
            IDictionary<int, string> pages = new Dictionary<int, string>();

            var document = WebHelper.GetHtmlDocument(chapter.Url);
            var chapterPages = document.SelectNodes(@"(//select[@id=""pages""])[1]/option");
            if (chapterPages == null)
            {
                throw new ParserException("Could not find expected elements on website.", document.InnerHtml);
            }

            foreach (var pageLink in chapterPages)
            {
                int pageNumber = 0;

                Int32.TryParse(pageLink.InnerText, out pageNumber);

                if (pages.ContainsKey(pageNumber))  // if page is already in dictionary use random number instead
                    pageNumber = Random;

                pages.Add(pageNumber, GetFullUrl(pageLink.Attributes["value"].Value));
            }

            return pages;
        }

        #endregion // Private methods
    }
}
