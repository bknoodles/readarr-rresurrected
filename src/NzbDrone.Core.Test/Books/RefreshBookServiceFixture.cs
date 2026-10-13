using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Books
{
    // A book can be removed from Goodreads after it was added; the metadata service then
    // answers 404 for its work id.
    [TestFixture]
    public class RefreshBookServiceFixture : CoreTest<RefreshBookService>
    {
        private Book _removed;
        private Book _present;

        [SetUp]
        public void SetUp()
        {
            _removed = new Book { Id = 1, ForeignBookId = "111", Title = "Removed", AuthorMetadataId = 5 };
            _present = new Book { Id = 2, ForeignBookId = "222", Title = "Present", AuthorMetadataId = 5 };

            Mocker.GetMock<IProvideBookInfo>()
                  .Setup(s => s.GetBookInfo("111"))
                  .Throws(new BookNotFoundException("111"));

            Mocker.GetMock<IBookService>()
                  .Setup(s => s.GetBooks(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Book> { _removed, _present });

            Mocker.GetMock<IBookService>()
                  .Setup(s => s.GetBook(1))
                  .Returns(_removed);
        }

        [TearDown]
        public void TearDown()
        {
            ExceptionVerification.IgnoreErrors();
            ExceptionVerification.IgnoreWarns();
        }

        [Test]
        public void refreshing_a_book_removed_upstream_should_not_throw()
        {
            Assert.DoesNotThrow(() => Subject.Execute(new RefreshBookCommand(1)));
        }

        [Test]
        public void refreshing_a_book_removed_upstream_should_report_no_change()
        {
            Subject.RefreshBookInfo(_removed).Should().BeFalse();
        }

        [Test]
        public void bulk_refresh_should_continue_past_a_book_removed_upstream()
        {
            Subject.Execute(new BulkRefreshBookCommand(new List<int> { 1, 2 }));

            Mocker.GetMock<IProvideBookInfo>().Verify(s => s.GetBookInfo("222"), Times.Once());
        }
    }
}
