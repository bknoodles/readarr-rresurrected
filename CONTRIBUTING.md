# Contributing

Thanks for helping. Bug reports, questions and feature ideas go to
[GitHub Issues](https://github.com/ricetim/readarr-rresurrected/issues).

## Pull requests

1. Open an issue first for anything larger than a small fix, so we can agree on the approach.
2. Branch from `develop` and keep each pull request to one change.
3. Run the tests for what you touched, and add tests for new behaviour:

   ```bash
   # Backend (build from the solution, then test without rebuilding)
   dotnet build src/Readarr.sln -p:Configuration=Debug -p:Platform=Posix --no-restore
   dotnet test src/NzbDrone.Core.Test/Readarr.Core.Test.csproj -p:Platform=Posix --no-build

   # Metadata service
   cd bookinfo && pytest

   # Frontend
   yarn lint
   ```

4. If you change behaviour users see, add an entry under `## [Unreleased]` in `CHANGELOG.md`,
   written for users rather than as a commit message, and update the documentation in `docs/`.
5. If you rename a documentation heading, a health check or an integration, run
   `python3 .github/scripts/check_docs_anchors.py`: the web interface links to those headings.

The project is licensed under the [GNU GPL v3](LICENSE.md), and contributions are accepted under
the same license.
