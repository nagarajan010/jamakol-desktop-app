// Test script to verify Jama Graha calculation
// Run with: dotnet script test_jamagraha.csx

Console.WriteLine("=== Jama Graha Calculation Test ===\n");

// Test data
var testTime = new DateTime(2025, 12, 22, 22, 58, 0);  // Monday at 22:58
var dayOfWeek = testTime.DayOfWeek;
Console.WriteLine($"Test Time: {testTime:yyyy-MM-dd HH:mm}");
Console.WriteLine($"Day of Week: {dayOfWeek} (Monday = Moon day)");

// Day lord calculation
string dayLord = dayOfWeek switch
{
    DayOfWeek.Sunday => "Sun",
    DayOfWeek.Monday => "Moon",
    DayOfWeek.Tuesday => "Mars",
    DayOfWeek.Wednesday => "Mercury",
    DayOfWeek.Thursday => "Jupiter",
    DayOfWeek.Friday => "Venus",
    DayOfWeek.Saturday => "Saturn",
    _ => "Sun"
};
Console.WriteLine($"Day Lord: {dayLord}");

// Time calculation
double timeInMinutes = testTime.Hour * 60 + testTime.Minute;
Console.WriteLine($"\nTime in minutes from midnight: {timeInMinutes}");

// Wrap time
double wrappedTime = timeInMinutes;
if (wrappedTime < 360)
{
    wrappedTime += 720;
    Console.WriteLine($"Time < 360 (6 AM), adding 720: {wrappedTime}");
}
else if (wrappedTime >= 1080)
{
    wrappedTime -= 720;
    Console.WriteLine($"Time >= 1080 (6 PM), subtracting 720: {wrappedTime}");
}
else
{
    Console.WriteLine($"Time in range [360, 1080), no wrap needed: {wrappedTime}");
}

// Period ranges
var periods = new[] {
    (360.0, 450.0, "J1: 6:00-7:30 AM"),
    (450.0, 540.0, "J2: 7:30-9:00 AM"),
    (540.0, 630.0, "J3: 9:00-10:30 AM"),
    (630.0, 720.0, "J4: 10:30-12:00 PM"),
    (720.0, 810.0, "J5: 12:00-1:30 PM"),
    (810.0, 900.0, "J6: 1:30-3:00 PM"),
    (900.0, 990.0, "J7: 3:00-4:30 PM"),
    (990.0, 1080.0, "J8: 4:30-6:00 PM"),
};

Console.WriteLine($"\nFinding period for wrapped time {wrappedTime}:");
int currentPeriod = 0;
for (int i = 0; i < periods.Length; i++)
{
    var (start, end, name) = periods[i];
    bool inRange = wrappedTime >= start && wrappedTime < end;
    Console.WriteLine($"  Period {i} ({name}): [{start}, {end}) -> {(inRange ? "MATCH!" : "no")}");
    if (inRange)
    {
        currentPeriod = i;
    }
}
Console.WriteLine($"\nCurrent Period Index: {currentPeriod}");

// Moon table (from PHP)
var moonTable = new string[,] {
    // J1 (index 0): ['Pisces' => 'Moon', 'Capricorn' => 'Snake', ...]
    { "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn" },
    // J2 (index 1): ['Pisces' => 'Saturn', ...]
    { "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus" },
    // J3 (index 2): ['Pisces' => 'Venus', ...]
    { "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter", "Mercury" },
    // J4 (index 3): ['Pisces' => 'Mercury', ...]
    { "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars", "Jupiter" },
    // J5 (index 4): ['Pisces' => 'Jupiter', ...]
    { "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun", "Mars" },
    // J6 (index 5): ['Pisces' => 'Mars', ...]
    { "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake", "Sun" },
    // J7 (index 6): ['Pisces' => 'Sun', ...]
    { "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon", "Snake" },
    // J8 (index 7): ['Pisces' => 'Snake', ...]
    { "Snake", "Sun", "Mars", "Jupiter", "Mercury", "Venus", "Saturn", "Moon" }
};

// Signs in order (from PHP key order)
var signs = new[] { "Pisces", "Capricorn", "Sagittarius", "Libra", "Virgo", "Cancer", "Gemini", "Aries" };
var signNumbers = new[] { 12, 10, 9, 7, 6, 4, 3, 1 };

Console.WriteLine($"\n=== Moon Table Row for Period {currentPeriod} ===");
for (int i = 0; i < 8; i++)
{
    string planet = moonTable[currentPeriod, i];
    string sign = signs[i];
    int house = signNumbers[i];
    Console.WriteLine($"  {sign} (House {house}): {planet}");
}

// Verify what should be in Pisces
string piscePlanet = moonTable[currentPeriod, 0];
Console.WriteLine($"\n=== RESULT ===");
Console.WriteLine($"For Monday (Moon) at {testTime:HH:mm}, Period {currentPeriod}:");
Console.WriteLine($"Pisces (House 12) should have: {piscePlanet}");

if (currentPeriod == 3)
{
    Console.WriteLine("\nExpected: Mercury (J4)");
}
else if (currentPeriod == 2)
{
    Console.WriteLine("\nExpected: Venus (J3)");
}
