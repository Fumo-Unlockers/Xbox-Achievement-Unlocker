# Xbox Achievement Unlocker
This is a tool designed to unlock achievements ~~ideally like Steam Achivements Manager~~ on microsoft/xbox games.
[Discord server](https://discord.gg/ugDvSw7cns)

## Why
After doing not that much research I couldn't find any single achievement unlockers and found a bunch of paid services offering unlock tools or services to unlock a full game's achievements on your account.
I decided I wanted to make a program that didnt indiscriminately add gamerscore from random games or charge you to unlock a game's achievements.

## How does it work?
I am making use of memory.dll to conveniently get the users XAuth token from one of the xbox app processes and then using that to make web requests to xbox servers to get information on achievements and then tell the server which of these achievements have been unlocked

## Done
Can semi-reliably grab XAuth (probably about as good as I will get it without making my code even worse)

Can query the xbox server to find out profile information(XUID, Gamerscore and Gamertag)

Can populate panel with recently played games (non xbox titles filtered out)

Can grab achievement data after clicking one of these games

Can populate a list on second form with the achievement data

Can unlock achievements on non event based games

Can detect which games are event based

Can spoof time in unowned and games using a supplied Title ID
## Todo
Figure out event based achivements and how they are unlocked

Rework the main window UI to allow for more games being displayed (partially done as the limit is now 42)

Create a list of Title IDs for the user to pick from in the game spoofer

Add a dropdown to allow the user to switch between recently played and library games

Change args on the images to load lower quality versions

Add a list of SCIDs the user can pick from for viewing and unlocking achivements in unowned games

Make loading the achivements list faster

Improve the list UI
