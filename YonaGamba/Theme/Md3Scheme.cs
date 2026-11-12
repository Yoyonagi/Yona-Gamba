using System;
using System.Numerics;

namespace YonaGamba.Theme;

/// <summary>
/// The Material 3 color roles, filled with a fixed navy / electric-blue palette.
/// See https://m3.material.io/styles/color/roles
/// </summary>
public sealed class Md3Scheme
{
    public bool IsDark { get; init; }
    public uint Seed { get; init; }

    public Vector4 Primary { get; init; }
    public Vector4 OnPrimary { get; init; }
    public Vector4 PrimaryContainer { get; init; }
    public Vector4 OnPrimaryContainer { get; init; }

    public Vector4 Secondary { get; init; }
    public Vector4 OnSecondary { get; init; }
    public Vector4 SecondaryContainer { get; init; }
    public Vector4 OnSecondaryContainer { get; init; }

    public Vector4 Tertiary { get; init; }
    public Vector4 OnTertiary { get; init; }
    public Vector4 TertiaryContainer { get; init; }
    public Vector4 OnTertiaryContainer { get; init; }

    public Vector4 Error { get; init; }
    public Vector4 OnError { get; init; }
    public Vector4 ErrorContainer { get; init; }
    public Vector4 OnErrorContainer { get; init; }

    public Vector4 Surface { get; init; }
    public Vector4 OnSurface { get; init; }
    public Vector4 SurfaceVariant { get; init; }
    public Vector4 OnSurfaceVariant { get; init; }
    public Vector4 SurfaceContainerLowest { get; init; }
    public Vector4 SurfaceContainerLow { get; init; }
    public Vector4 SurfaceContainer { get; init; }
    public Vector4 SurfaceContainerHigh { get; init; }
    public Vector4 SurfaceContainerHighest { get; init; }

    public Vector4 Outline { get; init; }
    public Vector4 OutlineVariant { get; init; }

    public Vector4 InverseSurface { get; init; }
    public Vector4 InverseOnSurface { get; init; }
    public Vector4 InversePrimary { get; init; }
    public Vector4 Scrim { get; init; }

    /// <summary>
    /// A primary-family color that is safe to use for text and thin strokes on surfaces.
    /// Defaults to <see cref="Primary"/>; themes whose primary is too dark for text override it.
    /// </summary>
    public Vector4 Accent { get => accent ?? Primary; init => accent = value; }
    private readonly Vector4? accent;

    private static Vector4 Hex(uint rgb) => ColorMath.FromArgb(0xFF000000 | rgb);

    /// <summary>
    /// Fixed palette inspired by nousresearch.com: deep navy surfaces, electric blue (#0000F2)
    /// actions, periwinkle accents and off-white text.
    /// </summary>
    public static Md3Scheme Create(bool dark) => dark
        ? new Md3Scheme
        {
            IsDark = true, Seed = 0xFF0000F2,
            Primary = Hex(0x0000F2), OnPrimary = Hex(0xFFFFFF), PrimaryContainer = Hex(0x000091), OnPrimaryContainer = Hex(0xFAFAFF),
            Secondary = Hex(0x8585F9), OnSecondary = Hex(0x0D0D24), SecondaryContainer = Hex(0x2B2B57), OnSecondaryContainer = Hex(0xFAFAFF),
            Tertiary = Hex(0xB8B8CB), OnTertiary = Hex(0x0D0D24), TertiaryContainer = Hex(0x2E2E4A), OnTertiaryContainer = Hex(0xE6E6EF),
            Error = Hex(0xFF7B72), OnError = Hex(0x2A0505), ErrorContainer = Hex(0x5C1414), OnErrorContainer = Hex(0xFFDAD6),
            Surface = Hex(0x19192F), OnSurface = Hex(0xF2F2F2), SurfaceVariant = Hex(0x26264A), OnSurfaceVariant = Hex(0xB8B8CB),
            SurfaceContainerLowest = Hex(0x0A0A1C), SurfaceContainerLow = Hex(0x0D0D24), SurfaceContainer = Hex(0x141430),
            SurfaceContainerHigh = Hex(0x19192F), SurfaceContainerHighest = Hex(0x26264A),
            Outline = Hex(0x6A6A99), OutlineVariant = Hex(0x2E2E55),
            InverseSurface = Hex(0xF2F2F2), InverseOnSurface = Hex(0x0D0D24), InversePrimary = Hex(0x0000F2),
            Scrim = new Vector4(0, 0, 0, 1),
            Accent = Hex(0x8585F9),
        }
        : new Md3Scheme
        {
            IsDark = false, Seed = 0xFF0000F2,
            Primary = Hex(0x0000F2), OnPrimary = Hex(0xFFFFFF), PrimaryContainer = Hex(0xD9D9FF), OnPrimaryContainer = Hex(0x000061),
            Secondary = Hex(0x000091), OnSecondary = Hex(0xFFFFFF), SecondaryContainer = Hex(0xD6D6F2), OnSecondaryContainer = Hex(0x0D0D24),
            Tertiary = Hex(0x4A4A6A), OnTertiary = Hex(0xFFFFFF), TertiaryContainer = Hex(0xDEDEEA), OnTertiaryContainer = Hex(0x19192F),
            Error = Hex(0xB3261E), OnError = Hex(0xFFFFFF), ErrorContainer = Hex(0xF9DEDC), OnErrorContainer = Hex(0x410E0B),
            Surface = Hex(0xF2F2F2), OnSurface = Hex(0x0D0D24), SurfaceVariant = Hex(0xDEDEEA), OnSurfaceVariant = Hex(0x4A4A66),
            SurfaceContainerLowest = Hex(0xFFFFFF), SurfaceContainerLow = Hex(0xFAFAFF), SurfaceContainer = Hex(0xECECF4),
            SurfaceContainerHigh = Hex(0xF2F2F2), SurfaceContainerHighest = Hex(0xE0E0EC),
            Outline = Hex(0x77778F), OutlineVariant = Hex(0xC4C4D6),
            InverseSurface = Hex(0x19192F), InverseOnSurface = Hex(0xF2F2F2), InversePrimary = Hex(0x8585F9),
            Scrim = new Vector4(0, 0, 0, 1),
            Accent = Hex(0x0000F2),
        };
}
