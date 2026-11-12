using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using YonaGamba.Theme;
using YonaGamba.UI;

namespace YonaGamba.Modules.Minigames;

/// <summary>
/// Single-deck blackjack against the dealer. The dealer stands on every 17, a natural
/// blackjack pays 3:2, and doubling down is allowed on the first two cards. No splits.
/// </summary>
public sealed class Blackjack
{
    private enum Phase { Betting, Player, Dealer, Done }

    private const int DealerPaceMs = 650;

    private readonly MinigamesModule bank;
    private readonly Deck deck = new();
    private readonly List<DealtCard> player = new();
    private readonly List<DealtCard> dealer = new();

    private Phase phase = Phase.Betting;
    private int bet;
    private long nextDealerMove;
    private string result = string.Empty;
    private int outcome; // -1 lost, 0 push, 1 won

    public Blackjack(MinigamesModule bank) => this.bank = bank;

    /// <summary>True while chips are on the table, so the bet can't be changed.</summary>
    public bool InRound => phase is Phase.Player or Phase.Dealer;

    private static (int Total, bool Soft) Value(IEnumerable<DealtCard> hand)
    {
        var total = 0;
        var aces = 0;
        foreach (var dealt in hand)
        {
            if (dealt.FaceDown) continue;
            var rank = dealt.Card.Rank;
            if (rank == 1) aces++;
            total += rank switch { 1 => 11, > 10 => 10, _ => rank };
        }

        while (total > 21 && aces > 0)
        {
            total -= 10;
            aces--;
        }

        return (total, aces > 0);
    }

    private static string Describe(IReadOnlyList<DealtCard> hand)
    {
        if (hand.Count == 0) return string.Empty;
        var (total, soft) = Value(hand);
        if (hand.Count == 2 && total == 21 && hand.All(c => !c.FaceDown)) return "Blackjack";
        return soft && total < 21 ? $"Soft {total}" : total.ToString();
    }

    // ───────────────────────────── Rules ─────────────────────────────

    private void Deal()
    {
        bet = bank.Bet;
        if (!bank.TrySpend(bet)) return;

        // A fresh shuffle whenever the deck is getting thin keeps card counting out of it.
        if (deck.Remaining.Count < 15) deck.Shuffle();

        player.Clear();
        dealer.Clear();
        result = string.Empty;
        player.Add(new DealtCard(deck.Draw()));
        dealer.Add(new DealtCard(deck.Draw(), false, 150));
        player.Add(new DealtCard(deck.Draw(), false, 300));
        dealer.Add(new DealtCard(deck.Draw(), true, 450));
        phase = Phase.Player;

        var playerNatural = Value(player).Total == 21;
        var dealerNatural = dealer[0].Card.Rank is 1 or >= 10 && Natural(dealer);
        if (playerNatural || dealerNatural) Stand(800);
    }

    private static bool Natural(IReadOnlyList<DealtCard> hand)
    {
        if (hand.Count != 2) return false;
        var a = hand[0].Card.Rank;
        var b = hand[1].Card.Rank;
        return (a == 1 && b >= 10) || (b == 1 && a >= 10);
    }

    private void Hit()
    {
        player.Add(new DealtCard(deck.Draw()));
        var total = Value(player).Total;
        if (total > 21) Finish();
        else if (total == 21) Stand();
    }

    private void DoubleDown()
    {
        if (!bank.TrySpend(bet)) return;
        bet *= 2;
        player.Add(new DealtCard(deck.Draw()));
        if (Value(player).Total > 21) Finish();
        else Stand();
    }

    private void Stand(int delayMs = DealerPaceMs)
    {
        phase = Phase.Dealer;
        nextDealerMove = Environment.TickCount64 + delayMs;
    }

    /// <summary>Plays the dealer's hand one card at a time so the draws can be followed.</summary>
    private void Tick()
    {
        if (phase != Phase.Dealer || Environment.TickCount64 < nextDealerMove) return;

        if (dealer[1].FaceDown)
        {
            dealer[1].Reveal();
            nextDealerMove = Environment.TickCount64 + DealerPaceMs;
            return;
        }

        var playerNatural = Natural(player);
        if (!playerNatural && Value(dealer).Total < 17)
        {
            dealer.Add(new DealtCard(deck.Draw()));
            nextDealerMove = Environment.TickCount64 + DealerPaceMs;
            return;
        }

        Finish();
    }

    private void Finish()
    {
        if (dealer.Count > 1) dealer[1].Reveal();
        phase = Phase.Done;

        var mine = Value(player).Total;
        var theirs = Value(dealer).Total;
        var myNatural = Natural(player);
        var theirNatural = Natural(dealer);

        int payout;
        if (mine > 21)
        {
            (outcome, payout, result) = (-1, 0, $"Bust. You lose {bet:N0}.");
        }
        else if (myNatural && !theirNatural)
        {
            payout = bet + bet * 3 / 2;
            (outcome, result) = (1, $"Blackjack! You win {payout - bet:N0}.");
        }
        else if (theirNatural && !myNatural)
        {
            (outcome, payout, result) = (-1, 0, $"Dealer has blackjack. You lose {bet:N0}.");
        }
        else if (theirs > 21)
        {
            (outcome, payout, result) = (1, bet * 2, $"Dealer busts. You win {bet:N0}.");
        }
        else if (mine > theirs)
        {
            (outcome, payout, result) = (1, bet * 2, $"{mine} beats {theirs}. You win {bet:N0}.");
        }
        else if (mine < theirs)
        {
            (outcome, payout, result) = (-1, 0, $"{theirs} beats {mine}. You lose {bet:N0}.");
        }
        else
        {
            (outcome, payout, result) = (0, bet, "Push. Your bet comes back.");
        }

        bank.Settle(payout, s =>
        {
            if (outcome > 0) s.BlackjackWins++;
            else if (outcome < 0) s.BlackjackLosses++;
            else s.BlackjackPushes++;
        });
    }

    // ───────────────────────────── Drawing ─────────────────────────────

    public void Draw()
    {
        Tick();
        var s = Md3Theme.Scale;
        var c = Md3Theme.Scheme;

        Md3.Label(dealer.Count == 0 ? "DEALER" : $"DEALER · {Describe(dealer)}".ToUpperInvariant(), c.Accent);
        if (dealer.Count == 0) Placeholder("The dealer stands on 17. Blackjack pays 3 to 2.");
        else CardView.Row(dealer);

        Md3.Spacer(6);
        Md3.Label(player.Count == 0 ? "YOU" : $"YOU · {Describe(player)}".ToUpperInvariant(), c.Accent);
        if (player.Count == 0) Placeholder("Pick a bet and deal to start.");
        else CardView.Row(player);

        Md3.Spacer(6);
        switch (phase)
        {
            case Phase.Player:
                Md3.Title($"Bet {bet:N0}");
                if (Md3.FilledButton("Hit##bj-hit", FontAwesomeIcon.Plus)) Hit();
                Md3.Tooltip("Take another card");
                ImGui.SameLine(0, 8 * s);
                if (Md3.TonalButton("Stand##bj-stand", FontAwesomeIcon.Check)) Stand();
                Md3.Tooltip("Keep your hand and let the dealer play");
                ImGui.SameLine(0, 8 * s);
                var canDouble = player.Count == 2 && bank.Chips >= bet;
                using (ImRaii.Disabled(!canDouble))
                {
                    if (Md3.OutlinedButton("Double##bj-double", FontAwesomeIcon.AngleDoubleUp)) DoubleDown();
                }

                Md3.Tooltip("Double your bet, take exactly one more card, then stand");
                break;

            case Phase.Dealer:
                Md3.Title($"Bet {bet:N0}");
                Md3.Supporting("Dealer is playing…");
                break;

            default:
                if (phase == Phase.Done)
                    Md3.Title(result, outcome > 0 ? c.Accent : outcome < 0 ? c.Error : c.OnSurface);
                bank.DrawBetRow("bj", phase == Phase.Done ? "Deal again" : "Deal", Deal);
                break;
        }

        Md3.Spacer(4);
        var stats = bank.Stats;
        Md3.Label($"Won {stats.BlackjackWins:N0} · Lost {stats.BlackjackLosses:N0} · Pushed {stats.BlackjackPushes:N0}");
    }

    private static void Placeholder(string text)
    {
        var s = Md3Theme.Scale;
        var pos = ImGui.GetCursorScreenPos();
        var size = new System.Numerics.Vector2(CardView.Width, CardView.Height) * s;
        ImGui.Dummy(size);
        ImGui.GetWindowDrawList().AddRect(pos, pos + size, Md3.U(Md3Theme.Scheme.OutlineVariant), 9 * s, ImDrawFlags.None, 1.5f * s);
        ImGui.SameLine(0, 14 * s);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (size.Y - ImGui.GetTextLineHeight()) / 2);
        Md3.Supporting(text);
        ImGui.SetCursorScreenPos(pos + new System.Numerics.Vector2(0, size.Y));
        ImGui.Dummy(System.Numerics.Vector2.Zero);
    }
}
