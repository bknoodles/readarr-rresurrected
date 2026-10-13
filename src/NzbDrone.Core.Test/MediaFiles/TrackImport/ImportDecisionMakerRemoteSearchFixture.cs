using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.BookImport
{
    // Only rescans after a refresh (FilterFilesType.Matched) trust an earlier "searched, no
    // match", and only for a file that hasn't changed since and a search under 30 days old.
    [TestFixture]
    public class ImportDecisionMakerRemoteSearchFixture : FileSystemTest<ImportDecisionMaker>
    {
        private static readonly DateTime LastWrite = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _path = "/books/Unknown/book.epub".AsOsAgnostic();
        private BookFile _dbFile;

        [SetUp]
        public void SetUp()
        {
            FileSystem.AddFile(_path, new MockFileData("".PadRight(10)) { LastWriteTime = LastWrite });

            _dbFile = new BookFile { Path = _path, Size = 10, Modified = LastWrite, LastRemoteSearchTime = DateTime.UtcNow.AddDays(-3) };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.FilterUnchangedFiles(It.IsAny<List<IFileInfo>>(), It.IsAny<FilterFilesType>()))
                  .Returns((List<IFileInfo> files, FilterFilesType filter) => files);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFileWithPath(It.IsAny<List<string>>()))
                  .Returns(() => new List<BookFile> { _dbFile });

            Mocker.GetMock<IMetadataTagService>()
                  .Setup(s => s.ReadTags(It.IsAny<IFileInfo>()))
                  .Returns(new ParsedTrackInfo());
        }

        private LocalBook Read(FilterFilesType filter)
        {
            var files = new List<IFileInfo> { DiskProvider.GetFileInfo(_path) };
            return Subject.GetLocalTracks(files, null, null, filter).Item1.Single();
        }

        [Test]
        public void rescan_should_carry_a_recent_search_for_an_unchanged_file()
        {
            Read(FilterFilesType.Matched).LastRemoteSearchTime.Should().Be(_dbFile.LastRemoteSearchTime);
        }

        [TestCase(FilterFilesType.None)]
        [TestCase(FilterFilesType.Known)]
        public void other_scans_should_search_again(FilterFilesType filter)
        {
            Read(filter).LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void rescan_should_search_again_after_30_days()
        {
            _dbFile.LastRemoteSearchTime = DateTime.UtcNow.AddDays(-31);

            Read(FilterFilesType.Matched).LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void rescan_should_search_again_when_the_file_size_changed()
        {
            _dbFile.Size = 9;

            Read(FilterFilesType.Matched).LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void rescan_should_search_again_when_the_file_was_modified()
        {
            _dbFile.Modified = LastWrite.AddMinutes(-5);

            Read(FilterFilesType.Matched).LastRemoteSearchTime.Should().NotHaveValue();
        }

        [Test]
        public void rescan_should_search_a_file_never_searched()
        {
            _dbFile.LastRemoteSearchTime = null;

            Read(FilterFilesType.Matched).LastRemoteSearchTime.Should().NotHaveValue();
        }
    }
}
