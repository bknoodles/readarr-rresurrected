# Health checks

Readarr runs these checks at startup and on a schedule, and lists any problems under
**System → Status**. Each message there links to its section below.

Paths in these messages are the paths **inside the container**. Most path problems come from the
container and the download client seeing the same files at different paths; see
[Paths and permissions in Docker](troubleshooting.md#paths-and-permissions-in-docker).

Something not covered here? Ask on [GitHub Issues](https://github.com/ricetim/readarr-rresurrected/issues).

## Indexers

### Indexers are unavailable due to failures

One or more indexers failed recently, so Readarr is backing off from them. It retries
automatically with increasing delays, and the message clears once an indexer responds again. The
variant that says "for more than 6 hours" means it has been failing for a long time.

To fix it, open **Settings → Indexers**, edit the indexer and press **Test**, then check the log
(**System → Logs**) for the error. Common causes: an expired API key or session cookie, the site
being down, or a proxy (such as Prowlarr) that is unreachable.

### No indexers available with RSS sync enabled, Readarr will not grab new releases automatically

No enabled indexer has **Enable RSS** turned on, so Readarr never sees new releases as they are
posted. Enable RSS on at least one indexer in **Settings → Indexers**. Some indexers don't support
RSS at all; see [Supported integrations](supported.md#indexers).

### No indexers available with Automatic Search enabled, Readarr will not provide any automatic search results

No enabled indexer has **Enable Automatic Search** turned on, so searches started by Readarr (for
example when you add a book) return nothing. Turn it on for at least one indexer.

### No indexers available with Interactive Search enabled

No enabled indexer has **Enable Interactive Search** turned on, so the manual search dialog shows
no results. Turn it on for at least one indexer.

### Invalid indexer download client setting

An indexer is set to send its releases to a specific download client, and that client no longer
exists or is disabled. Edit the indexer and pick a different client under **Download Client**, or
leave it blank to use any suitable client.

### Jackett all endpoint used

The indexer uses Jackett's aggregate `all` endpoint, which mixes results from every tracker and
breaks per-indexer settings and failure handling. Add each tracker as its own indexer instead, or
use Prowlarr to sync them.

## Download clients

### No download client is available

No download client is enabled, so nothing Readarr grabs can be downloaded. Add or enable one under
**Settings → Download Clients**.

### Unable to communicate with download client

Readarr couldn't reach a download client or log in to it. The message ends with the underlying
error. Check the host, port, URL base and credentials, and that the client is reachable from inside
the container: `localhost` there means the Readarr container itself, not your host machine.

### Download clients are unavailable due to failures

A download client failed repeatedly, so Readarr is backing off from it. It retries automatically.
Edit the client and press **Test** to see the current error.

### Completed download handling is disabled

Readarr doesn't import anything when downloads finish, so books never move into your library on
their own. Turn on **Completed Download Handling** in **Settings → Download Clients**.

### Download client removes completed downloads

The download client deletes downloads as soon as they finish, which can happen before Readarr has
imported them. Turn that option off in the download client, or let Readarr remove downloads after
import instead.

### Downloads in root folder

The download client saves into a folder that is also a Readarr root folder. Downloads and your
library must be separate: give the client its own download folder.

## Paths and imports

### Bad download client settings

A download client on the same machine reports a path that isn't valid for this operating system,
for example a Windows path while Readarr runs on Linux. Check the download folder in the client's
own settings.

### Bad remote path mapping

A download client reports a path that doesn't exist where Readarr runs. This is what
[remote path mappings](settings.md#remote-path-mappings) are for: they translate the client's path
into Readarr's. Add or correct a mapping so that the client's path maps to the same files as seen
by Readarr.

### Docker: bad remote path mapping

Same as [Bad remote path mapping](#bad-remote-path-mapping), in a container: the download client
reports a path that doesn't exist inside the Readarr container. Usually the two containers mount
the download folder at different paths. Mount it at the same path in both, or add a remote path
mapping. See [Paths and permissions in Docker](troubleshooting.md#paths-and-permissions-in-docker).

### Permissions error

Readarr can see the download folder or file but isn't allowed to read it. The user Readarr runs as
(`1000:1000` in the provided compose file) needs read access to downloads and write access to your
root folders. Fix the ownership or permissions on the host, or run the container as a user that
has them.

### Remote path: import failed

A download finished but Readarr couldn't import it. The log (**System → Logs**) has the reason,
most often a path, permission or naming problem. **Activity → Queue** shows the affected download.

### Remote path: file removed

A file disappeared while Readarr was processing it, usually because the download client or another
tool moved or deleted it. Make sure only Readarr moves completed downloads.

### Calibre root does not match

A root folder uses Calibre, and Calibre reports its library at a path Readarr doesn't recognise.
Calibre and Readarr must see the library at the same path, or you need a remote path mapping for
it.

## Library

### Missing root folder

A root folder configured in Readarr doesn't exist, or isn't mounted. Authors in it can't be scanned
or imported into. Check the volume mount, or remove the root folder in **Settings → Media
Management**.

### Import list missing root folder

An import list adds books to a root folder that no longer exists. Edit the list and choose an
existing root folder.

### Author path mounted read-only

A folder holding an author's books is on a read-only mount, so Readarr can't add, rename or
replace files there. Remount it read-write.

### Cannot write recycle bin

The recycling bin folder set in **Settings → Media Management** doesn't exist or isn't writable.
Create it, fix its permissions, or clear the setting.

## Lists and notifications

### Import lists are unavailable due to failures

An import list failed repeatedly, so Readarr is backing off from it. Edit the list and press
**Test** to see the current error.

### Notifications are unavailable due to failures

A connection under **Settings → Connect** failed repeatedly. Edit it and press **Test** to see the
current error.

## Database

### Database is missing columns

The database is missing columns this version of Readarr needs. Almost always this means it was
upgraded by a different Readarr fork, which used some of the same database upgrade numbers for
different changes, so this fork skipped them as already done. Pages and searches that use those
columns fail until it's fixed.

Readarr reports this rather than repairing it, because a skipped upgrade may have done more than
add columns. Restore a backup from before the database was used with the other fork, or open an
issue with the message and your logs. See also
[Migrating from upstream Readarr or another fork](troubleshooting.md#migrating-from-upstream-readarr-or-another-fork).

## Updates and system

### New update is available

A newer release exists. With Docker, update by pulling the new image and recreating the container,
for example `docker compose pull && docker compose up -d`. Release notes are under
**System → Updates**.

### Branch is not a valid release branch

The update branch isn't one Readarr recognises, so it can't check for updates. The Docker image
sets it to `develop` through `READARR__UPDATE__BRANCH`; remove any other branch you set.

### Cannot install update because startup folder is not writable by the user

Only applies to the built-in updater, which the Docker image doesn't use. If you see it, you are
probably running a build outside Docker: the user running Readarr needs write access to the
folder Readarr runs from.

### Cannot install update because UI folder is not writable by the user

As above, for the folder holding the web interface.

### Updating will not be possible to prevent deleting AppData on update

The data folder is inside the folder Readarr runs from, so an update would delete it. Doesn't
apply to the Docker image, which keeps data in `/config`.

### Package maintainer message

A message from whoever built the package you're running. Read it in **System → Status**.

### Invalid API key

Readarr's API key is too short. Generate a new one under **Settings → General → Security** and
update any apps that use it.

### System time off

The clock in the container is more than a day off, so scheduled tasks may not run correctly. The
container uses the host's clock: fix the time on the host.

### Proxy failed: resolve IP

The proxy host in **Settings → General → Proxy** doesn't resolve to an address. Check the
hostname.

### Proxy failed: test

Readarr couldn't make a request through the configured proxy. Check the proxy's address, port and
credentials.

## Web interface

### Could not connect to SignalR

The browser couldn't open the live-update connection to Readarr, so pages won't refresh on their
own. If you use a reverse proxy, it must pass WebSocket connections through (the `Upgrade` and
`Connection` headers). Reloading the page retries.
