using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using YonaGamba.Theme;
using YonaGamba.UI;

namespace YonaGamba.Modules.Minigames;

public sealed class MinigamesConfig
{
    /// <summary>Play chips shared by every game. They are not gil and have no value.</summary>
    public int Chips { get; set; } = MinigamesModule.StartingChips;

    public int BetIndex { get; set; } = 1;
    public int LastGame { get; set; }

    public int BlackjackWins { get; set; }
    public int BlackjackLosses { get; set; }
    public int BlackjackPushes { get; set; }

    public int HiLoBestStreak { get; set; }
    public int HiLoBestCashOut { get; set; }
}

/// <summary>
/// Small card games to pass the time in queue. Everything runs locally with play chips:
/// nothing is sent to the game or to other players.
/// </summary>
public sealed class MinigamesModule : ModuleBase<MinigamesConfig>
{
    public const int StartingChips = 1000;

    private static readonly int[] Bets = { 10, 25, 50, 100, 250 };
    private static readonly string[] BetLabels = { "10", "25", "50", "100", "250" };
    private static readonly string[] Games = { "Blackjack", "Higher or Lower" };

    private Blackjack? blackjack;
    private HigherLower? higherLower;

    public override string Id => "minigames";
    public override string Name => "Minigames";
    public override string Description => "Blackjack and Higher or Lower, played with play chips while you wait.";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Dice;
    public override int Order => 20;
    public override bool EnabledByDefault => true;
    public override bool HasSettings => true;

    internal int Chips => Config.Chips;
    internal int Bet => Bets[Math.Clamp(Config.BetIndex, 0, Bets.Length - 1)];
    internal MinigamesConfig Stats => Config;

    protected override void OnEnable()
    {
        blackjack = new Blackjack(this);
        higherLower = new HigherLower(this);
    }

    protected override void OnDisable()
    {
        blackjack = null;
        higherLower = null;
    }

    // ───────────────────────────── Bank ─────────────────────────────

    internal bool TrySpend(int amount)
    {
        if (amount <= 0 || Config.Chips < amount) return false;
        Config.Chips -= amount;
        SaveConfig();
        return true;
    }

    /// <summary>Pays out the end of a round and lets the game record its statistics.</summary>
    internal void Settle(int payout, Action<MinigamesConfig> record)
    {
        Config.Chips += Math.Max(0, payout);
        record(Config);
        SaveConfig();
    }

    /// <summary>The bet picker and start button shown between rounds, or a refill when the chips ran out.</summary>
    internal void DrawBetRow(string id, string startLabel, Action start)
    {
        var s = Md3Theme.Scale;

        if (Config.Chips < Bets[0])
        {
            Md3.Supporting("You're out of chips.");
            if (Md3.TonalButton($"Take {StartingChips:N0} more chips##{id}-refill", FontAwesomeIcon.Undo))
            {
                Config.Chips = StartingChips;
                SaveConfig();
            }

            return;
        }

        Md3.Label("BET");
        var avail = ImGui.GetContentRegionAvail().X;
        var width = Math.Min(330 * s, avail);
        var index = Math.Clamp(Config.BetIndex, 0, Bets.Length - 1);
        var top = ImGui.GetCursorPos();
        if (Md3.SegmentedButtons($"{id}-bet", BetLabels, ref index, width))
        {
            Config.BetIndex = index;
            SaveConfig();
        }

        // The start button sits beside the picker, or under it when the page is narrow.
        var after = ImGui.GetCursorPos();
        var beside = avail - width >= 190 * s;
        if (beside) ImGui.SetCursorPos(top + new Vector2(width + 12 * s, 2 * s));

        var affordable = Config.Chips >= Bet;
        using (ImRaii.Disabled(!affordable))
        {
            if (Md3.FilledButton($"{startLabel}##{id}-start", FontAwesomeIcon.Play)) start();
        }

        Md3.Tooltip(affordable ? $"Bet {Bet:N0} chips" : "You don't have enough chips for this bet");

        if (beside)
        {
            ImGui.SetCursorPos(after);
            ImGui.Dummy(Vector2.Zero);
        }
    }

    // ───────────────────────────── Page ─────────────────────────────

    public override void DrawPage()
    {
        if (blackjack == null || higherLower == null) return;
        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;

        var avail = ImGui.GetContentRegionAvail().X;
        var top = ImGui.GetCursorPos();
        var game = Math.Clamp(Config.LastGame, 0, Games.Length - 1);
        if (Md3.SegmentedButtons("minigames-game", Games, ref game, Math.Min(340 * s, avail)))
        {
            Config.LastGame = game;
            SaveConfig();
        }

        var after = ImGui.GetCursorPos();

        // Chip balance, right-aligned on the same row when there is room for it.
        var chips = $"{Config.Chips:N0}";
        float chipsWidth;
        using (Md3Type.Title()) chipsWidth = ImGui.CalcTextSize(chips).X;
        float captionWidth;
        using (Md3Type.Label()) captionWidth = ImGui.CalcTextSize("CHIPS").X;
        var block = chipsWidth + captionWidth + 8 * s;
        if (avail - Math.Min(340 * s, avail) >= block + 16 * s)
        {
            ImGui.SetCursorPos(top + new Vector2(avail - block, 14 * s));
            Md3.Label("CHIPS");
            ImGui.SetCursorPos(top + new Vector2(avail - chipsWidth, 6 * s));
            Md3.Title(chips, c.Accent);
            ImGui.SetCursorPos(after);
            ImGui.Dummy(Vector2.Zero);
        }
        else
        {
            Md3.Label($"CHIPS · {chips}", c.Accent);
        }

        Md3.Spacer(2);
        using var table = Md3.Pane("##minigames-table", Vector2.Zero, c.SurfaceContainerHigh, 20, 16);
        if (!table.Visible) return;

        if (game == 0) blackjack.Draw();
        else higherLower.Draw();
    }

    public override void DrawSettings()
    {
        Md3.Supporting("The games use play chips only. They are not gil, have no value, and nothing is sent to the game or to other players.");
        Md3.Spacer(4);
        Md3.Body($"Chips: {Config.Chips:N0}");

        var busy = blackjack?.InRound == true || higherLower?.InRound == true;
        using (ImRaii.Disabled(busy))
        {
            if (Md3.TonalButton($"Reset chips to {StartingChips:N0}##minigames-reset-chips", FontAwesomeIcon.Undo))
            {
                Config.Chips = StartingChips;
                SaveConfig();
            }
        }

        if (busy) Md3.Tooltip("Finish the round in progress first");

        ImGui.SameLine(0, 8 * Md3Theme.Scale);
        if (Md3.TextButton("Clear statistics##minigames-reset-stats"))
        {
            Config.BlackjackWins = Config.BlackjackLosses = Config.BlackjackPushes = 0;
            Config.HiLoBestStreak = Config.HiLoBestCashOut = 0;
            SaveConfig();
        }
    }
}
