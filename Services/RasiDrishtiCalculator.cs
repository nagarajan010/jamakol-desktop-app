namespace JamakolAstrology.Services;

/// <summary>
/// Rasi drishti - the aspects SIGNS cast on one another, as distinct from graha drishti.
///
/// The rule: a movable rasi aspects the fixed rasis except the fixed one right next to it; a
/// fixed rasi aspects the movable rasis except the movable one right next to it; a dual rasi
/// aspects all the other dual rasis.
///
/// "Right next to it" means the 2nd sign from a movable rasi and the 12th from a fixed one -
/// which is the same pair seen from either end, so the relation comes out symmetric.
/// </summary>
public static class RasiDrishtiCalculator
{
    /// <summary>Movable (cara) signs: Aries, Cancer, Libra, Capricorn.</summary>
    public static bool IsMovable(int sign) => Normalise(sign) % 3 == 0;

    /// <summary>Fixed (sthira) signs: Taurus, Leo, Scorpio, Aquarius.</summary>
    public static bool IsFixed(int sign) => Normalise(sign) % 3 == 1;

    /// <summary>Dual (dvisvabhava) signs: Gemini, Virgo, Sagittarius, Pisces.</summary>
    public static bool IsDual(int sign) => Normalise(sign) % 3 == 2;

    /// <summary>
    /// Whether the sign <paramref name="from"/> casts rasi drishti on <paramref name="to"/>.
    /// Both 0-based from Aries. A sign does NOT aspect itself: occupation is a separate relation,
    /// and every caller wanting "joined with or aspecting" tests both.
    /// </summary>
    public static bool Aspects(int from, int to)
    {
        int a = Normalise(from);
        int b = Normalise(to);
        if (a == b) return false;

        if (IsDual(a)) return IsDual(b);
        if (IsMovable(a)) return IsFixed(b) && b != Normalise(a + 1);
        return IsMovable(b) && b != Normalise(a - 1); // fixed
    }

    private static int Normalise(int sign) => ((sign % 12) + 12) % 12;
}
