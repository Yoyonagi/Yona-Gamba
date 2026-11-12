using System;
using System.Numerics;

namespace YonaGamba.Theme;

/// <summary>
/// Color helpers built on CIELAB / LCh(ab).
/// Material 3's "tone" is exactly CIELAB L*, so tonal palettes generated here use the
/// same tones as the spec; hue and chroma are approximated with LCh instead of CAM16.
/// </summary>
public static class ColorMath
{
    private const double Xn = 0.95047, Yn = 1.0, Zn = 1.08883;
    private const double Epsilon = 216.0 / 24389.0;
    private const double Kappa = 24389.0 / 27.0;

    public static Vector4 FromArgb(uint argb) => new(
        ((argb >> 16) & 0xFF) / 255f,
        ((argb >> 8) & 0xFF) / 255f,
        (argb & 0xFF) / 255f,
        ((argb >> 24) & 0xFF) / 255f);

    public static uint ToArgb(Vector4 c) =>
        (Byte(c.W) << 24) | (Byte(c.X) << 16) | (Byte(c.Y) << 8) | Byte(c.Z);

    /// <summary>Packs a color into ImGui's ABGR u32 format.</summary>
    public static uint ToImU32(Vector4 c) =>
        (Byte(c.W) << 24) | (Byte(c.Z) << 16) | (Byte(c.Y) << 8) | Byte(c.X);

    public static Vector4 WithAlpha(Vector4 c, float alpha) => c with { W = alpha };

    /// <summary>Material "state layer": <paramref name="content"/> laid over <paramref name="container"/> at <paramref name="opacity"/>.</summary>
    public static Vector4 StateLayer(Vector4 container, Vector4 content, float opacity)
    {
        if (container.W <= 0.001f)
            return content with { W = opacity };

        var rgb = Vector3.Lerp(new Vector3(container.X, container.Y, container.Z),
                               new Vector3(content.X, content.Y, content.Z), opacity);
        return new Vector4(rgb, container.W);
    }

    public static (double L, double C, double H) ToLch(Vector4 rgb)
    {
        var r = Linear(rgb.X);
        var g = Linear(rgb.Y);
        var b = Linear(rgb.Z);

        var x = 0.4124564 * r + 0.3575761 * g + 0.1804375 * b;
        var y = 0.2126729 * r + 0.7151522 * g + 0.0721750 * b;
        var z = 0.0193339 * r + 0.1191920 * g + 0.9503041 * b;

        var fx = F(x / Xn);
        var fy = F(y / Yn);
        var fz = F(z / Zn);

        var l = 116 * fy - 16;
        var a = 500 * (fx - fy);
        var bb = 200 * (fy - fz);

        var c = Math.Sqrt(a * a + bb * bb);
        var h = Math.Atan2(bb, a) * 180 / Math.PI;
        if (h < 0) h += 360;
        return (l, c, h);
    }

    /// <summary>Converts LCh to sRGB, reducing chroma until the color fits in gamut.</summary>
    public static Vector4 FromLch(double l, double c, double h)
    {
        if (l <= 0.0) return new Vector4(0, 0, 0, 1);
        if (l >= 100.0) return new Vector4(1, 1, 1, 1);
        if (TryLch(l, c, h, out var direct)) return direct;

        TryLch(l, 0, h, out var best);
        double lo = 0, hi = c;
        for (var i = 0; i < 24; i++)
        {
            var mid = (lo + hi) / 2;
            if (TryLch(l, mid, h, out var candidate))
            {
                lo = mid;
                best = candidate;
            }
            else
            {
                hi = mid;
            }
        }

        return best;
    }

    private static bool TryLch(double l, double c, double h, out Vector4 rgb)
    {
        var hr = h * Math.PI / 180;
        var a = c * Math.Cos(hr);
        var b = c * Math.Sin(hr);

        var fy = (l + 16) / 116;
        var fx = fy + a / 500;
        var fz = fy - b / 200;

        var x = Xn * FInv(fx);
        var y = Yn * FInv(fy);
        var z = Zn * FInv(fz);

        var r = 3.2404542 * x - 1.5371385 * y - 0.4985314 * z;
        var g = -0.9692660 * x + 1.8760108 * y + 0.0415560 * z;
        var bl = 0.0556434 * x - 0.2040259 * y + 1.0572252 * z;

        const double tol = 1e-4;
        var inGamut = r is >= -tol and <= 1 + tol && g is >= -tol and <= 1 + tol && bl is >= -tol and <= 1 + tol;

        rgb = new Vector4((float)Gamma(Math.Clamp(r, 0, 1)),
                          (float)Gamma(Math.Clamp(g, 0, 1)),
                          (float)Gamma(Math.Clamp(bl, 0, 1)), 1f);
        return inGamut;
    }

    private static uint Byte(float v) => (uint)Math.Round(Math.Clamp(v, 0f, 1f) * 255f);
    private static double F(double t) => t > Epsilon ? Math.Cbrt(t) : (Kappa * t + 16) / 116;
    private static double FInv(double t) { var t3 = t * t * t; return t3 > Epsilon ? t3 : (116 * t - 16) / Kappa; }
    private static double Linear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    private static double Gamma(double c) => c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
}

/// <summary>A hue/chroma pair from which any tone (0 = black, 100 = white) can be sampled.</summary>
public readonly record struct TonalPalette(double Hue, double Chroma)
{
    public Vector4 Tone(double tone) => ColorMath.FromLch(tone, Chroma, Hue);
}
