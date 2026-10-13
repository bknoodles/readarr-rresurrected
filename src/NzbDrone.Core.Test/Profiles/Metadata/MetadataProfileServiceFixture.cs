using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Profiles.Metadata
{
    [TestFixture]

    public class MetadataProfileServiceFixture : CoreTest<MetadataProfileService>
    {
        [Test]
        public void init_should_add_default_profiles()
        {
            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IMetadataProfileRepository>()
                  .Verify(v => v.Insert(It.IsAny<MetadataProfile>()), Times.Exactly(2));
        }

        [Test]

        //This confirms that new profiles are added only if no other profiles exists.
        //We don't want to keep adding them back if a user deleted them on purpose.
        public void Init_should_skip_if_any_profiles_already_exist()
        {
            Mocker.GetMock<IMetadataProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(Builder<MetadataProfile>.CreateListOfSize(2).Build().ToList());

            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IMetadataProfileRepository>()
                  .Verify(v => v.Insert(It.Is<MetadataProfile>(x => x.Name == "Standard")), Times.Never());
        }

        [Test]
        public void init_should_add_none_profile_if_it_doesnt_exist()
        {
            Mocker.GetMock<IMetadataProfileRepository>()
                  .Setup(s => s.All())
                  .Returns(Builder<MetadataProfile>.CreateListOfSize(2).Build().ToList());

            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IMetadataProfileRepository>()
                  .Verify(v => v.Insert(It.Is<MetadataProfile>(x => x.Name == "None")), Times.Once());
        }

        [Test]
        public void init_should_move_existing_none_profile()
        {
            var profiles = Builder<MetadataProfile>.CreateListOfSize(2)
                .TheFirst(1)
                .With(x => x.Name = MetadataProfileService.NONE_PROFILE_NAME)
                .BuildList();

            Mocker.GetMock<IMetadataProfileRepository>()
                .Setup(s => s.All())
                .Returns(profiles);

            Subject.Handle(new ApplicationStartedEvent());

            Mocker.GetMock<IMetadataProfileRepository>()
                .Verify(v => v.Update(It.Is<MetadataProfile>(x => x.Id == profiles.First().Id && x.Name == "None.1")), Times.Once());

            Mocker.GetMock<IMetadataProfileRepository>()
                  .Verify(v => v.Insert(It.Is<MetadataProfile>(x => x.Name == "None")), Times.Once());
        }

        [Test]
        public void should_not_be_able_to_edit_none_profile()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Name = MetadataProfileService.NONE_PROFILE_NAME)
                .Build();

            Assert.Throws<InvalidOperationException>(() => Subject.Update(profile));
        }

        [Test]
        public void should_not_be_able_to_delete_none_profile()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Name = MetadataProfileService.NONE_PROFILE_NAME)
                .Build();

            Mocker.GetMock<IMetadataProfileRepository>().Setup(c => c.Get(profile.Id)).Returns(profile);

            Assert.Throws<MetadataProfileInUseException>(() => Subject.Delete(profile.Id));
        }

        [Test]
        public void should_not_be_able_to_delete_profile_if_assigned_to_author()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Id = 2)
                .Build();

            var authorList = Builder<Author>.CreateListOfSize(3)
                                            .Random(1)
                                            .With(c => c.MetadataProfileId = profile.Id)
                                            .Build().ToList();

            var importLists = Builder<ImportListDefinition>.CreateListOfSize(2)
                .All()
                .With(c => c.MetadataProfileId = 1)
                .Build().ToList();

            var rootFolders = Builder<RootFolder>.CreateListOfSize(2)
                .All()
                .With(f => f.DefaultMetadataProfileId = 1)
                .BuildList();

            Mocker.GetMock<IAuthorService>().Setup(c => c.GetAllAuthors()).Returns(authorList);
            Mocker.GetMock<IImportListFactory>().Setup(c => c.All()).Returns(importLists);
            Mocker.GetMock<IRootFolderService>().Setup(c => c.All()).Returns(rootFolders);
            Mocker.GetMock<IMetadataProfileRepository>().Setup(c => c.Get(profile.Id)).Returns(profile);

            Assert.Throws<MetadataProfileInUseException>(() => Subject.Delete(profile.Id));

            Mocker.GetMock<IMetadataProfileRepository>().Verify(c => c.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_not_be_able_to_delete_profile_if_assigned_to_import_list()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Id = 2)
                .Build();

            var authorList = Builder<Author>.CreateListOfSize(3)
                .All()
                .With(c => c.MetadataProfileId = 1)
                .Build().ToList();

            var importLists = Builder<ImportListDefinition>.CreateListOfSize(2)
                .Random(1)
                .With(c => c.MetadataProfileId = profile.Id)
                .Build().ToList();

            var rootFolders = Builder<RootFolder>.CreateListOfSize(2)
                .All()
                .With(f => f.DefaultMetadataProfileId = 1)
                .BuildList();

            Mocker.GetMock<IAuthorService>().Setup(c => c.GetAllAuthors()).Returns(authorList);
            Mocker.GetMock<IImportListFactory>().Setup(c => c.All()).Returns(importLists);
            Mocker.GetMock<IRootFolderService>().Setup(c => c.All()).Returns(rootFolders);
            Mocker.GetMock<IMetadataProfileRepository>().Setup(c => c.Get(profile.Id)).Returns(profile);

            Assert.Throws<MetadataProfileInUseException>(() => Subject.Delete(profile.Id));

            Mocker.GetMock<IMetadataProfileRepository>().Verify(c => c.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_not_be_able_to_delete_profile_if_assigned_to_root_folder()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Id = 2)
                .Build();

            var authorList = Builder<Author>.CreateListOfSize(3)
                .All()
                .With(c => c.MetadataProfileId = 1)
                .Build().ToList();

            var importLists = Builder<ImportListDefinition>.CreateListOfSize(2)
                .All()
                .With(c => c.MetadataProfileId = 1)
                .Build().ToList();

            var rootFolders = Builder<RootFolder>.CreateListOfSize(2)
                .Random(1)
                .With(f => f.DefaultMetadataProfileId = profile.Id)
                .BuildList();

            Mocker.GetMock<IAuthorService>().Setup(c => c.GetAllAuthors()).Returns(authorList);
            Mocker.GetMock<IImportListFactory>().Setup(c => c.All()).Returns(importLists);
            Mocker.GetMock<IRootFolderService>().Setup(c => c.All()).Returns(rootFolders);
            Mocker.GetMock<IMetadataProfileRepository>().Setup(c => c.Get(profile.Id)).Returns(profile);

            Assert.Throws<MetadataProfileInUseException>(() => Subject.Delete(profile.Id));

            Mocker.GetMock<IMetadataProfileRepository>().Verify(c => c.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_delete_profile_if_not_assigned_to_author_import_list_or_root_folder()
        {
            var profile = Builder<MetadataProfile>.CreateNew()
                .With(p => p.Id = 1)
                .Build();

            var authorList = Builder<Author>.CreateListOfSize(3)
                                            .All()
                                            .With(c => c.MetadataProfileId = 2)
                                            .Build().ToList();

            var importLists = Builder<ImportListDefinition>.CreateListOfSize(2)
                .All()
                .With(c => c.MetadataProfileId = 2)
                .Build().ToList();

            var rootFolders = Builder<RootFolder>.CreateListOfSize(2)
                .All()
                .With(f => f.DefaultMetadataProfileId = 2)
                .BuildList();

            Mocker.GetMock<IAuthorService>().Setup(c => c.GetAllAuthors()).Returns(authorList);
            Mocker.GetMock<IImportListFactory>().Setup(c => c.All()).Returns(importLists);
            Mocker.GetMock<IRootFolderService>().Setup(c => c.All()).Returns(rootFolders);
            Mocker.GetMock<IMetadataProfileRepository>().Setup(c => c.Get(profile.Id)).Returns(profile);

            Subject.Delete(1);

            Mocker.GetMock<IMetadataProfileRepository>().Verify(c => c.Delete(1), Times.Once());
        }

        private static Book GivenRemoteBook(string id)
        {
            return new Book
            {
                ForeignBookId = id,
                Title = $"Book {id}",
                Ratings = new Ratings { Votes = 100, Value = 4 },
                ReleaseDate = DateTime.UtcNow.AddYears(-1),
                Editions = new List<Edition> { new Edition { ForeignEditionId = $"e{id}", Title = $"Book {id}", PageCount = 300 } }
            };
        }

        private List<Book> FilterWithSeries(List<Series> series, params Book[] books)
        {
            var profile = new MetadataProfile { Id = 1, SkipSeriesSecondary = true, Ignored = new List<string>() };

            Mocker.GetMock<IMetadataProfileRepository>().Setup(s => s.Get(1)).Returns(profile);
            Mocker.GetMock<IMediaFileService>().Setup(s => s.GetFilesByAuthor(It.IsAny<int>())).Returns(new List<BookFile>());

            var author = new Author
            {
                Metadata = new AuthorMetadata { ForeignAuthorId = "100" },
                Series = series == null ? null : new LazyLoaded<List<Series>>(series),
                Books = new List<Book>(books)
            };

            return Subject.FilterBooks(author, 1);
        }

        // A metadata server can list a series without any works; until the payload is
        // normalised that series reaches here with no links at all.
        [TestCase(true)]
        [TestCase(false)]
        public void filter_books_should_ignore_series_without_links(bool nullLazyLoaded)
        {
            var primary = GivenRemoteBook("1");
            var secondary = GivenRemoteBook("2");

            var valid = new Series { ForeignSeriesId = "s1", Title = "Valid" };
            valid.LinkItems = new List<SeriesBookLink>
            {
                new SeriesBookLink { Book = primary, Series = valid, IsPrimary = true },
                new SeriesBookLink { Book = secondary, Series = valid, IsPrimary = false }
            };

            var empty = new Series
            {
                ForeignSeriesId = "s2",
                Title = "Empty",
                LinkItems = nullLazyLoaded ? null : new LazyLoaded<List<SeriesBookLink>>(null)
            };

            var result = FilterWithSeries(new List<Series> { empty, valid }, primary, secondary);

            // the secondary-series rule still applies to the series that does have links
            result.Select(x => x.ForeignBookId).Should().Equal("1");
        }

        [Test]
        public void filter_books_should_handle_author_without_series()
        {
            var result = FilterWithSeries(null, GivenRemoteBook("1"));

            result.Select(x => x.ForeignBookId).Should().Equal("1");
        }
    }
}
