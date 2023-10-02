# Xbox Achievement Unlocker
[![GitHub contributors][contributors-badge]][contributors-url]
[![GitHub forks][forks-badge]][forks-url]
[![GitHub stars][stars-badge]][stars-url]
[![GitHub issues][issues-badge]][issues-url]
[![GitHub release][release-badge]][release-url]

[![Discord][discord-id]][discord-invite]

A convenient tool for unlocking achievements on Microsoft/Xbox games, inspired by the functionality of Steam Achievements Manager.

## Table of Contents
- [Xbox Achievement Unlocker](#xbox-achievement-unlocker)
  - [Table of Contents](#table-of-contents)
  - [Why Xbox Achievement Unlocker?](#why-xbox-achievement-unlocker)
  - [How It Works](#how-it-works)
  - [Requirements](#requirements)
  - [What can it do?](#what-can-it-do)
  - [Screenshots](#screenshots)
  - [To-Do List](#to-do-list)
  - [Discord Server](#discord-server)
  - [License Information](#license-information)

## Why Xbox Achievement Unlocker?
Through my research, I found that there weren't any free achievement unlockers available. Instead, there were numerous paid services offering tools or services to unlock a full game's achievements on your account. This prompted me to create a program that doesn't randomly add gamerscore from arbitrary games or charge you to unlock a game's achievements.

## How It Works
This tool utilizes memory.dll to extract the user's XAuth token from one of the Xbox app processes. The token is then used to make web requests to Xbox servers, pulling information on achievements and informing the server which of these achievements have been unlocked.

## Requirements
Literally just [dotnet 7](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.11-windows-x64-installer) and the [new xbox app](https://apps.microsoft.com/store/detail/xbox/9MV0B5HZVK9Z)

## What can it do?
- Grab XAuth from xbox app
- Obtain a list of games from the user or any selected XUID
- Unlock Achievements for any Title Managed game
- Spoof time in any game
- Automatically detect updates and update itself

## Screenshots
coming soon probably

## To-Do List
- Implement auto spoofing functionality
- Implement an in app TitleID search
- Implement a stats editor
- Implement support for Event based stats
- Implement support for Event based achievements
- Implement in app login

## Discord Server
Join the [Discord server][discord-invite] (dont its full of idiots)

## License Information
The UI for this program was built ontop of the WPF-UI Fluent template as of [this commit][https://github.com/lepoco/wpfui/tree/c8cd75f6f82414a52a94d2a55fe2a21dd5db83d7] which is MIT licensed therefore a [copy is included in the repo][MIT-LICENSE]
Any and all modifications and/or additions to this template are GNU GPL licensed which you can find a copy of [here][LICENSE]


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