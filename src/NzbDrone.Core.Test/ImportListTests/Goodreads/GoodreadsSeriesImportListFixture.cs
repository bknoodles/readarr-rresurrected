using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Goodreads;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.Goodreads
{
    // Goodreads returns a series 100 works at a time: page 1 by default, then ?page=2 and so
    // on, with an empty page past the end. Nancy Drew Mystery Stories (175 works) comes back
    // as 100 + 75 + 0, The Hardy Boys (238) as 100 + 100 + 38 + 0.
    [TestFixture]
    public class GoodreadsSeriesImportListFixture : CoreTest<GoodreadsSeriesImportList>
    {
        private readonly List<int> _requestedPages = new List<int>();
        private Func<int, string> _pages;

        [SetUp]
        public void SetUp()
        {
            _requestedPages.Clear();

            Mocker.GetMock<ICachedHttpResponseService>()
                  .Setup(s => s.Get(It.IsAny<HttpRequest>(), It.IsAny<bool>(), It.IsAny<TimeSpan>()))
                  .Returns<HttpRequest, bool, TimeSpan>((r, _, _) =>
                  {
                      var query = HttpUtility.ParseQueryString(new Uri(r.Url.FullUri).Query);
                      var page = int.TryParse(query["page"], out var p) ? p : 1;
                      _requestedPages.Add(page);
                      return new HttpResponse(r, new HttpHeader(), _pages(page), HttpStatusCode.OK);
                  });

            Mocker.SetConstant<IProvideSeriesInfo>(Mocker.Resolve<GoodreadsProxy>());

            Subject.Definition = new ImportListDefinition
            {
                Id = 1,
                Name = "Series",
                Settings = new GoodreadsSeriesImportListSettings { SeriesId = 40642 }
            };
        }

        private static string SeriesXml(int total, IEnumerable<int> workIds)
        {
            var works = new StringBuilder();
            foreach (var id in workIds)
            {
                works.Append($@"
<series_work><id>{id + 500000}</id><user_position>{id}</user_position>
<work><id>{id}</id>
<best_book><id>{id + 900000}</id><title>Book {id}</title><author><id>6001</id><name>Carolyn Keene</name></author></best_book>
<original_title>Book {id}</original_title></work></series_work>");
            }

            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<GoodreadsResponse><series><id>40642</id><title><![CDATA[Nancy Drew Mystery Stories]]></title>
<series_works_count>{total}</series_works_count><primary_work_count>{total}</primary_work_count><numbered>true</numbered>
<series_works>{works}</series_works></series></GoodreadsResponse>";
        }

        // Pages of 100 from a series of `total` works, empty past the end
        private void GivenSeries(int total)
        {
            _pages = page => SeriesXml(total, Enumerable.Range(((page - 1) * 100) + 1, Math.Max(0, Math.Min(100, total - ((page - 1) * 100)))));
        }

        [TestCase(10, 1)]
        [TestCase(100, 1)]
        [TestCase(175, 2)]
        [TestCase(238, 3)]
        public void should_import_every_work_in_the_series(int total, int pagesWithWorks)
        {
            GivenSeries(total);

            var items = Subject.Fetch();

            items.Should().HaveCount(total);
            items.Select(x => x.BookGoodreadsId).Should().OnlyHaveUniqueItems();
            items.Last().BookGoodreadsId.Should().Be(total.ToString());
            _requestedPages.Max().Should().BeLessOrEqualTo(pagesWithWorks + 1);
        }

        [Test]
        public void should_stop_when_goodreads_repeats_the_last_page()
        {
            // a server that ignores ?page would otherwise be asked for pages forever
            _pages = page => SeriesXml(175, Enumerable.Range(1, 100));

            var items = Subject.Fetch();

            items.Should().HaveCount(100);
            _requestedPages.Should().HaveCountLessOrEqualTo(2);
        }

        [Test]
        public void should_stop_when_the_series_count_is_reached_even_if_pages_continue()
        {
            // the count says 150, and page 3 would wrongly start over
            _pages = page => SeriesXml(150, page == 2 ? Enumerable.Range(101, 50) : Enumerable.Range(1, 100));

            var items = Subject.Fetch();

            items.Should().HaveCount(150);
            _requestedPages.Should().Equal(1, 2);
        }

        [Test]
        public void should_keep_what_it_has_when_a_later_page_fails()
        {
            GivenSeries(175);
            var full = _pages;
            _pages = page => page == 1 ? full(page) : throw new HttpException(new HttpResponse(new HttpRequest("http://goodreads"), new HttpHeader(), "", HttpStatusCode.ServiceUnavailable));

            var items = Subject.Fetch();

            // list sync only ever adds books, so the works already fetched are still worth adding
            items.Should().HaveCount(100);
            Mocker.GetMock<IImportListStatusService>().Verify(s => s.RecordFailure(1, It.IsAny<TimeSpan>()), Times.Once());
        }
    }
}
