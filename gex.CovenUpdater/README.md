# gex.CovenUpdater

companion to gex.Coven that can update gex.Coven based on tags from a git repo

by default, this pulls from the default git server,
but can changed using the following command line arguments:

| arg | desc | example |
| --- | --- | --- |
| `--Repository:Instance` | URL of the git server, including `https://`, but not a trailing `/` | `https://github.com` |
| `--Repository:RepositoryOwner` | The user that owns the repository | `user` |
| `--Repository:RepositoryName` | The repository that contains the tags used for version checking | `gex` |

the application expects to be in the same directory where the gex.Coven application is
