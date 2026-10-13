using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class bookfile_last_remote_searchFixture : MigrationTest<bookfile_last_remote_search>
    {
        private static ColumnRow[] Columns(IDirectDataMapper db)
        {
            return db.Query<ColumnRow>("SELECT \"name\" AS \"Name\", \"type\" AS \"Type\" FROM pragma_table_info('BookFiles')").ToArray();
        }

        [Test]
        public void should_add_the_column()
        {
            var db = WithMigrationTestDb(c => { });

            Columns(db).Should().ContainSingle(c => c.Name == "LastRemoteSearchTime")
                .Which.Type.Should().Be("DATETIME");
        }

        [Test]
        public void should_do_nothing_when_the_column_is_already_there()
        {
            // Bookshelf adds the same column under its migration 41
            var db = WithMigrationTestDb(c =>
            {
                c.Alter.Table("BookFiles").AddColumn("LastRemoteSearchTime").AsDateTimeOffset().Nullable();
            });

            Columns(db).Count(c => c.Name == "LastRemoteSearchTime").Should().Be(1);
        }

        private class ColumnRow
        {
            public string Name { get; set; }
            public string Type { get; set; }
        }
    }
}
