using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    /// <summary>
    /// Records when a file was last searched for on the metadata service without a match, so
    /// rescans stop searching again for files that can't be identified (see IdentificationService).
    ///
    /// Bookshelf adds the same column under its migration 41, so a database brought over from it
    /// may already have the column; the check makes this a no-op there, as 045 does for Kca.
    /// </summary>
    [Migration(046)]
    public class bookfile_last_remote_search : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase("postgres").Execute.Sql(
                "ALTER TABLE \"BookFiles\" ADD COLUMN IF NOT EXISTS \"LastRemoteSearchTime\" TIMESTAMP WITH TIME ZONE");

            IfDatabase("sqlite").Execute.WithConnection(AddColumnIfMissing);
        }

        private void AddColumnIfMissing(IDbConnection conn, IDbTransaction tran)
        {
            var columns = conn.Query<string>(
                "SELECT \"name\" FROM pragma_table_info('BookFiles')",
                transaction: tran).ToList();

            if (columns.Contains("LastRemoteSearchTime"))
            {
                return;
            }

            conn.Execute("ALTER TABLE \"BookFiles\" ADD COLUMN \"LastRemoteSearchTime\" DATETIME", transaction: tran);
        }
    }
}
