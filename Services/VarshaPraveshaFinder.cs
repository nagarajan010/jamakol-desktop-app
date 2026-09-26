namespace JamakolAstrology.Services;

/// <summary>
/// Varsha pravesha - the instant the Sun returns to its natal degree and a year of life begins,
/// which opens the Tajaka annual chart. Ported from the Android app.
///
/// PVR Narasimha Rao: "At the exact moment when Sun returns to the exact position he occupied at
/// the time of a person's birth, a new year is said to commence in the life of that person."
///
/// This adds no arithmetic: <see cref="SolarReturnFinder"/> already solves the Sun back to its
/// natal longitude by bisection, since its apparent speed varies through the year and a return
/// cannot be reached by scaling. It uses the exact solve, not Rao's table-based approximation for
/// hand work, which exists only because the exact method is laborious by hand.
/// </summary>
public class VarshaPraveshaFinder
{
    /// <summary>
    /// The rule a caller must not get wrong. Rao, repeated in a footnote: "The longitude and
    /// latitude of the birthplace must be used in casting this chart, irrespective of the place of
    /// living at the commencement of the new year." The instant is a fact about the Sun and does
    /// not depend on place; the ASCENDANT cast at it does, and that is what the rule protects.
    /// </summary>
    public const string PlaceRule =
        "Cast for the birthplace coordinates, whatever the native's current residence.";

    private readonly SolarReturnFinder _returns;

    public VarshaPraveshaFinder(SolarReturnFinder returns)
    {
        _returns = returns;
    }

    /// <summary>
    /// The pravesha opening the year after <paramref name="completedYears"/> complete years of
    /// life, as a UT Julian Day. Year 0 is the birth itself. Null for a negative count.
    /// </summary>
    public double? ForYear(double birthJd, int completedYears)
    {
        if (completedYears < 0) return null;
        return _returns.JdAfterYears(birthJd, completedYears);
    }
}
