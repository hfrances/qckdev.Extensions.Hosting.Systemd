[![NuGet Version](https://img.shields.io/nuget/v/qckdev.Extensions.Hosting.Systemd.svg)](https://www.nuget.org/packages/qckdev.Extensions.Hosting.Systemd)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=qckdev.Extensions.Hosting.Systemd&metric=alert_status)](https://sonarcloud.io/dashboard?id=qckdev.Extensions.Hosting.Systemd)
[![Code Coverage](https://sonarcloud.io/api/project_badges/measure?project=qckdev.Extensions.Hosting.Systemd&metric=coverage)](https://sonarcloud.io/dashboard?id=qckdev.Extensions.Hosting.Systemd)
![Azure Pipelines Status](https://hfrances.visualstudio.com/qckdev/_apis/build/status/qckdev.Extensions.Hosting.Systemd?branchName=master)

# qckdev.Extensions.Hosting.Systemd

Compatibility package for `Microsoft.Extensions.Hosting.Systemd`.

This package centralizes framework-specific references so consumer projects do not need conditional `PackageReference` blocks per target framework.

## Installation

```bash
dotnet add package qckdev.Extensions.Hosting.Systemd
```

## Supported target frameworks

- `netcoreapp3.1` -> `Microsoft.Extensions.Hosting.Systemd` `3.1.32`
- `net5.0` -> `Microsoft.Extensions.Hosting.Systemd` `5.0.1`
- `net6.0` -> `Microsoft.Extensions.Hosting.Systemd` `6.0.1`
- `net8.0` -> `Microsoft.Extensions.Hosting.Systemd` `8.0.1`
- `net10.0` -> `Microsoft.Extensions.Hosting.Systemd` `10.0.3`

## Usage

Add the package reference in your service project and keep your host configuration in your own application code.

```xml
<ItemGroup>
  <PackageReference Include="qckdev.Extensions.Hosting.Systemd" Version="0.1.0" />
</ItemGroup>
```

## 🤝 Contributing
Issues and pull requests are welcome! See the contribution guidelines (coming soon).

## 📜 License
This project is licensed under the terms of the [MIT License](LICENSE).
