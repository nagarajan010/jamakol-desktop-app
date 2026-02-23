using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;

namespace JamakolAstrology.Controls;

public partial class NavataraDetailsPanel : UserControl
{
    private readonly DnaAstrologyService _dnaService = new DnaAstrologyService();

    public NavataraDetailsPanel()
    {
        InitializeComponent();
        InitializeReferenceCombo();
    }

    private class PlanetOption
    {
        public string Name { get; set; } = "";
        public int Id { get; set; } // -1 for Lagna, else SwissEph id
    }

    private ChartData? _currentChart;

    private void InitializeReferenceCombo()
    {
        var options = new List<PlanetOption>
        {
            new PlanetOption { Name = "Lagna (Ascendant)", Id = -1 },
            new PlanetOption { Name = "Sun", Id = SwissEphNet.SwissEph.SE_SUN },
            new PlanetOption { Name = "Moon", Id = SwissEphNet.SwissEph.SE_MOON },
            new PlanetOption { Name = "Mars", Id = SwissEphNet.SwissEph.SE_MARS },
            new PlanetOption { Name = "Mercury", Id = SwissEphNet.SwissEph.SE_MERCURY },
            new PlanetOption { Name = "Jupiter", Id = SwissEphNet.SwissEph.SE_JUPITER },
            new PlanetOption { Name = "Venus", Id = SwissEphNet.SwissEph.SE_VENUS },
            new PlanetOption { Name = "Saturn", Id = SwissEphNet.SwissEph.SE_SATURN },
            new PlanetOption { Name = "Rahu", Id = SwissEphNet.SwissEph.SE_MEAN_NODE },
            new PlanetOption { Name = "Ketu", Id = SwissEphNet.SwissEph.SE_TRUE_NODE }
        };

        ReferencePlanetCombo.ItemsSource = options;
        
        // Default to Moon as requested
        ReferencePlanetCombo.SelectedValue = SwissEphNet.SwissEph.SE_MOON;
    }

    public void UpdateChart(ChartData? chart)
    {
        _currentChart = chart;
        CalculateNavatara();
    }

    private void ReferencePlanetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CalculateNavatara();
    }

    private void ShowSpecialPointsCheckBox_CheckedChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (ColSpecialPoints != null)
        {
            ColSpecialPoints.Visibility = (ShowSpecialPointsCheckBox.IsChecked == true) 
                ? System.Windows.Visibility.Visible 
                : System.Windows.Visibility.Collapsed;
        }
    }

    private void CalculateNavatara()
    {
        if (_currentChart == null || ReferencePlanetCombo.SelectedValue == null)
        {
            NavataraGrid.ItemsSource = null;
            return;
        }

        int selectedId = (int)ReferencePlanetCombo.SelectedValue;
        
        // Find reference nakshatra
        int referenceNakshatra = 1; // 1-27

        if (selectedId == -1) // Lagna
        {
            // Use Ascendant Degree to find Nakshatra
            referenceNakshatra = ZodiacUtils.DegreeToNakshatra(_currentChart.AscendantDegree);
        }
        else
        {
            // Find planet in chart
            // Note: Planet enum in ChartData.Planets might imply we need to map ID to Enum or search by ID if available. 
            // The models usually use SwissEph IDs or custom Enum. 
            // Let's look at ChartData.Planets which is List<PlanetInfo>. PlanetInfo.Planet is an Enum 'Planet'.
            // We need to cast our ID to Planet enum if compatible, or map it.
            // SwissEph IDs: Sun=0, Moon=1... 
            // Planet Enum: likely matches. 
            
            Planet pEnum = (Planet)selectedId;
            var planet = _currentChart.Planets.FirstOrDefault(p => p.Planet == pEnum);
            if (planet != null)
            {
                referenceNakshatra = planet.Nakshatra;
            }
        }

        var navataraList = GenerateNavataraList(referenceNakshatra);
        NavataraGrid.ItemsSource = navataraList;
    }

    private List<NavataraRow> GenerateNavataraList(int referenceNakshatra)
    {
        var list = new List<NavataraRow>();
        
        // Sequence of Navatara names
        string[] taraNames = { 
            "Janma", "Sampat", "Vipat", "Kshema", "Pratyak", 
            "Saadhana", "Naidhana/Vadha", "Mitra", "Parama Mitra" 
        };

        // We need to list 27 Nakshatras starting from reference? 
        // Request says: "list all 27 nakstras from any selected planet"
        // "1 Janma ... Nak 1"
        // This implies the table starts with the Janma nakshatra (the reference nakshatra) as row 1.
        
        for (int i = 0; i < 27; i++)
        {
            // Calculate which nakshatra this is
            // If ref is 5, i=0 => 5. i=1 => 6. i=26 => 4.
            // (ref - 1 + i) % 27 + 1
            int nakshatraNum = (referenceNakshatra - 1 + i) % 27 + 1;
            
            // Tara index (0-8)
            int taraIndex = i % 9;
            string taraName = taraNames[taraIndex];
            
            // Nakshatra Name
            string nakName = ZodiacUtils.GetNakshatraName(nakshatraNum);
            
            // Nak Lord
            // Need a way to get Lord. 
            // ZodiacUtils.GetNakshatraLord(nakshatraNum)? OR standard sequence:
            // Ketu, Venus, Sun, Moon, Mars, Rahu, Jupiter, Saturn, Mercury (repeat)
            // 1=Ashwini(Ketu), 2=Bharani(Venus)...
            // Let's use ZodiacUtils or implement basic sequence if missing.
            // Standard Vimshottari sequence 1..27 maps to lords.
            Planet lord = GetNakshatraLord(nakshatraNum);
            string lordName = ZodiacUtils.GetPlanetName(lord);

            // Planets in this Nakshatra
            var occupants = new List<string>();
            
            // Check Lagna
            if (ZodiacUtils.DegreeToNakshatra(_currentChart.AscendantDegree) == nakshatraNum)
            {
                occupants.Add("Lagna");
            }
            
            // Check Planets
            foreach (var p in _currentChart.Planets)
            {
                if (p.Nakshatra == nakshatraNum)
                {
                    occupants.Add(p.Name); // Or GetPlanetName(p.Planet)
                }
            }
            
            var dnaLord = _dnaService.GetDnaLord(nakshatraNum);
            string dnaLordName = ZodiacUtils.GetPlanetName(dnaLord);
            
            // Calculate Special Point
            string specialPoint = GetSpecialPoint(i + 1);

            list.Add(new NavataraRow
            {
                Index = i + 1,
                NavataraName = taraName,
                NakshatraName = $"{nakName} ({nakshatraNum})",
                NakshatraLord = lordName,
                DnaLord = dnaLordName,
                SpecialPoint = specialPoint,
                PlanetsInNakshatra = string.Join(", ", occupants)
            });
        }
        
        return list;
    }

    private Planet GetNakshatraLord(int nakshatra)
    {
        // 1 Ashwini - Ketu
        // 2 Bharani - Venus
        // 3 Krittika - Sun
        // 4 Rohini - Moon
        // 5 Mrigashirsha - Mars
        // 6 Ardra - Rahu
        // 7 Punarvasu - Jupiter
        // 8 Pushya - Saturn
        // 9 Ashlesha - Mercury
        
        int remainder = (nakshatra - 1) % 9;
        return remainder switch
        {
            0 => Planet.Ketu, // 1, 10, 19
            1 => Planet.Venus,
            2 => Planet.Sun,
            3 => Planet.Moon,
            4 => Planet.Mars,
            5 => Planet.Rahu,
            6 => Planet.Jupiter,
            7 => Planet.Saturn,
            8 => Planet.Mercury,
            _ => Planet.Ketu
        };
    }

    private string GetSpecialPoint(int index)
    {
        // Cycle 1: 1-12 match Houses 1-12
        if (index >= 1 && index <= 12)
        {
            return GetHouseDescription(index);
        }
        
        // Break: 13, 14
        if (index == 13) return "Right Eye (Destiny)";
        if (index == 14) return "Left Eye (Emotion)";
        
        // Cycle 2: From 15, restarts from House 1
        if (index >= 15)
        {
            // Special Case: 27
            if (index == 27) return "The Third Eye (Shiva's Nakshatra)";

            // 15 -> 1, 16 -> 2...
            int houseNum = index - 14; 
            if (houseNum <= 12)
            {
                 return $"Cycle 2: {GetHouseDescription(houseNum)}";
            }
            return ""; 
        }
        
        return "";
    }
    
    private string GetHouseDescription(int houseNum)
    {
        return houseNum switch
        {
            1 => "1H: Self",
            2 => "2H: Speech, Food",
            3 => "3H: Communication, Population",
            4 => "4H: Home, Vehicles",
            5 => "5H: Children, Creativity",
            6 => "6H: Crying, Conflicts",
            7 => "7H: Death, Protection",
            8 => "8H: Transformations",
            9 => "9H: Protection (Father, Dharma)",
            10 => "10H: Career",
            11 => "11H: Friends, Associates",
            12 => "12H: Special Friend, Commitment",
            _ => $"House {houseNum}"
        };
    }
}

public class NavataraRow
{
    public int Index { get; set; }
    public string NavataraName { get; set; } = "";
    public string NakshatraName { get; set; } = "";
    public string NakshatraLord { get; set; } = "";
    public string DnaLord { get; set; } = "";
    public string SpecialPoint { get; set; } = "";
    public string PlanetsInNakshatra { get; set; } = "";
}
