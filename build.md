# Build SixPix.NET

## Basic

```sh
dotnet build -c Debug ./src
```

## use ImageSharp v4 alpha

To use the ImageSharp 4 alpha version, you will need to add the source to nuget settings.

```console
$ cd path/to/project_dir
$ dotnet nuget add source https://f.feedz.io/sixlabors/sixlabors/nuget/index.json
Package source with Name: Package source 1 added successfully.

$ cat nuget.config
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="Package source 1" value="https://f.feedz.io/sixlabors/sixlabors/nuget/index.json" />
  </packageSources>
</configuration>
```

And build with a property `UseImsageSharp4=true`

```sh
dotnet build -c Debug -p:"UseImsageSharp4=true" ./src
```

