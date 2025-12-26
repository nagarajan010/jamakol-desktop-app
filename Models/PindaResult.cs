namespace JamakolAstrology.Models;

public class PindaResult
{
    public int RasiPinda { get; set; }
    public int GrahaPinda { get; set; }
    public int SodhyaPinda => RasiPinda + GrahaPinda;
}
