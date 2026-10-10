# Supported integrations

Everything Readarr can connect to, with notes where something needs explaining. The **More info**
link next to each integration in Readarr's settings opens its section here.

Missing something, or something here doesn't work? Ask on
[GitHub Issues](https://github.com/ricetim/readarr-rresurrected/issues).

## Indexers

Indexers are where Readarr searches for releases. Most people add them through
[Prowlarr](https://prowlarr.com), which syncs indexers to Readarr as Newznab and Torznab entries,
but they can also be added directly.

### Bibliotik

Built into this fork. Bibliotik has no API, so Readarr signs in with your browser's session
cookie: log in to Bibliotik, then copy the value of the `id` cookie into **Session Cookie**. The
field's help text explains where to find it. If the cookie expires, searches fail with an
authentication error until you paste a fresh one.

### FileList

Torrent tracker, using your FileList username and passkey.

### Gazelle API

Trackers running the Gazelle software, signing in with your username and password.

### IP Torrents

Torrent tracker, using the RSS feed URL from your IPTorrents account.

### MyAnonamouse

Built into this fork. Readarr signs in with your `mam_id` session cookie: log in to MyAnonamouse,
then copy the value of the `mam_id` cookie into **Session Cookie**. MyAnonamouse can tie that
cookie to the IP address it was created from, so create it for the address Readarr connects from.
**Search Type** chooses which torrents to include: only those with seeders, only those without, or
all of them.

### Newznab

Usenet indexers that speak the Newznab API, including indexers synced from Prowlarr. If your
indexer or Prowlarr is set to redirect downloads, that's fine: Readarr follows the redirect.

### Nyaa

The Nyaa torrent site.

### Torrent RSS Feed

Any torrent RSS feed. RSS-only: it can't be searched, so it only finds new releases as they're
posted.

### TorrentLeech

Torrent tracker, using the API key from your TorrentLeech account.

### Torznab

Torrent indexers that speak the Torznab API, including indexers synced from Prowlarr or Jackett.
Add each tracker separately rather than through Jackett's `all` endpoint; see
[Jackett all endpoint used](health-checks.md#jackett-all-endpoint-used).

## Download clients

### Aria2

Torrent client, controlled through its RPC interface.

### Deluge

Torrent client, through the Deluge Web UI (not the daemon port).

### Download Station

Synology's download manager. It appears twice in the list, once for torrents and once for
Usenet; set up whichever matches the indexers that will use it.

### Flood

The Flood web interface for rTorrent, qBittorrent or Transmission.

### Hadouken

Torrent client.

### NZBGet

Usenet client.

### NZBVortex

Usenet client.

### Pneumatic

Saves NZB files for Kodi's Pneumatic add-on to download.

### qBittorrent

Torrent client, through the qBittorrent Web UI.

### rTorrent

Torrent client, through its XML-RPC interface (often behind ruTorrent).

### SABnzbd

Usenet client.

### Torrent Blackhole

Drops `.torrent` files into a folder for another program to pick up, and watches a second folder
for finished downloads to import.

### Transmission

Torrent client.

### Usenet Blackhole

Drops `.nzb` files into a folder for another program to pick up, and watches a second folder for
finished downloads to import.

### uTorrent

Torrent client, through the uTorrent Web UI.

### Vuze

Torrent client, through its Transmission-compatible interface.

## Import lists

Import lists add authors and books to Readarr automatically.

### Goodreads List

Adds the books on a public Goodreads list (Listopia), by the list's ID: the number in its address,
for example `1` in `goodreads.com/list/show/1.Best_Books_Ever`. It reads Goodreads' legacy XML API,
which Goodreads could switch off at any time.

### Goodreads Series

Adds the books in a Goodreads series, by the series' ID: the number in its address. It uses the
same legacy API as Goodreads List.

The Goodreads **Bookshelves** and **Owned Books** lists were removed: they signed in to Goodreads
through the original Readarr project's servers, for an API Goodreads has closed. Existing ones are
removed automatically.

### LazyLibrarian

Imports wanted books from a LazyLibrarian instance.

### Readarr

Imports authors from another Readarr instance, optionally limited to certain profiles or tags.

## Notifications

Connections tell other services when something happens in Readarr. See also
[Connections](settings.md#connections).

### Apprise

Sends notifications through an Apprise server, which can forward to many other services.

### Custom Script

Runs a script for each event. See [Connections](settings.md#connections).

### Discord

Posts to a Discord channel through a webhook URL.

### Email

Sends email through any SMTP server.

### Gotify

Sends notifications to a Gotify server.

### Join

Sends notifications to devices through Join.

### Kavita

Tells a Kavita server to rescan its library after an import.

### Mailgun

Sends email through Mailgun.

### Notifiarr

Sends events to Notifiarr.

### ntfy.sh

Sends notifications through ntfy, either ntfy.sh or your own server.

### Plex Media Server

Tells a Plex server to update its library after an import.

### Prowl

Sends notifications to iOS devices through Prowl.

### Pushbullet

Sends notifications through Pushbullet.

### Pushover

Sends notifications through Pushover.

### SendGrid

Sends email through SendGrid.

### Signal

Sends Signal messages through a signal-cli REST API server.

### Simplepush

Sends notifications through Simplepush.

### Slack

Posts to a Slack channel through a webhook URL.

### Subsonic

Tells a Subsonic-compatible server to rescan its library after an import.

### Synology Indexer

Tells Synology's media indexer about new and removed files. It uses Synology's `synoindex` tool,
which only exists on the NAS itself, so it does nothing in the Docker image.

### Telegram

Sends messages through a Telegram bot.

### Twitter

Posts to X (formerly Twitter) using your own developer app's consumer key and secret. X restricts
API access, so this may need a paid developer account.

### Webhook

Sends a JSON description of each event to a URL. See [Connections](settings.md#connections).
