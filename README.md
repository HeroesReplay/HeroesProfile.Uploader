# Heroes Profile Uploader

[![Join Discord Chat](https://img.shields.io/discord/650747275886198815?label=Discord&logo=discord)](https://discord.gg/cADfdFP)

Uploads Heroes of the Storm replays to [https://heroesprofile.com](https://www.heroesprofile.com/).

## Installation

* Requires .NET 8 Desktop Runtime

## Contributing

Coding conventions are as usual for C# except braces, those are in egyptian style ([OTBS](https://en.wikipedia.org/wiki/Indent_style#1TBS)). For repos included as submodules their coding style is used.

All logic is contained in `Heroesprofile.Uploader.Common` to make UI project as thin as possible. `Heroesprofile.Uploader.Windows` is responsible for only OS-specific tasks such as auto update, tray icon, autorun, file locations.

For the current to do list look in the [Project](https://github.com/Heroes-Profile/HeroesProfile.Uploader/projects/1) page
