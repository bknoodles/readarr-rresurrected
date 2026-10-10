# Settings

Notes on the settings the web interface links here. Most settings explain themselves in the help
text next to them; this page covers the ones that need more context.

Questions? Ask on [GitHub Issues](https://github.com/ricetim/readarr-rresurrected/issues).

## How Readarr finds releases

Readarr finds releases in two ways:

- **RSS sync** checks each indexer's feed of newly posted releases on a schedule (**Settings →
  Indexers → Options → RSS Sync Interval**) and grabs anything that matches a monitored book you
  don't have yet, or that upgrades one you do. This is how new releases are picked up without you
  doing anything.
- **Searches** ask indexers for a specific book. Automatic searches run when you add a book or
  author with searching enabled, or when you press **Search**. Interactive searches show you the
  results so you can pick one.

Each indexer has separate switches for RSS, automatic search and interactive search. Keep the RSS
interval reasonable: indexers set limits on how often you may poll them, and breaking those limits
can get an account banned.

## Metadata source

Book, author and series data comes from the metadata service bundled in the container, which
listens on port 28202. To use a different server, set the `READARR_METADATA_URL` environment
variable on the container, keeping the `{route}` placeholder, for example
`http://rreading-glasses:8788/{route}`. There is no setting for this in the web interface.

## Updates

The Docker image updates by pulling a new image, not from inside Readarr, so the update settings
here have no effect in Docker. Pull `ricetim/readarr-rresurrected:latest` and recreate the
container to update; see [New update is available](health-checks.md#new-update-is-available).

## Authentication

Readarr asks for a username and password on a login page. **Authentication Required** decides
when: **Enabled** always asks, and **Disabled for Local Addresses** skips the login for requests
from your local network. That's only safe if Readarr isn't reachable from the internet and you
trust every device on your network. Never expose Readarr to the internet without authentication.

The browser's built-in login prompt (Basic authentication) was removed in 11.0; existing
configurations switch to the login page automatically.

## Custom formats

Custom formats score releases by patterns in their title or other properties, for example
preferring a particular narrator or penalising a release group. Quality profiles then use those
scores to decide what to grab and upgrade. Each condition can be **Required** (must match for the
format to apply) or **Negate** (must not match). The **Release Title** condition takes a regular
expression.

## Tags

Tags link authors to the indexers, download clients, connections, release profiles and delay
profiles that should apply to them. An item with a tag only applies to authors with the same tag;
an item without tags applies to everyone.

## Remote path mappings

A download client sometimes reports a path that doesn't exist where Readarr runs, typically
because the client runs in another container or on another machine. A remote path mapping tells
Readarr how to translate it: if the client says `/downloads/complete` and Readarr sees the same
files at `/data/downloads/complete`, map the first to the second.

If both containers can mount the downloads at the same path, do that instead and skip the mapping.
See [Paths and permissions in Docker](troubleshooting.md#paths-and-permissions-in-docker).

## Connections

Connections notify other services when something happens in Readarr, such as a book being grabbed,
imported or upgraded. Choose which events each connection reacts to. Two are general purpose:

- **Webhook** sends a JSON description of each event to a URL you choose.
- **Custom Script** runs a script inside the container for each event, with details passed in
  environment variables whose names start with `Readarr_` (the capital R matters on Linux).

The rest are listed under [Supported integrations](supported.md#notifications).

## Write metadata to book files

Applies only to books in a root folder backed by a Calibre library. Readarr updates each book's
record in Calibre with its metadata; with **Embed Metadata** on, Calibre also writes it into the
ebook file itself, and **Update Covers** sets Calibre's cover to Readarr's. Choose whether to do
this for new files only, for all files, or for all files kept in sync when the metadata changes
later. Writing into files changes them, so tools that match files by checksum will see them as
different. Books outside a Calibre library are not touched.

## Write metadata to audio files

Readarr writes tags into audiobook files: the title, book title, author, track and disc numbers,
genres, publisher and cover art. Choose not to write tags, to write them for new files only, for
all files, or for all files kept in sync when the metadata changes later. Tags are written with
the TagLib library, which supports the common formats (MP3, M4B/M4A, FLAC).
