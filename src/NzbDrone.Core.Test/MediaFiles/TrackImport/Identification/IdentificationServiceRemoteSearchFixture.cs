using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    // Rescans used to search the metadata service again for every file they couldn't identify,
    // on every rescan. The search is now recorded on the file and not repeated while recent.
    [TestFixture]
    public class IdentificationServiceRemoteSearchFixture : CoreTest<IdentificationService>
    {
        private static readonly DateTime Earlier = DateTime.UtcNow.AddDays(-3);

        private List<LocalBook> _files;

        [SetUp]
        public void SetUp()
        {
            _files = new List<LocalBook>
            {
                GivenFile("/books/Unknown/book.epub")
            };

            Mocker.GetMock<ICandidateService>()
                  .Setup(x => x.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()))
                  .Returns(new List<CandidateEdition>());
        }

        private static LocalBook GivenFile(string path)
        {
            return new LocalBook
            {
                Path = path,
                FileTrackInfo = new ParsedTrackInfo { Authors = new List<string> { "Jane Author" }, BookTitle = "The Book" }
            };
        }

        // An edition the files above match closely
        private static Edition GivenEdition()
        {
            var metadata = new AuthorMetadata { Name = "Jane Author" };
            var book = new Book { Title = "The Book", AuthorMetadata = metadata, Author = new Author { Metadata = metadata }, SeriesLinks = new List<SeriesBookLink>() };
            return new Edition { Title = "The Book", Book = book };
        }

        private void GivenRemoteSearch(bool fails = false, Edition match = null)
        {
            Mocker.GetMock<ICandidateService>()
                  .Setup(x => x.GetRemoteCandidates(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>()))
                  .Returns<LocalEdition, IdentificationOverrides>((edition, _) => RemoteCandidates(edition, fails, match));
        }

        // Lazy like the real one: the search happens as the candidates are enumerated
        private static IEnumerable<CandidateEdition> RemoteCandidates(LocalEdition edition, bool fails, Edition match)
        {
            edition.RemoteSearched = true;

            if (fails)
            {
                MetadataFailureScope.ReportFailure();
            }

            if (match != null)
            {
                yield return new CandidateEdition(match);
            }
        }

        private List<LocalEdition> Identify()
        {
            return Subject.Identify(_files, new IdentificationOverrides(), new ImportDecisionMakerConfig { SingleRelease = true, AddNewAuthors = true });
        }

        private void VerifyRemoteSearched(Times times)
        {
            Mocker.GetMock<ICandidateService>().Verify(x => x.GetRemoteCandidates(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>()), times);
        }

        [Test]
        public void should_record_a_search_that_found_no_match()
        {
            GivenRemoteSearch();

            Identify();

            _files.Single().LastRemoteSearchTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Test]
        public void should_not_record_a_search_that_failed()
        {
            GivenRemoteSearch(fails: true);

            Identify();

            _files.Single().LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void should_clear_the_record_when_a_search_that_was_due_again_fails()
        {
            // an expired record isn't loaded, so this is a search that happens to fail
            GivenRemoteSearch(fails: true);
            _files.Single().LastRemoteSearchTime = null;

            Identify();

            _files.Single().LastRemoteSearchTime.Should().NotHaveValue();
            VerifyRemoteSearched(Times.Once());
        }

        [Test]
        public void should_not_search_again_for_a_file_searched_recently()
        {
            GivenRemoteSearch();
            _files.Single().LastRemoteSearchTime = Earlier;

            Identify();

            VerifyRemoteSearched(Times.Never());

            // kept as it was, so it still expires on time
            _files.Single().LastRemoteSearchTime.Should().Be(Earlier);
        }

        [Test]
        public void should_still_match_locally_a_file_searched_recently()
        {
            var edition = GivenEdition();

            Mocker.GetMock<ICandidateService>()
                  .Setup(x => x.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()))
                  .Returns(new List<CandidateEdition> { new CandidateEdition(edition) });

            _files.Single().LastRemoteSearchTime = Earlier;

            var release = Identify().Single();

            release.Edition.Title.Should().Be(edition.Title);
            VerifyRemoteSearched(Times.Never());
            _files.Single().LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void should_search_when_only_some_files_were_searched_recently()
        {
            GivenRemoteSearch();
            _files.Add(GivenFile("/books/Unknown/book2.epub"));
            _files[0].LastRemoteSearchTime = Earlier;

            Identify();

            VerifyRemoteSearched(Times.Once());
            _files.Should().OnlyContain(x => x.LastRemoteSearchTime > Earlier);
        }

        [Test]
        public void should_clear_the_record_when_the_search_finds_a_match()
        {
            GivenRemoteSearch(match: GivenEdition());

            var release = Identify().Single();

            release.Edition.Should().NotBeNull();
            _files.Single().LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void should_not_record_anything_when_no_search_was_made()
        {
            Mocker.GetMock<ICandidateService>()
                  .Setup(x => x.GetRemoteCandidates(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>()))
                  .Returns(new List<CandidateEdition>());

            Identify();

            _files.Single().LastRemoteSearchTime.Should().NotHaveValue();
        }
    }
}
