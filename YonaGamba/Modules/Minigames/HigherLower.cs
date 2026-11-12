using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using YonaGamba.Theme;
using YonaGamba.UI;

namespace YonaGamba.Modules.Minigames;

/// <summary>
/// Higher or Lower: guess whether the next card ranks above or below the one showing.
/// Every right guess multiplies the pot by odds worked out from the cards left in the deck,
/// so unlikely calls pay more. Cash out whenever you like; a wrong guess loses the pot.
/// Aces are low, kings are high, and a card of the same rank changes nothing.
/// </summary>
public sealed class HigherLower
{
    private enum Phase { Idle, Playing, Over }

    private const float HouseEdge = 0.96f;
    private const int HistoryShown = 9;

    private readonly MinigamesModule bank;
    private readonly Deck deck = new();
    private readonly List<DealtCard> cards = new();

    private Phase phase = Phase.Idle;
    private int pot;
    private int streak;
    private string message = string.Empty;
    private int outcome;

    public HigherLower(MinigamesModule bank) => this.bank = bank;

    public bool InRound => phase == Phase.Playing;

    private PlayingCard Current => cards[^1].Card;

    private void Start()
    {
        var bet = bank.Bet;
        if (!bank.TrySpend(bet)) return;

        deck.Shuffle();
        cards.Clear();
        cards.Add(new DealtCard(deck.Draw()));
        pot = bet;
        streak = 0;
        message = "Will the next card be higher or lower?";
        outcome = 0;
        phase = Phase.Playing;
    }

    /// <summary>How many cards left in the deck rank above and below the current one.</summary>
    private (int Higher, int Lower, int Total) Odds()
    {
        if (deck.Remaining.Count == 0) deck.Shuffle();
        var rank = Current.Rank;
        return (deck.Remaining.Count(c => c.Rank > rank), deck.Remaining.Count(c => c.Rank < rank), deck.Remaining.Count);
    }

    private static float Multiplier(int winning, int total) =>
        winning <= 0 ? 0f : MathF.Max(1.05f, HouseEdge * total / winning);

    private void Guess(bool higher)
    {
        var (up, down, total) = Odds();
        var multiplier = Multiplier(higher ? up : down, total);
        var previous = Current;
        var next = deck.Draw();
        cards.Add(new DealtCard(next));

        if (next.Rank == previous.Rank)
        {
            message = "Same rank. Nothing changes, guess again.";
            return;
        }

        if (next.Rank > previous.Rank == higher)
        {
            pot = (int)MathF.Round(pot * multiplier);
            streak++;
            message = $"Right! The pot is now {pot:N0}.";
            return;
        }

        var lost = pot;
        var reached = streak;
        pot = 0;
        phase = Phase.Over;
        outcome = -1;
        message = $"Wrong, it was {(higher ? "lower" : "higher")}. You lose the {lost:N0} pot.";
        bank.Settle(0, s => s.HiLoBestStreak = Math.Max(s.HiLoBestStreak, reached));
    }

    private void CashOut()
    {
        var won = pot;
        var reached = streak;
        pot = 0;
        phase = Phase.Over;
        outcome = 1;
        message = $"Cashed out {won:N0} after {reached} right in a row.";
        bank.Settle(won, s =>
        {
            s.HiLoBestStreak = Math.Max(s.HiLoBestStreak, reached);
            s.HiLoBestCashOut = Math.Max(s.HiLoBestCashOut, won);
        });
    }

    public void Draw()
    {
        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;

        Md3.Label(cards.Count == 0 ? "CURRENT CARD" : $"CURRENT CARD · {RankName(Current.Rank)}".ToUpperInvariant(), c.Accent);

        // The card on the left, the pot beside it.
        var pos = ImGui.GetCursorScreenPos();
        var size = new Vector2(CardView.Width, CardView.Height) * s;
        ImGui.Dummy(size);
        var dl = ImGui.GetWindowDrawList();
        if (cards.Count == 0) dl.AddRect(pos, pos + size, Md3.U(c.OutlineVariant), 9 * s, ImDrawFlags.None, 1.5f * s);
        else CardView.Draw(dl, pos, cards[^1]);

        ImGui.SameLine(0, 18 * s);
        using (ImRaii.Group())
        {
            Md3.Spacer(6);
            if (phase == Phase.Playing)
            {
                Md3.Label("POT");
                Md3.Headline($"{pot:N0}");
                Md3.Label(streak == 0 ? "No right guesses yet" : $"{streak} right in a row");
            }
            else
            {
                Md3.Supporting("Aces are low, kings are high.\nUnlikely guesses pay more.\nA wrong guess loses the pot.");
            }
        }

        ImGui.SetCursorScreenPos(pos + new Vector2(0, size.Y + ImGui.GetStyle().ItemSpacing.Y));
        ImGui.Dummy(Vector2.Zero);

        // Earlier cards of this run, oldest first.
        Md3.Label("PREVIOUS CARDS", c.Accent);
        const float small = 0.6f;
        if (cards.Count > 1)
        {
            var from = Math.Max(0, cards.Count - 1 - HistoryShown);
            CardView.Row(cards.GetRange(from, cards.Count - 1 - from), small);
        }
        else
        {
            ImGui.Dummy(new Vector2(1, CardView.Height * small * s));
        }

        Md3.Spacer(6);
        if (phase == Phase.Playing)
        {
            Md3.Body(message);
            var (up, down, total) = Odds();
            var upX = Multiplier(up, total);
            var downX = Multiplier(down, total);

            using (ImRaii.Disabled(up == 0))
            {
                if (Md3.FilledButton(up == 0 ? "Higher##hl-up" : $"Higher  ×{upX:0.00}##hl-up", FontAwesomeIcon.ArrowUp)) Guess(true);
            }

            Md3.Tooltip(up == 0 ? "No card left in the deck is higher" : $"{up} of the {total} cards left are higher");
            ImGui.SameLine(0, 8 * s);
            using (ImRaii.Disabled(down == 0))
            {
                if (Md3.FilledButton(down == 0 ? "Lower##hl-down" : $"Lower  ×{downX:0.00}##hl-down", FontAwesomeIcon.ArrowDown)) Guess(false);
            }

            Md3.Tooltip(down == 0 ? "No card left in the deck is lower" : $"{down} of the {total} cards left are lower");
            ImGui.SameLine(0, 8 * s);
            using (ImRaii.Disabled(streak == 0))
            {
                if (Md3.OutlinedButton($"Cash out {pot:N0}##hl-cash", FontAwesomeIcon.Check)) CashOut();
            }

            Md3.Tooltip(streak == 0 ? "Guess right at least once before cashing out" : "Take the pot and end the run");
        }
        else
        {
            if (phase == Phase.Over)
                Md3.Title(message, outcome > 0 ? c.Accent : c.Error);
            bank.DrawBetRow("hl", phase == Phase.Over ? "Play again" : "Start", Start);
        }

        Md3.Spacer(4);
        var stats = bank.Stats;
        Md3.Label($"Best streak {stats.HiLoBestStreak:N0} · Biggest cash-out {stats.HiLoBestCashOut:N0}");
    }

    private static string RankName(int rank) => rank switch
    {
        1 => "Ace (low)",
        11 => "Jack",
        12 => "Queen",
        13 => "King",
        _ => rank.ToString(),
    };
}
