namespace JamakolAstrology.Models;

/// <summary>
/// House calculation systems supported by Swiss Ephemeris
/// </summary>
public enum HouseSystem
{
    /// <summary>Placidus (KP/Krishnamoorthy) - Default</summary>
    Placidus = 'P',
    
    /// <summary>Koch houses</summary>
    Koch = 'K',
    
    /// <summary>Porphyry houses (Sripathi)</summary>
    Porphyry = 'O',
    
    /// <summary>Regiomontanus houses</summary>
    Regiomontanus = 'R',
    
    /// <summary>Campanus houses</summary>
    Campanus = 'C',
    
    /// <summary>Equal houses (30° each, cusp 1 = ASC degree)</summary>
    Equal = 'A',
    
    /// <summary>Whole Sign (Each rasi is a house)</summary>
    WholeSign = 'W',
    
    /// <summary>Alcabitus houses</summary>
    Alcabitus = 'B',
    
    /// <summary>Polich/Page (Topocentric) houses</summary>
    Topocentric = 'T',
    
    /// <summary>Axial rotation system houses</summary>
    Axial = 'X'
}
