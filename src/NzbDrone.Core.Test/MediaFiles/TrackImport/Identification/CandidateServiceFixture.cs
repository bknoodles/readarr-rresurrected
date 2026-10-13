using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class CandidateServiceFixture : CoreTest<CandidateService>
    {
        [Test]
        public void should_not_throw_on_goodreads_exception()
        {
            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>(), true))
                .Throws(new GoodreadsException("Bad search"));

            var edition = new LocalEdition
            {
                LocalBooks = new List<LocalBook>
                {
                    new LocalBook
                    {
                        FileTrackInfo = new ParsedTrackInfo
                        {
                            Authors = new List<string> { "Author" },
                            BookTitle = "Book"
                        }
                    }
                }
            };

            Subject.GetRemoteCandidates(edition, null).Should().BeEmpty();
        }

        private static LocalEdition GivenTaggedEdition(string author = "Author", string title = "Book")
        {
            return new LocalEdition
            {
                LocalBooks = new List<LocalBook>
                {
                    new LocalBook
                    {
                        FileTrackInfo = new ParsedTrackInfo
                        {
                            Authors = author == null ? new List<string>() : new List<string> { author },
                            BookTitle = title
                        }
                    }
                }
            };
        }

        [Test]
        public void a_search_with_no_results_should_count_as_searched_without_failure()
        {
            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>(), true))
                .Returns(new List<Book>());

            var edition = GivenTaggedEdition();

            using (var scope = MetadataFailureScope.Begin())
            {
                Subject.GetRemoteCandidates(edition, null).ToList().Should().BeEmpty();

                scope.Failed.Should().BeFalse();
            }

            edition.RemoteSearched.Should().BeTrue();
        }

        [Test]
        public void a_metadata_service_error_should_be_reported_and_the_other_searches_still_run()
        {
            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook("Book", "Author", true))
                .Throws(new BookInfoException("bookinfo returned 503"));

            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook("Book", null, true))
                .Returns(new List<Book>());

            Mocker.GetMock<ISearchForNewBook>()
                .Setup(s => s.SearchForNewBook("Author", null, true))
                .Returns(new List<Book>());

            var edition = GivenTaggedEdition();

            using (var scope = MetadataFailureScope.Begin())
            {
                Assert.DoesNotThrow(() => Subject.GetRemoteCandidates(edition, null).ToList());

                scope.Failed.Should().BeTrue();
            }

            Mocker.GetMock<ISearchForNewBook>().Verify(s => s.SearchForNewBook("Book", null, true), Times.Once());
        }

        [Test]
        public void files_without_usable_tags_should_not_count_as_searched()
        {
            var edition = GivenTaggedEdition(author: null, title: null);

            Subject.GetRemoteCandidates(edition, null).ToList().Should().BeEmpty();

            edition.RemoteSearched.Should().BeFalse();
            Mocker.GetMock<ISearchForNewBook>().Verify(s => s.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }
    }
}
