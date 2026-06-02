<!-- Remember to keep the README in src/ up to date with this one. -->

<p align="center">
	<img src=".github/assets/Header.png" alt="Elapsed banner" />
</p>

<p align="center">
	<a href="https://elapsed.hackclub.com">
		<img src=".github/assets/Source_Web.svg" alt="Find it on the Web" height="64" />
	</a>
	<a href="https://github.com/hackclub/elapsed/releases/latest">
		<img src=".github/assets/Source_GitHub.svg" alt="Get it from GitHub" height="64" />
	</a>
	<a href="https://nuget.org/packages/Riverside.Elapsed.CommandLine">
		<img src=".github/assets/Source_NuGet.svg" alt="Get it from NuGet" height="64" />
	</a>
</p>

---

Introducing **Elapsed**, a **high-performance**, **cross-platform**, **native app** for Hack Club's timelapse programme, [Lapse](https://lapse.hackclub.com), complete with a **modern UI** that is built from the ground up.
It is designed to feel **just like any other native app** on every platform it supports.
Built with .NET and Uno Platform, it **brings the Lapse experience** to desktop and mobile with a **responsive**, **dynamic interface**, **smooth navigation**, and a feature set tailored for **real timelapse workflows** rather than a direct web clone.

Beyond the app itself, Elapsed also includes a **powerful command-line interface** for **power-users** and **automation**.
The CLI exposes the **full Lapse API** surface, returns **clean JSON output**, and makes it **easy to script** or inspect requests when **building tools**, **testing integrations**, or **debugging workflows** with Lapse.

---

## Using Elapsed

### Building from source

> These are the instructions for building the Uno Platform project (main app).
> For other projects, you can build as normal with the latest .NET SDK, without the prerequisites listed below.

#### 1. Prerequisites

- .NET 10 SDK or later
- [`Uno.Check`](https://platform.uno/docs/articles/uno-check.html)
- Git
- For Android builds:
	- Android SDK and platform tools (Android Studio)
	- JDK 17+
- For iOS builds:
	- Xcode (on macOS)

#### 2. Set up IDE

> Using Visual Studio 2026 is recommended for Elapsed development.
> Otherwise, you might see issues with the .NET SDK.

##### Visual Studio

- Microsoft Visual Studio with .NET
- Workloads:
	- ".NET desktop development" (desktop target, including all platform SDKs)
	- ".NET multi-platform app UI development"
	- "WinUI application development"
- Uno Platform extension

##### Rider

- JetBrains Rider with .NET
- Android SDK/JDK (and Xcode SDK on macOS) are configured correctly in Rider settings (for mobile targets)

##### Visual Studio Code

- Microsoft Visual Studio Code
- C# Dev Kit extension
- Uno Platform extension

#### 3. Run `Uno.Check`

> *This step is optional, but is good practice to check your installed all the necessary dependencies to build Elapsed on your computer.*

Run the following command and follow all of its instructions (you need to have `Uno.Check` installed!)

```bash
uno-check
```

See the [official `Uno.Check` guide](https://platform.uno/docs/articles/uno-check.html) for tips.

#### 4. Clone the repository

> *Click the "Code" button on the top of this page to make cloning easier.*

```bash
git clone https://github.com/hackclub/elapsed
cd Elapsed
```

#### 5. Build the project

##### Visual Studio / Rider

- Open the solution `Elapsed.slnx`
- Set `Riverside.Elapsed.App.csproj` as the startup project
- Select the appropriate target platform (Desktop, Android, iOS)
- Run with debugger

##### Visual Studio Code

- Open the `Elapsed` folder
- Navigate to the debug pane in the left-side activity bar
- Run the appropriate debug profile for the platform you want to build for (or press <kbd>F5</kbd> anywhere in VS Code)

##### CLI

Building Elapsed with the MSBuild CLI is a great way to quickly test whether the app builds, as the MSBuild CLI does not rely on GUI-heavy apps such as Visual Studio.
It is recommended to use the .NET Framework version of MSBuild as it works best for the tech stack that the Elapsed project consumes.

- Restore the project dependencies via NuGet

```bash
msbuild -t:Restore -p:Configuration=Release
```

- Build/publish the project of your choice, such as `Riverside.Elapsed.App` for the main app project.

You must specify a `TargetFramework` property when building the app project, such as `net10.0-desktop`, `net10.0-windows`, or `net10.0-browserwasm`.
When building the Windows head of the app project, it is recommended to debug via an IDE such as Visual Studio as an IDE will provide a better experience for building WinUI apps.
It is also recommended to make use of the `Publish` task when building the WebAssembly/WinAppSDK/Skia heads as this will ensure the output is production ready.

```bash
msbuild src\platforms\Riverside.Elapsed.App -t:Publish -p:Configuration=Release -p:TargetFramework=net10.0-desktop
```

The build output can always be found in the `bin` folder of the project root directory.

<!--
### How to use Elapsed

{0}
-->

### Elapsed CLI

> While the main Elapsed cross-platform app was created for [Hack Club: The Game](https://game.hackclub.com), the Elapsed CLI was created for the [Campfire Flagship](https://flagship.hackclub.com) programme.
> With that said, the Elapsed CLI is mostly feature complete, and while it will receive updates for any breaking changes to the Lapse API projection, there are no new features planned.

Besides the main cross-platform app, Elapsed also comes with a powerful CLI that supports all interactions with the Lapse API projection.
It can be invoked with the command `elapsed`, and supports every platform that .NET supports (it is a .NET Core app).

It is a clean, modern CLI that uses the [`System.CommandLine`](https://github.com/dotnet/command-line-api) library and supports all documented features of the Lapse API.
It returns API responses as pretty JSON, serialised from the Lapse API projection.
Appending `-?`, `-h`, `--help` at the end of any command will show an explanation of available commands, or the current command.
If you want to see all the HTTP requests the Elapsed CLI (and, by extension, the Lapse API projection) can make, run the command `elapsed list-operations`.

You can download the Elapsed CLI [from GitHub Releases](https://github.com/hackclub/elapsed/releases/latest), or build the `Riverside.Elapsed.CommandLine` project from source.
Optionally, you can install the Elapsed CLI [as a .NET tool](https://nuget.org/packages/Riverside.Elapsed.CommandLine) from NuGet.

<img width="620" height="375" alt="Elapsed CLI" src="https://github.com/user-attachments/assets/f435c52b-7800-45f0-8d6c-37e418cc22e5" />

### Elapsed API

The Lapse API projection is a .NET Standard library that uses the [Kiota generator](https://github.com/Lamparter/CompilerPlatform/tree/main/src/features/Riverside.CompilerPlatform.Features.Kiota) from the "Advanced Compiler Services for .NET" library.
The Kiota generator emits clean C# code based on the [Lapse API documentation](https://api.lapse.hackclub.com/docs) every time the core MSBuild pipeline is activated.

> *Microsoft Kiota is the core backend that powers the Lapse API projection generator.*

Using the client in the Lapse API is incredibly simple - you must pass a request adapter instance (`IRequestAdapter`) into the API client class constructor (`Riverside.Elapsed.ApiClient`).
The API client class is an abstraction over HTTP requests that is specific to the Lapse API.

If you are unsure how to consume the API client class, the [Elapsed CLI project](https://github.com/hackclub/elapsed/tree/main/src/platforms/Riverside.Elapsed.CommandLine) is an excellent example implementation.
It is recommended to have a good understanding of [how Microsoft Kiota works](https://learn.microsoft.com/en-us/openapi/kiota/design) before using the Elapsed API projection.

### Supported platforms

> [!NOTE]
> The purpose of Elapsed is to create a modern, high-performance, native timelapse app that supports all major platforms with a consistent feature set.
> It can run on a huge variety of devices (as supported by Uno Platform and the .NET Runtime).
> *See the "design choices" section for more info.*

- Web: Elapsed is available for use on the web, just like the classic Lapse client.
- Android and VR: Elapsed supports all modern Android phones.
- iOS and iPadOS: Elapsed supports iOS and iPadOS.
- Mac and Linux: Elapsed provides a fully integrated UX for Linux (Wayland/X11) and macOS.
- Windows: Elapsed runs as a self-contained Windows App SDK app on Windows.

<!-- add images of Elapsed on different platforms when ready -->

## Contributing

> [!IMPORTANT]
> The Lapse API projection for .NET is generated automatically on build based on the public Lapse API.
> If you are looking for the code for the Lapse API projection, it is a part of the [Advanced Compiler Services for .NET](https://github.com/Lamparter/CompilerPlatform) repo, specifically the [`Riverside.CompilerPlatform.CSharp.Features.Kiota`](https://github.com/Lamparter/CompilerPlatform/tree/main/src/features/Riverside.CompilerPlatform.Features.Kiota) library.
> If you have found a bug with the Lapse API or want to improve API documentation, please make an issue or PR as appropriate on the [main Lapse repo](https://github.com/hackclub/lapse).
> If you're ever unsure of where to report an issue, ask in the [`#lapse-help`](https://hackclub.enterprise.slack.com/archives/C09NVLWU61E) channel on Slack!

**Contributions are welcome** - please feel free to add **missing features**, **new styles**, or **fix bugs**.
And of course, please open as many issues or pull requests as you like! *All contributions are helpful in their own way.*

### Project structure

```r
Elapsed/
│
├── eng/                                  # Build pipelines and versioning
├── src/                                  # Source code
│  ├──platforms/                          # Platform-related source code
│  │  ├──Riverside.Elapsed.App/           # The main cross-platform app entrypoint
│  │  └──Riverside.Elapsed.CommandLine/   # The Elapsed CLI programme
│  ├──core/                               # Shared source code used by multiple projects
│  │  └──Riverside.Elapsed/               # The project head for the Lapse API projection
│  └──README.md
├── tests/                                # API and UI tests
├── Directory.Build.props
├── Directory.Packages.props
├── Elapsed.slnx
├── LICENSE
└── README.md
```

### Project design choices

As an **open-source project**, Elapsed is **developed in the open** by the Hack Club community and contributors from related projects.
The **goal is simple**: deliver a **modern**, **high-performance** timelapse experience that is **accessible**, **transparent**, and **continuously evolving** across Windows, Mac, Linux, Android, iOS and more!

#### Code style

> [!TIP]
> For consistency, I always prefer British English in documentation, however, to keep in line with community practices, I use American English spellings in .NET code.
> You might see `Analyzer` in the source, but I will always write `Analyser` in documentation (including in-code comments and XMLDoc specs).
> All contributors are encouraged to follow this custom and can be expected to have their code modified if not.

The code style convention of Elapsed is generally not very strict, but these are a must:
- always use tabs for indentation
- use file-scoped namespaces

If you want a more detailed understanding of my C# coding conventions, see the [example code style document I made](https://gist.github.com/Lamparter/512ed5f2bdd4174376eb7fbe4460c2b2).

I also recommend formatting issues & PR names [like how the Files project does](https://github.com/files-community/Files/commits/main) with a capitalised prefix such as "Code Quality:", "Fix:", "Feature:", "Docs:", "Bug:" etc. followed by a concise yet descriptive title.
Commit names should also follow the same convention but do not need a prefix. For a good example of this, take a look at any of the commit names for Git commits I've already made in the repo.

#### Versioning practices

Elapsed uses a custom versioning system that is unlike the generic Semantic Versioning spec.
Versions follow the format `{MAJOR}.{MINOR}.{DATE}[-{LEVEL}{PRERELEASE}]`, where:
- `MAJOR` is the major release version
- `MINOR` is the minor release version
- `DATE` is the build date in yymmdd format, e.g. `260320`
- `LEVEL` is the release level (`preview` if a preview channel, or none if else)
- `PRERELEASE` is the version of the prerelease (this is automatically updated)

For example, a version could look like this: `2.2.260220`, or `2.5.260522-preview2`.

The Elapsed project contains two build configurations, <kbd>Release</kbd> and <kbd>Debug</kbd>.
Each build configuration has slightly different settings, and feature flags within the app may be dependent on which build configuration is enabled.
Generally, you should always build the 'Debug' configuration for development purposes, as it enables extra feature flags for debugging.

Releases are automatically published to GitHub and other marketplaces (as listed below) by the CD workflow.
The release process can be induced by updating the `eng/CurrentVersion.props` with a new version number, which activates the CD pipeline.

The CD workflow deploys to the following marketplaces:
- GitHub Releases
- NuGet
- Web
- ~~Microsoft Store~~
- ~~Google Play Store~~
- ~~Apple App Store~~

#### Trimming and native AOT compilation

> [!NOTE]
> Trimming and Native AOT compilation only occurs when the 'release' configuration of any project is built.

The Elapsed API projection is a .NET Standard 2.0 class library and executable projects are .NET 10 binaries; all *should* support Native AOT and trimming out of the box.

The Elapsed CLI programme uses illegal methods (Reflection) that are not AOT or trimming safe.
Therefore, the CLI does not ship with the desktop programme, which is a Native AOT (ILLink) app and uses trimming.

#### UI design

Elapsed uses Microsoft's WinUI design system (a part of Uno).
Elapsed follows all rules of the Microsoft design language very strictly, inspired by various apps that use this same design system (all of which I have contributed do), such as:
- [FluentHub](https://github.com/0x5BFA/FluentHub)
- [Files](https://github.com/files-community/Files)
- [Rise Media Player](https://github.com/theimpactfulcompany/Rise-Media-Player)
- [Rebound 11](https://github.com/IviriusCommunity/Rebound)

and many others!

> Elapsed's primitive design mock-ups are available for public browsing [on Figma](https://www.figma.com/design/dUoOj27yGtoY3Y6HRKYJnF/Elapsed--WinUI-3-).

It is recommended to use [Uno Platform Studio](https://platform.uno/hot-design) (although this is a paid tool) as it makes designing cross-platform UI so much easier without having devices on-hand.

<!--
### Differences from the Lapse website

> [!NOTE]
> Elapsed is an app built from the ground up, using the Lapse API projection for .NET.
> It differs in features from the [Lapse website](https://lapse.hackclub.com).
> This is a comprehensive list of the various parts of the app that differ from the web version.

{0}

-->

### Dependencies

Elapsed is made possible by work from [open source contributors](https://github.com/hackclub/elapsed/graphs/contributors), [contributors to the Lapse project](https://github.com/hackclub/lapse/graphs/contributors), and the lead developers [`@Lamparter`](https://github.com/Lamparter) and [`@ascpixi`](https://github.com/ascpixi).
Elapse also relies on the following open source projects to function:

- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
- [Uno Platform](https://github.com/unoplatform/uno)
- [Lapse](https://github.com/hackclub/lapse)'s API
- [Hackatime](https://github.com/hackclub/hackatime)'s API
- [Advanced Compiler Services for .NET](https://github.com/Lamparter/CompilerPlatform)
- [Win32 P/Invoke bindings for .NET Standard](https://github.com/Lamparter/Win32)
- [.NET Command Line APIs](https://github.com/dotnet/command-line-api)
- [Skia](https://github.com/google/skia) and [SkiaSharp](https://github.com/mono/SkiaSharp)
- [Sentry](https://github.com/getsentry/sentry-dotnet)

## License

This project is **free, open source software** licensed under the MIT License.

Elapsed is a Hack Club project, made by teenagers, for teenagers.
There are no plans to make Elapsed a paid app.

As the HQ sponsor, `@ascpixi` is reponsible for the Lapse project. Please contact him for legal questions.

---

<p align="center">
	<img src=".github/assets/Showcase.png" alt="Elapsed banner" />
</p>

<p align="center">
	<sub>Made with &lt;3 by teenagers, for teenagers. <a href="https://hackclub.com">Learn more about Hack Club.</a></sub>
</p>
