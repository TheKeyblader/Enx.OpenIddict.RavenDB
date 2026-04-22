# Enx.OpenIddict.RavenDB

RavenDB storage provider for [OpenIddict](https://github.com/openiddict/openiddict-core) — the flexible, standards-compliant OpenID Connect/OAuth 2.0 server framework for ASP.NET Core.

[![NuGet](https://img.shields.io/nuget/v/Enx.OpenIddict.RavenDB.svg)](https://www.nuget.org/packages/Enx.OpenIddict.RavenDB)
[![CI](https://github.com/TheKeyblader/Enx.OpenIddict.RavenDB/actions/workflows/dotnet.yml/badge.svg)](https://github.com/TheKeyblader/Enx.OpenIddict.RavenDB/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/TheKeyblader/Enx.OpenIddict.RavenDB/graph/badge.svg?token=3EKSCB86VR)](https://codecov.io/gh/TheKeyblader/Enx.OpenIddict.RavenDB)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Overview

`Enx.OpenIddict.RavenDB` implements all four OpenIddict core store interfaces backed by RavenDB, enabling you to use a document-oriented database as the persistence layer for your OpenID Connect / OAuth 2.0 server.

| OpenIddict entity | Store implemented |
|---|---|
| Application | `OpenIddictRavenDBApplicationStore<T>` |
| Authorization | `OpenIddictRavenDBAuthorizationStore<T>` |
| Scope | `OpenIddictRavenDBScopeStore<T>` |
| Token | `OpenIddictRavenDBTokenStore<T>` |

## Requirements

- .NET Standard 2.0 / 2.1, .NET 8, or .NET 10
- OpenIddict 7.x
- RavenDB.Client 7.x

## Installation

```shell
dotnet add package Enx.OpenIddict.RavenDB
```

The models package is included transitively. If you need to reference the entity types directly in a separate project, add:

```shell
dotnet add package Enx.OpenIddict.RavenDB.Models
```

## Getting started

### 1. Configure RavenDB

Register your RavenDB `IDocumentStore` as usual before configuring OpenIddict:

```csharp
builder.Services.AddSingleton<IDocumentStore>(provider =>
{
    var store = new DocumentStore
    {
        Urls = ["http://localhost:8080"],
        Database = "MyApp"
    };
    store.Initialize();
    return store;
});

// Register a scoped async session used by the OpenIddict stores
builder.Services.AddScoped(provider =>
    provider.GetRequiredService<IDocumentStore>().OpenAsyncSession());
```

### 2. Register OpenIddict with the RavenDB provider

```csharp
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseRavenDB();
    })
    .AddServer(options =>
    {
        // ... your server configuration
    });
```

### 3. Deploy RavenDB indexes

The library ships with custom RavenDB indexes for efficient querying. Deploy them once at startup:

```csharp
var store = app.Services.GetRequiredService<IDocumentStore>();
await IndexCreation.CreateIndexesAsync(
    typeof(OpenIddictRavenDBApplication).Assembly, store);
```

## Custom entities

All entity types are extensible. Inherit from the base model, then tell OpenIddict to use your custom type:

```csharp
public class MyApplication : OpenIddictRavenDBApplication
{
    public string? Tenant { get; set; }
}
```

```csharp
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseRavenDB(ravendb =>
        {
            ravendb.ReplaceDefaultApplicationEntity<MyApplication>();
        });
    });
```

The same pattern applies to `ReplaceDefaultAuthorizationEntity<T>`, `ReplaceDefaultScopeEntity<T>`, and `ReplaceDefaultTokenEntity<T>`.

## Entity models

| Model | Key properties |
|---|---|
| `OpenIddictRavenDBApplication` | ClientId, ClientSecret, DisplayName, RedirectUris, Permissions, Requirements |
| `OpenIddictRavenDBAuthorization` | ApplicationId, Subject, Status, Scopes, CreationDate |
| `OpenIddictRavenDBScope` | Name, DisplayName, Description, Resources |
| `OpenIddictRavenDBToken` | ApplicationId, AuthorizationId, Subject, Type, Status, ExpirationDate, Payload |

## Contributing

Contributions are welcome. Please open an issue before submitting a pull request for significant changes.

## License

This project is licensed under the [MIT License](LICENSE). Copyright © 2026 Jean-François Pustay.
