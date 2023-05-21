# Xbox Achievement Unlocker
![Discord][discord-id]  

A convenient tool for unlocking achievements on Microsoft/Xbox games, inspired by the functionality of Steam Achievements Manager.

## Table of Contents
- [Why Xbox Achievement Unlocker?](#why-xbox-achievement-unlocker)
- [How It Works](#how-it-works)
- [Achievements So Far](#achievements-so-far)
- [To-Do List](#to-do-list)
- [Discord Server](#discord-server)

## Why Xbox Achievement Unlocker?
Through my research, I found that there weren't any free achievement unlockers available. Instead, there were numerous paid services offering tools or services to unlock a full game's achievements on your account. This prompted me to create a program that doesn't randomly add gamerscore from arbitrary games or charge you to unlock a game's achievements.

## How It Works
This tool utilizes memory.dll to extract the user's XAuth token from one of the Xbox app processes. The token is then used to make web requests to Xbox servers, pulling information on achievements and informing the server which of these achievements have been unlocked.

## Achievements So Far
1. Can semi-reliably obtain XAuth.
2. Able to query the Xbox server for profile information (XUID, Gamerscore, and Gamertag).
3. Can populate panel with recently played games (non-Xbox titles filtered out).
4. Able to retrieve achievement data after selecting one of these games.
5. Can populate a list on the second form with achievement data.
6. Can unlock achievements on non-event-based games.
7. Able to detect which games are event-based.
8. Can spoof time in unowned games using a supplied Title ID.

## To-Do List
1. Understand event-based achievements and how they are unlocked.
2. Redesign the main window UI to allow for the display of more games.
3. Create a list of Title IDs for the user to choose from in the game spoofer.
4. Add a dropdown menu to switch between recently played and library games.
5. Lower image load quality.
6. Add a list of SCIDs that the user can select from for viewing and unlocking achievements in unowned games.
7. Speed up the loading of the achievements list.
8. Improve the list UI.

## Discord Server
Join us on our [Discord server][discord-invite] to stay updated and engage with our community.

[discord-id]: https://img.shields.io/discord/1013602813093359657?logo=discord&style=for-the-badge
[discord-invite]: https://discord.gg/ugDvSw7cns