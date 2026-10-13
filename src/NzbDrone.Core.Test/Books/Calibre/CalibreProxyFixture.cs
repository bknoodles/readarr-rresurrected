using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Books.Calibre
{
    [TestFixture]
    public class CalibreProxyFixture : CoreTest<CalibreProxy>
    {
        private readonly List<HttpRequest> _requests = new List<HttpRequest>();

        [SetUp]
        public void SetUp()
        {
            _requests.Clear();

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                  .Callback<HttpRequest>(r => _requests.Add(r))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), "{}", System.Net.HttpStatusCode.OK));
        }

        [TearDown]
        public void TearDown()
        {
            ExceptionVerification.IgnoreErrors();
            ExceptionVerification.IgnoreWarns();
        }

        private JObject SetFieldsChanges(string language)
        {
            var book = new Book { Title = "Book", Genres = new List<string>(), SeriesLinks = new List<SeriesBookLink>() };
            var edition = new Edition { Title = "Book", Language = language, Book = book, Ratings = new Ratings() };
            var file = new BookFile
            {
                CalibreId = 5,
                Path = "/library/Author/Book (5)/Book.epub",
                Edition = edition,
                Author = new Author { Metadata = new AuthorMetadata { Name = "Author" } }
            };

            try
            {
                // Readarr reads the book back afterwards; only the request sent first matters here
                Subject.SetFields(file, new CalibreSettings { Host = "calibre", Port = 8080, Library = "lib" }, false);
            }
            catch
            {
            }

            var request = _requests.First(r => r.Url.FullUri.Contains("cdb/set-fields/5"));
            return (JObject)JObject.Parse(request.ContentSummary)["changes"];
        }

        // Calibre's content server reads languages as a list. Calibre before August 2026 iterated
        // a plain string, so "eng" reached it as the three languages "e", "n" and "g".
        [TestCase("English", "eng")]
        [TestCase("eng", "eng")]
        [TestCase("fr", "fra")]
        public void set_fields_should_send_languages_as_a_list(string language, string expected)
        {
            var languages = SetFieldsChanges(language)["languages"];

            languages.Should().BeOfType<JArray>();
            languages.ToObject<List<string>>().Should().Equal(expected);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a language")]
        public void set_fields_should_leave_calibre_languages_alone_when_language_unknown(string language)
        {
            SetFieldsChanges(language).ContainsKey("languages").Should().BeFalse();
        }
    }
}
