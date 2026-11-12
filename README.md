# Yona Gamba

A Dalamud plugin for Final Fantasy XIV: two card games to pass the time in queue, played
with play chips. No gil is involved and nothing is sent to the game or to other players.
Material Design 3 interface in a navy and electric-blue palette, dark and light.

## Install

1. In game, open `/xlsettings` → **Experimental**.
2. Under **Custom Plugin Repositories**, add:
   ```
   https://raw.githubusercontent.com/Yoyonagi/Yona-XIV-Repo/main/repo.json
   ```
3. Tick the checkbox, save, then find **Yona Gamba** in `/xlplugins`.

Coming from Yona Toolbox? The first start carries your chips and statistics over.

## Commands

| Command | What it does |
| --- | --- |
| `/ygamba` | Toggle the main window |
| `/ygamba settings` | Open the settings page |

## Games

- **Blackjack**: the dealer stands on 17, blackjack pays 3 to 2, and you can double down
  on your first two cards.
- **Higher or Lower**: guess whether the next card is higher or lower. Each right guess
  multiplies the pot by odds based on the cards left in the deck; cash out any time after
  the first right guess. Aces are low.
- You start with 1,000 chips and can refill when you run out, or reset them in Settings.

## Building

Pushes to `main` build a dev artifact in GitHub Actions; a tag like `v1.2.3` publishes a
release that [Yona XIV Repo](https://github.com/Yoyonagi/Yona-XIV-Repo) picks up.
