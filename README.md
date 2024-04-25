# Xbox Achievement Unlocker
![GitHub contributors][contributors-badge]
![GitHub forks][forks-badge]
![GitHub stars][stars-badge]
![GitHub issues][issues-badge]
![GitHub release][release-badge]

[Join our Discord][discord-invite]

Unlock achievements on Microsoft/Xbox games with ease. This tool is inspired by the functionality of Steam Achievements Manager and is completely free to use.

## Table of Contents
- [Xbox Achievement Unlocker](#xbox-achievement-unlocker)
  - [Table of Contents](#table-of-contents)
  - [About Xbox Achievement Unlocker](#about-xbox-achievement-unlocker)
  - [How It Works](#how-it-works)
  - [Requirements](#requirements)
  - [Features](#features)
  - [Screenshots](#screenshots)
  - [Future Improvements](#future-improvements)
  - [Join Our Discord Server](#join-our-discord-server)
  - [License](#license)

## About Xbox Achievement Unlocker
There are numerous paid services offering tools or services to unlock a full game's achievements on your account. Xbox Achievement Unlocker is a free alternative that doesn't randomly add gamerscore from arbitrary games or charge you to unlock a game's achievements.

## How It Works
Xbox Achievement Unlocker uses code from memory.dll to extract the user's XAuth token from one of the Xbox app processes. This token is then used to make web requests to Xbox servers, pulling information on achievements and informing the server which of these achievements have been unlocked.

## Requirements
- [dotnet 7](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.11-windows-x64-installer)
- [New Xbox app](https://apps.microsoft.com/store/detail/xbox/9MV0B5HZVK9Z)

## Features
- Extract XAuth from Xbox app
- Obtain a list of games from the user or any selected XUID
- Unlock Achievements for any Title Managed game
- Spoof time in any game
- Automatic updates
- Automatically spoof time in the currently viewed game
- Search TA and use the Xbox API to get the titleID for any game with a store page

## Screenshots
Coming soon.

## Future Improvements
- Stats editor
- Support for Event based stats
- Support for Event based achievements
- In-app login

## Join Our Discord Server
Feel free to join our [Discord server][discord-invite] for updates and discussions.

## License
The UI for this program was built on top of the WPF-UI Fluent template as of [this commit](https://github.com/lepoco/wpfui/tree/c8cd75f6f82414a52a94d2a55fe2a21dd5db83d7) which is MIT licensed. Any and all modifications and/or additions to this template are GNU GPL licensed. You can find a copy of the licenses [here][LICENSE] and [here][MIT-LICENSE].

## Sponsors
Thanks very much to all of my sponsors. Below are messages included as one of the sponsorship rewards
### ziqnr
"I have brain damage" - [ziqnr](https://github.com/ziqnr) 2024


[contributors-badge]: https://img.shields.io/github/contributors/ItsLogic/Xbox-Achievement-Unlocker?style=for-the-badge
[contributors-url]: https://github.com/ItsLogic/Xbox-Achievement-Unlocker/graphs/contributors
[forks-badge]: https://img.shields.io/github/forks/ItsLogic/Xbox-Achievement-Unlocker?style=for-the-badge
[forks-url]: https://github.com/ItsLogic/Xbox-Achievement-Unlocker/network/members
[stars-badge]: https://img.shields.io/github/stars/ItsLogic/Xbox-Achievement-Unlocker?style=for-the-badge
[stars-url]: https://github.com/ItsLogic/Xbox-Achievement-Unlocker/stargazers
[issues-badge]: https://img.shields.io/github/issues/ItsLogic/Xbox-Achievement-Unlocker?style=for-the-badge
[issues-url]: https://github.com/ItsLogic/Xbox-Achievement-Unlocker/issues
[release-badge]: https://img.shields.io/github/v/release/ItsLogic/Xbox-Achievement-Unlocker?style=for-the-badge
[release-url]: https://github.com/ItsLogic/Xbox-Achievement-Unlocker/releases
[discord-id]: https://img.shields.io/discord/1013602813093359657?logo=discord&style=for-the-badge
[discord-invite]: https://discord.gg/ugDvSw7cns
[WPF-Commit]: https://github.com/lepoco/wpfui/tree/c8cd75f6f82414a52a94d2a55fe2a21dd5db83d7
[LICENSE]:LICENSE
[MIT-LICENSE]:LICENSE.MIT
