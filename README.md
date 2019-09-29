# Murg

Murg is a console tool to simplify the process of reorganizing downloaded music. It uses info like file name, count of tracks in a directory to find [Discogs](https://www.discogs.com/) info. It renames a directory and tracks. It sets tags too.

## Notice

Murg is a prototype. I recommend to run it with `--dry` option first.

## How to use it

Use need [.NET Core 3 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) to be installed. In the future I'll provide compiled binaries that do not require .NET Core at all.

You can run it like

```bash
dotnet run --project ../dev/Murg/src/Murg/Murg.csproj --dry "攻殻機動隊 STAND ALONE COMPLEX O.S.T"
```

There is `--recursive` option if you want to process subdirectories.
