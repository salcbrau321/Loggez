# Loggez

[![Pre-release](https://img.shields.io/github/v/release/YourUserName/Loggez?label=pre-release&sort=semver)](https://github.com/YourUserName/Loggez/releases)

A simple Avalonia-based log file viewer powered by Lucene.NET for substring searches.

> ⚠️ **Alpha Pre-release**: This is an early demo build. Core functionality currently covers basic substring search only. File management, progress UI, and additional features are planned.

## Features (alpha)

- **Basic substring search**: Type any text to trigger real-time search.
- **Date range filtering**: Pick a start and end date.
- **Non-blocking UI**: Indexing runs asynchronously (progress dialog forthcoming).

---

## Getting Started

### Prerequisites

- **.NET 8 SDK**
- **Visual Studio 2022** / **Rider** / **VS Code**

### Build & Run

```bash
git clone https://github.com/YourUserName/Loggez.git
cd Loggez
dotnet restore
dotnet build
dotnet run --project Loggez.UI
```

## Using the App

1. **Load logs**: Click **File**, then **Upload…** and select option for uploading logs (Folder, Files, or Zip).
2. **Wait for indexing**: The app will automatically index the files (progress dialog forthcoming in later versions).
3. **Enter search**: Type your substring into the search box.
4. **Toggle case sensitivity**: Use the **Case sensitive** checkbox if needed.
7. **Review results**: Matching lines appear grouped by file; click a line to preview its context.

---

## License

Apache License 2.0. See [LICENSE](LICENSE) for details.
