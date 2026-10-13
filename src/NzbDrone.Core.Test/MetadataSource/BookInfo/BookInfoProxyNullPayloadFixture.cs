using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.BookInfo
{
    // Metadata servers written in Go, rreading-glasses among them, encode an empty list as null
    // rather than []. READARR_METADATA_URL can point at one, so every list in a payload may be null.
    [TestFixture]
    public class BookInfoProxyNullPayloadFixture : CoreTest<BookInfoProxy>
    {
        private const string Edition = @"{""ForeignId"": 11, ""Title"": ""Book One"", ""RatingCount"": 10, ""AverageRating"": 4.0, ""Contributors"": [{""ForeignId"": 100, ""Role"": ""Author""}]}";
        private const string Author = @"{""ForeignId"": 100, ""Name"": ""Jane Author"", ""Works"": [], ""Series"": []}";

        [SetUp]
        public void SetUp()
        {
            var factory = new HttpRequestBuilder("http://test/{route}").CreateFactory();
            Mocker.GetMock<IMetadataRequestBuilder>()
                  .Setup(x => x.GetRequestBuilder())
                  .Returns(factory);

            Mocker.GetMock<IAuthorMetadataService>()
                  .Setup(x => x.FindById(It.IsAny<string>()))
                  .Returns((AuthorMetadata)null);
        }

        private void GivenGet(string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(x => x.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), json, HttpStatusCode.OK));
        }

        private static string Work(int id, string edition, string series = "[]")
        {
            return $@"{{""ForeignId"": {id}, ""Title"": ""Book {id}"", ""Books"": [{edition}], ""Series"": {series}, ""Authors"": [{Author}]}}";
        }

        private static string AuthorPayload(string series, string works = null)
        {
            works ??= Work(1, Edition);
            return $@"{{""ForeignId"": 100, ""Name"": ""Jane Author"", ""Works"": [{works}], ""Series"": {series}}}";
        }

        [Test]
        public void author_with_series_whose_link_items_are_null_should_load()
        {
            GivenGet(AuthorPayload(@"[{""ForeignId"": 7, ""Title"": ""Empty Series"", ""LinkItems"": null}]"));

            var author = Subject.GetAuthorInfo("100");

            author.Books.Value.Should().ContainSingle();
            author.Series.Value.Should().ContainSingle()
                .Which.LinkItems.Value.Should().BeEmpty();
        }

        [Test]
        public void author_with_series_without_link_items_should_have_empty_links_not_null()
        {
            GivenGet(AuthorPayload(@"[{""ForeignId"": 7, ""Title"": ""Empty Series"", ""LinkItems"": []}]"));

            var author = Subject.GetAuthorInfo("100");

            author.Series.Value.Should().ContainSingle()
                .Which.LinkItems.Value.Should().NotBeNull().And.BeEmpty();
        }

        [Test]
        public void author_with_null_series_list_should_load()
        {
            GivenGet(AuthorPayload("null"));

            var author = Subject.GetAuthorInfo("100");

            author.Books.Value.Should().ContainSingle();
            author.Series.Value.Should().BeEmpty();
        }

        [Test]
        public void author_work_whose_editions_have_null_contributors_should_not_throw()
        {
            var noContributors = @"{""ForeignId"": 12, ""Title"": ""Book Two"", ""RatingCount"": 50, ""AverageRating"": 4.5, ""Contributors"": null}";
            GivenGet(AuthorPayload("[]", Work(1, Edition) + "," + Work(2, noContributors)));

            var author = Subject.GetAuthorInfo("100");

            // Work 2 can't be attributed to anyone, so it's left out like any other work by someone else
            author.Books.Value.Select(x => x.ForeignBookId).Should().Equal("1");
        }

        [Test]
        public void author_work_with_null_genres_and_related_works_should_load()
        {
            var work = $@"{{""ForeignId"": 1, ""Title"": ""Book 1"", ""Genres"": null, ""RelatedWorks"": null, ""Books"": [{Edition}], ""Series"": null, ""Authors"": [{Author}]}}";
            GivenGet(AuthorPayload("[]", work));

            var author = Subject.GetAuthorInfo("100");

            author.Books.Value.Should().ContainSingle();
        }

        [Test]
        public void work_whose_editions_have_null_contributors_should_load()
        {
            var noContributors = @"{""ForeignId"": 11, ""Title"": ""Book One"", ""RatingCount"": 10, ""AverageRating"": 4.0, ""Contributors"": null}";
            GivenGet(Work(1, noContributors, "null"));

            var result = Subject.GetBookInfo("1");

            result.Item1.Should().Be("0");
            result.Item2.ForeignBookId.Should().Be("1");
        }

        [Test]
        public void work_with_series_whose_link_items_are_null_should_load()
        {
            GivenGet(Work(1, Edition, @"[{""ForeignId"": 7, ""Title"": ""Empty Series"", ""LinkItems"": null}]"));

            var result = Subject.GetBookInfo("1");

            result.Item1.Should().Be("100");
        }

        private void GivenSearchHits(params int[] editionIds)
        {
            Mocker.GetMock<IGoodreadsSearchProxy>()
                  .Setup(x => x.Search(It.IsAny<string>()))
                  .Returns(editionIds.Select(id => new SearchJsonResource
                  {
                      BookId = id,
                      WorkId = id,
                      Author = new AuthorJsonResource { Id = 100 }
                  }).ToList());
        }

        private void GivenBulk(string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(x => x.Post<BulkBookResource>(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse<BulkBookResource>(new HttpResponse(r, new HttpHeader(), json, HttpStatusCode.OK)));
        }

        [TestCase("null")]
        [TestCase("[]")]
        public void search_should_return_books_whose_top_edition_has_no_contributors(string contributors)
        {
            // The most rated edition of work 2 has no contributors; a less rated one does
            var topEdition = $@"{{""ForeignId"": 21, ""Title"": ""Book Two"", ""RatingCount"": 500, ""AverageRating"": 4.5, ""Contributors"": {contributors}}}";
            var otherEdition = @"{""ForeignId"": 22, ""Title"": ""Book Two"", ""RatingCount"": 5, ""AverageRating"": 4.0, ""Contributors"": [{""ForeignId"": 100, ""Role"": ""Author""}]}";

            GivenSearchHits(11, 21);
            GivenBulk($@"{{""Works"": [{Work(1, Edition)}, {Work(2, topEdition + "," + otherEdition)}], ""Series"": [], ""Authors"": [{Author}]}}");

            var books = Subject.SearchForNewBook("book", null, false);

            books.Select(x => x.ForeignBookId).Should().BeEquivalentTo(new[] { "1", "2" });
            books.Single(x => x.ForeignBookId == "2").AuthorMetadata.Value.ForeignAuthorId.Should().Be("100");
        }

        [Test]
        public void search_should_return_books_when_bulk_series_are_null()
        {
            GivenSearchHits(11, 21);
            GivenBulk($@"{{""Works"": [{Work(1, Edition)}, {Work(2, Edition.Replace("11", "21"))}], ""Series"": null, ""Authors"": [{Author}]}}");

            var books = Subject.SearchForNewBook("book", null, false);

            books.Should().HaveCount(2);
        }
    }
}
