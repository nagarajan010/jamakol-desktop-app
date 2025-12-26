using System;
using System.Collections.Generic;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

public static class AmshaDevataCalculator
{
    public struct AmshaDevataResult
    {
        public string Deity { get; set; }
        public int DeityIndex { get; set; } // Index in the deity list (1-based)
        public int PartNumber { get; set; } // The Amsha part number (1-based)
    }

    public static AmshaDevataResult GetDeity(int division, double longitude, int sign)
    {
        // Degree in sign = longitude % 30
        double degreeInSign = longitude % 30;
        
        switch (division)
        {
            case 2: return GetHoraDeity(degreeInSign, sign);
            case 3: return GetDrekkanaDeity(degreeInSign, sign);
            case 4: return GetChaturthamsaDeity(degreeInSign, sign);
            case 7: return GetSaptamsaDeity(degreeInSign, sign);
            case 9: return GetNavamsaDeity(degreeInSign, sign);
            case 10: return GetDasamsaDeity(degreeInSign, sign);
            case 12: return GetDwadasamsaDeity(degreeInSign, sign);
            case 16: return GetShodasamsaDeity(degreeInSign, sign);
            case 20: return GetVimsamsaDeity(degreeInSign, sign);
            case 24: return GetSiddhamsaDeity(degreeInSign, sign);
            case 27: return GetNakshatramsaDeity(degreeInSign, sign);
            case 30: return GetTrimsamsaDeity(degreeInSign, sign);
            case 40: return GetKhavedamsaDeity(degreeInSign, sign);
            case 45: return GetAkshavedamsaDeity(degreeInSign, sign);
            case 60: return GetShashtiamsaDeity(degreeInSign, sign);
            default: return new AmshaDevataResult { Deity = "", DeityIndex = 0, PartNumber = 0 };
        }
    }

    private static bool IsOdd(int sign) => sign % 2 != 0;

    // D-2 Hora
    private static AmshaDevataResult GetHoraDeity(double deg, int sign)
    {
        bool firstHalf = deg < 15;
        // Part 1 or 2
        int part = firstHalf ? 1 : 2;
        
        if (IsOdd(sign)) 
            // 1st half: Deva (1), 2nd half: Pitri (2)
            return firstHalf ? new AmshaDevataResult { Deity = "Deva", DeityIndex = 1, PartNumber = part } 
                             : new AmshaDevataResult { Deity = "Pitri", DeityIndex = 2, PartNumber = part };
        else 
            // 1st half: Pitri (2), 2nd half: Deva (1)
            return firstHalf ? new AmshaDevataResult { Deity = "Pitri", DeityIndex = 2, PartNumber = part } 
                             : new AmshaDevataResult { Deity = "Deva", DeityIndex = 1, PartNumber = part };
    }

    // D-3 Drekkana
    private static AmshaDevataResult GetDrekkanaDeity(double deg, int sign)
    {
        int part = (int)(deg / 10); // 0, 1, 2
        int partNumber = part + 1;
        
        // 1=Movable, 2=Fixed, 0=Dual (sign % 3)
        int remainder = sign % 3;
        
        // Deities: Narada(1), Agastya(2), Durvasa(3)
        int deityIdx = 0; // 0-based
        string name = "";
        
        if (remainder == 1) // Movable
        {
            deityIdx = part switch { 0 => 0, 1 => 1, _ => 2 }; // 0,1,2
            name = part switch { 0 => "Narada", 1 => "Agastya", _ => "Durvasa" };
        }
        else if (remainder == 2) // Fixed
        {
            deityIdx = part switch { 0 => 1, 1 => 2, _ => 0 }; // 1,2,0 (Agastya, Durvasa, Narada)
            name = part switch { 0 => "Agastya", 1 => "Durvasa", _ => "Narada" };
        }
        else // Dual
        {
            deityIdx = part switch { 0 => 2, 1 => 0, _ => 1 }; // 2,0,1 (Durvasa, Narada, Agastya)
            name = part switch { 0 => "Durvasa", 1 => "Narada", _ => "Agastya" };
        }

        return new AmshaDevataResult { Deity = name, DeityIndex = deityIdx + 1, PartNumber = partNumber };
    }

    // D-4 Chaturthamsa
    private static AmshaDevataResult GetChaturthamsaDeity(double deg, int sign)
    {
        // Sanaka(1), Sananda(2), Sanatkumara(3), Sanatana(4)
        string[] deities = { "Sanaka", "Sananda", "Sanatkumara", "Sanatana" };
        int part = (int)(deg / 7.5); // 0-3
        int partNumber = part + 1;
        
        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 3 - part;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-7 Saptamsa
    private static AmshaDevataResult GetSaptamsaDeity(double deg, int sign)
    {
        // Kshaara(1), Ksheera(2)... Suddha(7)
        string[] deities = { "Kshaara", "Ksheera", "Dadhi", "Ghrita", "Ikshu Rasa", "Madya", "Suddha Jala" };
        
        double partSize = 30.0 / 7.0;
        int part = (int)(deg / partSize);
        if (part > 6) part = 6;
        int partNumber = part + 1;
        
        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 6 - part;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-9 Navamsa
    private static AmshaDevataResult GetNavamsaDeity(double deg, int sign)
    {
        string[] types = { "Deva", "Manushya", "Rakshasa" };
        double partSize = 30.0 / 9.0;
        int part = (int)(deg / partSize); // 0-8
        
        int remainder = sign % 3;
        int offset = 0;
        
        if (remainder == 1) offset = 0;      // Movable starts with Deva (index 0)
        else if (remainder == 2) offset = 1; // Fixed starts with Manushya (index 1)
        else offset = 2;                     // Dual starts with Rakshasa (index 2)
        
        int index = (offset + part) % 3;
        return new AmshaDevataResult { Deity = types[index], DeityIndex = index + 1, PartNumber = part + 1 };
    }

    // D-10 Dasamsa
    private static AmshaDevataResult GetDasamsaDeity(double deg, int sign)
    {
        string[] deities = { "Indra", "Agni", "Yama", "Rakshasa", "Varuna", "Vayu", "Kubera", "Isana", "Brahma", "Ananta" };
        int part = (int)(deg / 3.0); // 0-9
        int partNumber = part + 1;
        
        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 9 - part;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-12 Dwadasamsa
    private static AmshaDevataResult GetDwadasamsaDeity(double deg, int sign)
    {
        string[] deities = { "Ganesha", "Ashvini Kumaras", "Yama", "Sarpa" };
        int part = (int)(deg / 2.5); // 0-11
        int partNumber = part + 1;
        
        int idx = 0;
        int indexInCycle = part % 4;
        
        if (IsOdd(sign)) idx = indexInCycle;
        else idx = 3 - indexInCycle;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-16 Shodasamsa
    private static AmshaDevataResult GetShodasamsaDeity(double deg, int sign)
    {
        string[] deities = { "Brahma", "Vishnu", "Shiva", "Surya" };
        double partSize = 30.0 / 16.0;
        int part = (int)(deg / partSize); // 0-15
        int partNumber = part + 1;
        
        int idx = 0;
        int indexInCycle = part % 4;
        
        if (IsOdd(sign)) idx = indexInCycle;
        else idx = 3 - indexInCycle;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-20 Vimsamsa
    private static AmshaDevataResult GetVimsamsaDeity(double deg, int sign)
    {
        string[] deities = {
            "Kali", "Gauri", "Jaya", "Lakshmi", "Vijaya",
            "Vimala", "Sati", "Tara", "Jvalamukhi", "Sveta",
            "Lalita", "Bagalamukhi", "Pratyangira", "Shachi", "Raudri",
            "Bhavani", "Varada", "Jaya", "Tripurasundari", "Tara"
        };
        
        double partSize = 30.0 / 20.0;
        int part = (int)(deg / partSize); 
        if (part > 19) part = 19;
        int partNumber = part + 1;
        
        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 19 - part;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-24 Siddhamsa
    private static AmshaDevataResult GetSiddhamsaDeity(double deg, int sign)
    {
        string[] cycle = {
            "Skanda", "Parshudhara", "Anala", "Vishwakarma", "Bhaga", "Mitra", 
            "Maya", "Antaka", "Vrishadwaja", "Govinda", "Madana", "Bhima"
        };
        
        double partSize = 30.0 / 24.0;
        int part = (int)(deg / partSize);
        if (part > 23) part = 23;
        int partNumber = part + 1;
        
        int indexInCycle = part % 12; // 0-11
        int idx = 0;
        
        string name;
        if (IsOdd(sign)) idx = indexInCycle;
        else idx = 11 - indexInCycle;
        
        name = cycle[idx];
        return new AmshaDevataResult { Deity = name, DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-27 Nakshatramsa
    private static AmshaDevataResult GetNakshatramsaDeity(double deg, int sign)
    {
        string[] nakshatras = ZodiacUtils.NakshatraNames; // 0-26
        
        double partSize = 30.0 / 27.0;
        int part = (int)(deg / partSize);
        if (part > 26) part = 26;
        int partNumber = part + 1;
        

        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 26 - part;
        
        return new AmshaDevataResult { Deity = nakshatras[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }
    
    // D-30 Trimsamsa
    private static AmshaDevataResult GetTrimsamsaDeity(double deg, int sign)
    {
        // Custom ranges, define list for index
        // Agni=1, Vayu=2, Indra=3, Kubera=4, Varuna=5
        string name = "";
        int idx = 0;
        
        if (IsOdd(sign))
        {
            if (deg < 5) { name = "Agni"; idx = 1; }
            else if (deg < 10) { name = "Vayu"; idx = 2; }
            else if (deg < 18) { name = "Indra"; idx = 3; }
            else if (deg < 25) { name = "Kubera"; idx = 4; }
            else { name = "Varuna"; idx = 5; }
        }
        else
        {
            if (deg < 5) { name = "Varuna"; idx = 5; }
            else if (deg < 12) { name = "Kubera"; idx = 4; }
            else if (deg < 20) { name = "Indra"; idx = 3; }
            else if (deg < 25) { name = "Vayu"; idx = 2; }
            else { name = "Agni"; idx = 1; }
        }
        
        return new AmshaDevataResult { Deity = name, DeityIndex = idx, PartNumber = 1 }; // PartNumber symbolic here
    }

    // D-40 Khavedamsa
    private static AmshaDevataResult GetKhavedamsaDeity(double deg, int sign)
    {
        double partSize = 30.0 / 40.0;
        int part = (int)(deg / partSize);
        int partNumber = part + 1;
        
        string[] pair = { "Vishnu", "Chandra" };
        int idx = 0;
        
        if (IsOdd(sign)) idx = part % 2;
        else idx = (part + 1) % 2;
        
        return new AmshaDevataResult { Deity = pair[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-45 Akshavedamsa
    private static AmshaDevataResult GetAkshavedamsaDeity(double deg, int sign)
    {
        double partSize = 30.0 / 45.0;
        int part = (int)(deg / partSize);
        int partNumber = part + 1;
        
        string[] cycle = { "Brahma", "Shiva", "Vishnu" };
        int idx = 0;
        
        if (IsOdd(sign)) idx = part % 3;
        else idx = 2 - (part % 3);
        
        return new AmshaDevataResult { Deity = cycle[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }

    // D-60 Shashtiamsa
    private static AmshaDevataResult GetShashtiamsaDeity(double deg, int sign)
    {
        string[] deities = {
            "Ghora", "Rakshasa", "Deva", "Kubera", "Yaksha", "Kinnara", "Bhrashta", "Kulaghna", "Garala", "Vahni",
            "Maya", "Purishaka", "Apampati", "Marutwan", "Kaala", "Sarpa", "Amrita", "Indu", "Mridu", "Komala",
            "Heramba", "Brahma", "Vishnu", "Maheshwara", "Deva", "Ardra", "Kalinas", "Kshitisha", "Kamalakara", "Gulika",
            "Mrityu", "Kaala", "Davagni", "Ghora", "Yama", "Kantaka", "Sudha", "Amrita", "Poornachandra", "Vishadagdha",
            "Kulanasa", "Vamsakshaya", "Utpata", "Kaala", "Saumya", "Komala", "Sheetala", "Karaladamshtra", "Chandramukhi", "Praveena",
            "Kaalapavaka", "Dandayudha", "Nirmala", "Saumya", "Krura", "Atisheetala", "Amrita", "Payodhi", "Bhramana", "Chandrarekha"
        };
        
        double partSize = 30.0 / 60.0;
        int part = (int)(deg / partSize);
        if (part > 59) part = 59;
        int partNumber = part + 1;
        
        int idx = 0;
        if (IsOdd(sign)) idx = part;
        else idx = 59 - part;
        
        return new AmshaDevataResult { Deity = deities[idx], DeityIndex = idx + 1, PartNumber = partNumber };
    }
}
