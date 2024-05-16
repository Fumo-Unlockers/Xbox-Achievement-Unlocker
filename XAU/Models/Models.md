# Models

## Profile

These are two typical JSON responses for Xbox One APIs

### Tenured Profile

```json
{
    "people": [
        {
            "xuid": "",
            "isFavorite": false,
            "isFollowingCaller": false,
            "isFollowedByCaller": false,
            "isIdentityShared": false,
            "addedDateTimeUtc": null,
            "displayName": "",
            "realName": "",
            "displayPicRaw": "https://images-eds-ssl.xboxlive.com/image?",
            "showUserAsAvatar": "1",
            "gamertag": "",
            "gamerScore": "",
            "modernGamertag": "",
            "modernGamertagSuffix": "",
            "uniqueModernGamertag": "",
            "xboxOneRep": "GoodPlayer",
            "presenceState": "Offline",
            "presenceText": "Last seen 9d ago: Xbox Dev Mode",
            "presenceDevices": null,
            "isBroadcasting": false,
            "isCloaked": true,
            "isQuarantined": false,
            "isXbox360Gamerpic": false,
            "lastSeenDateTimeUtc": "2024-05-07T03:17:34.9881654",
            "suggestion": null,
            "recommendation": null,
            "search": null,
            "titleHistory": null,
            "multiplayerSummary": {
                "joinableActivities": [],
                "partyDetails": [],
                "inParty": 0
            },
            "recentPlayer": null,
            "follower": null,
            "preferredColor": {
                "primaryColor": "a21025",
                "secondaryColor": "450710",
                "tertiaryColor": "691117"
            },
            "presenceDetails": [
                {
                    "IsBroadcasting": false,
                    "Device": "Scarlett",
                    "DeviceSubType": null,
                    "GameplayType": null,
                    "PresenceText": "Last seen 9d ago: Xbox Dev Mode",
                    "State": "LastSeen",
                    "TitleId": "1879361470",
                    "TitleType": null,
                    "IsPrimary": true,
                    "IsGame": false,
                    "RichPresenceText": null
                }
            ],
            "titlePresence": null,
            "titleSummaries": null,
            "presenceTitleIds": null,
            "detail": {
                "accountTier": "Gold",
                "bio": "",
                "isVerified": false,
                "location": "",
                "tenure": "15",
                "watermarks": [
                    "ba75b64a-9a80-47ea-8c3a-76d3e2ea1422",
                    "9fa7919a-218d-4b1c-b81e-2bb18a25991f"
                ],
                "blocked": false,
                "mute": false,
                "followerCount": 366,
                "followingCount": 323,
                "hasGamePass": false
            },
            "communityManagerTitles": null,
            "socialManager": null,
            "broadcast": null,
            "avatar": null,
            "linkedAccounts": [
                {
                    "networkName": "Steam",
                    "displayName": "",
                    "showOnProfile": false,
                    "isFamilyFriendly": false,
                    "deeplink": ""
                }
            ],
            "colorTheme": "gamerpicblur",
            "preferredFlag": "",
            "preferredPlatforms": []
        }
    ],
    "recommendationSummary": null,
    "friendFinderState": null,
    "accountLinkDetails": null
}
```

### New profile

```json
{
    "people": [
        {
            "xuid": "",
            "isFavorite": false,
            "isFollowingCaller": false,
            "isFollowedByCaller": false,
            "isIdentityShared": false,
            "addedDateTimeUtc": null,
            "displayName": "",
            "realName": "",
            "displayPicRaw": "https://images-eds-ssl.xboxlive.com/image?",
            "showUserAsAvatar": "2",
            "gamertag": "",
            "gamerScore": "20",
            "modernGamertag": "",
            "modernGamertagSuffix": "",
            "uniqueModernGamertag": "",
            "xboxOneRep": "GoodPlayer",
            "presenceState": "Online",
            "presenceText": "Online",
            "presenceDevices": null,
            "isBroadcasting": false,
            "isCloaked": false,
            "isQuarantined": false,
            "isXbox360Gamerpic": false,
            "lastSeenDateTimeUtc": null,
            "suggestion": null,
            "recommendation": null,
            "search": null,
            "titleHistory": null,
            "multiplayerSummary": {
                "joinableActivities": [],
                "partyDetails": [],
                "inParty": 0
            },
            "recentPlayer": null,
            "follower": null,
            "preferredColor": {
                "primaryColor": "107c10",
                "secondaryColor": "102b14",
                "tertiaryColor": "155715"
            },
            "presenceDetails": [
                {
                    "IsBroadcasting": false,
                    "Device": "WindowsOneCore",
                    "DeviceSubType": null,
                    "GameplayType": null,
                    "PresenceText": "Online",
                    "State": "Active",
                    "TitleId": "704208617",
                    "TitleType": null,
                    "IsPrimary": true,
                    "IsGame": false,
                    "RichPresenceText": null
                }
            ],
            "titlePresence": null,
            "titleSummaries": null,
            "presenceTitleIds": null,
            "detail": {
                "accountTier": "Silver",
                "bio": "",
                "isVerified": false,
                "location": "",
                "tenure": "0",
                "watermarks": [],
                "blocked": false,
                "mute": false,
                "followerCount": 1,
                "followingCount": 2,
                "hasGamePass": false
            },
            "communityManagerTitles": null,
            "socialManager": null,
            "broadcast": null,
            "avatar": null,
            "linkedAccounts": [],
            "colorTheme": "gamerpicblur",
            "preferredFlag": "",
            "preferredPlatforms": []
        }
    ],
    "recommendationSummary": null,
    "friendFinderState": null,
    "accountLinkDetails": null
}
```
