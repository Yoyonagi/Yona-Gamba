using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using YonaGamba.Theme;
using YonaGamba.UI;

namespace YonaGamba.Modules.Minigames;

/// <summary>A playing card. Rank runs from 1 (ace) to 13 (king); suits are 0 spades, 1 hearts, 2 diamonds, 3 clubs.</summary>
public readonly record struct PlayingCard(int Rank, int Suit)
{
    public string Label => Rank switch
    {
        1 => "A",
        11 => "J",
        12 => "Q",
        13 => "K",
        _ => Rank.ToString(),
    };

    public bool Red => Suit is 1 or 2;
}

/// <summary>A card on the table, with the timestamps that drive its deal and flip animations.</summary>
public sealed class DealtCard
{
    public DealtCard(PlayingCard card, bool faceDown = false, int delayMs = 0)
    {
        Card = card;
        FaceDown = faceDown;
        Born = Environment.TickCount64 + delayMs;
    }

    public PlayingCard Card { get; }
    public bool FaceDown { get; private set; }
    public long Born { get; }
    public long FlippedAt { get; private set; }

    public void Reveal()
    {
        if (!FaceDown) return;
        FaceDown = false;
        FlippedAt = Environment.TickCount64;
    }
}

/// <summary>A shuffled 52-card deck that reshuffles itself when it runs out.</summary>
public sealed class Deck
{
    private readonly List<PlayingCard> cards = new(52);

    public Deck() => Shuffle();

    public IReadOnlyList<PlayingCard> Remaining => cards;

    public void Shuffle()
    {
        cards.Clear();
        for (var suit = 0; suit < 4; suit++)
            for (var rank = 1; rank <= 13; rank++)
                cards.Add(new PlayingCard(rank, suit));

        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    public PlayingCard Draw()
    {
        if (cards.Count == 0) Shuffle();
        var card = cards[^1];
        cards.RemoveAt(cards.Count - 1);
        return card;
    }
}

/// <summary>Draws cards with the draw list: no textures, and suits are built from simple shapes.</summary>
public static class CardView
{
    public const float Width = 76;
    public const float Height = 108;

    private const float DealMs = 220f;
    private const float FlipMs = 260f;

    private static readonly Vector4 Face = new(0.96f, 0.96f, 0.98f, 1f);
    private static readonly Vector4 Ink = new(0.10f, 0.10f, 0.18f, 1f);
    private static readonly Vector4 RedInk = new(0.85f, 0.20f, 0.28f, 1f);

    /// <summary>
    /// Reserves a row and draws the hand in it. Cards overlap when the row is too narrow.
    /// </summary>
    public static void Row(IReadOnlyList<DealtCard> cards, float scale = 1f)
    {
        var s = Md3Theme.Scale * scale;
        var size = new Vector2(Width, Height) * s;
        var avail = ImGui.GetContentRegionAvail().X;
        var pos = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(avail, size.Y));

        var step = size.X + 10 * s;
        if (cards.Count > 1) step = Math.Min(step, (avail - size.X) / (cards.Count - 1));
        step = Math.Max(step, 18 * s);

        var dl = ImGui.GetWindowDrawList();
        for (var i = 0; i < cards.Count; i++)
            Draw(dl, pos + new Vector2(i * step, 0), cards[i], scale);
    }

    public static void Draw(ImDrawListPtr dl, Vector2 pos, DealtCard dealt, float scale = 1f)
    {
        var now = Environment.TickCount64;
        if (now < dealt.Born) return;

        var s = Md3Theme.Scale * scale;
        var size = new Vector2(Width, Height) * s;

        // Deal: fade in while dropping into place.
        var t = Anim.Ease(Math.Clamp((now - dealt.Born) / DealMs, 0f, 1f));
        pos.Y -= (1f - t) * 18 * s;

        // Flip: the card narrows to a line showing its back, then widens showing its face.
        var faceDown = dealt.FaceDown;
        var squash = 1f;
        if (dealt.FlippedAt != 0)
        {
            var f = Math.Clamp((now - dealt.FlippedAt) / FlipMs, 0f, 1f);
            faceDown = f < 0.5f;
            squash = Math.Abs(1f - 2f * f);
        }

        var w = Math.Max(2 * s, size.X * squash);
        var min = new Vector2(pos.X + (size.X - w) / 2, pos.Y);
        var max = min + new Vector2(w, size.Y);
        var r = 9 * s;

        dl.AddRectFilled(min + new Vector2(0, 2 * s), max + new Vector2(0, 3 * s), Col(Md3Theme.Scheme.Scrim, 0.30f * t), r);

        if (faceDown)
        {
            var c = Md3Theme.Scheme;
            dl.AddRectFilled(min, max, Col(c.Primary, t), r);
            if (squash > 0.5f)
            {
                var inset = new Vector2(7 * s);
                dl.AddRect(min + inset, max - inset, Col(c.OnPrimary, 0.55f * t), 5 * s, ImDrawFlags.None, 1.5f * s);
                Diamond(dl, (min + max) / 2, 13 * s, Col(c.OnPrimary, 0.75f * t));
            }

            return;
        }

        dl.AddRectFilled(min, max, Col(Face, t), r);
        dl.AddRect(min, max, Col(Ink, 0.25f * t), r, ImDrawFlags.None, 1 * s);
        if (squash < 0.7f) return;

        var ink = Col(dealt.Card.Red ? RedInk : Ink, t);
        float lineHeight;
        using (Md3Type.Title())
        {
            lineHeight = ImGui.GetTextLineHeight();
            dl.AddText(min + new Vector2(8 * s, 4 * s), ink, dealt.Card.Label);
        }

        // Small cards only have room for the rank and one suit.
        if (scale >= 0.8f)
            Suit(dl, dealt.Card.Suit, new Vector2(min.X + 16 * s, min.Y + 4 * s + lineHeight + 9 * s), 7 * s, ink);
        Suit(dl, dealt.Card.Suit, new Vector2((min.X + max.X) / 2 + 8 * s, min.Y + size.Y * 0.68f), 18 * s, ink);
    }

    private static uint Col(Vector4 color, float alpha) => Md3.U(color with { W = color.W * alpha });

    private static void Diamond(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        var top = new Vector2(c.X, c.Y - r);
        var bottom = new Vector2(c.X, c.Y + r);
        dl.AddTriangleFilled(top, new Vector2(c.X + r * 0.74f, c.Y), bottom, col);
        dl.AddTriangleFilled(top, bottom, new Vector2(c.X - r * 0.74f, c.Y), col);
    }

    private static void Stem(ImDrawListPtr dl, Vector2 c, float r, uint col) =>
        dl.AddTriangleFilled(new Vector2(c.X, c.Y + r * 0.10f), new Vector2(c.X + r * 0.38f, c.Y + r), new Vector2(c.X - r * 0.38f, c.Y + r), col);

    /// <summary>Draws a suit centered on <paramref name="c"/>, roughly <paramref name="r"/> tall each way.</summary>
    private static void Suit(ImDrawListPtr dl, int suit, Vector2 c, float r, uint col)
    {
        switch (suit)
        {
            case 0: // spade: a point on two lobes, with a stem
                dl.AddTriangleFilled(new Vector2(c.X, c.Y - r), new Vector2(c.X + r * 0.93f, c.Y + r * 0.22f), new Vector2(c.X - r * 0.93f, c.Y + r * 0.22f), col);
                dl.AddCircleFilled(new Vector2(c.X - r * 0.45f, c.Y + r * 0.28f), r * 0.48f, col);
                dl.AddCircleFilled(new Vector2(c.X + r * 0.45f, c.Y + r * 0.28f), r * 0.48f, col);
                Stem(dl, c, r, col);
                break;
            case 1: // heart: two lobes on a point
                dl.AddCircleFilled(new Vector2(c.X - r * 0.45f, c.Y - r * 0.38f), r * 0.50f, col);
                dl.AddCircleFilled(new Vector2(c.X + r * 0.45f, c.Y - r * 0.38f), r * 0.50f, col);
                dl.AddTriangleFilled(new Vector2(c.X - r * 0.93f, c.Y - r * 0.22f), new Vector2(c.X + r * 0.93f, c.Y - r * 0.22f), new Vector2(c.X, c.Y + r), col);
                break;
            case 2:
                Diamond(dl, c, r, col);
                break;
            default: // club: three lobes and a stem
                dl.AddCircleFilled(new Vector2(c.X, c.Y - r * 0.50f), r * 0.44f, col);
                dl.AddCircleFilled(new Vector2(c.X - r * 0.50f, c.Y + r * 0.18f), r * 0.44f, col);
                dl.AddCircleFilled(new Vector2(c.X + r * 0.50f, c.Y + r * 0.18f), r * 0.44f, col);
                dl.AddCircleFilled(new Vector2(c.X, c.Y), r * 0.30f, col);
                Stem(dl, c, r, col);
                break;
        }
    }
}
