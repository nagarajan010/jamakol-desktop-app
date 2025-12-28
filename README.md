# Jamakol Astrology Software

A comprehensive desktop application for **Vedic Astrology**, specialized in **Jamakol Prasanam** (ஜாமக்கோள் பிரசன்னம்) and detailed **Birth Chart** analysis. Built with WPF and C# .NET, utilizing the Swiss Ephemeris for high-precision astronomical calculations.

![Jamakol Astrology](https://via.placeholder.com/800x400?text=Jamakol+Astrology+Desktop+App)

## ✨ Key Features

### 🕉️ Jamakol Prasanam
The core feature of this application is the robust **Jamakol Prasanam** module, designed for instant astrological queries.
* **Real-time Chart**: Interactive South Indian style chart with **(optional) Fixed Sign Boxes** support.
* **Special Points Calculation**:
  * **Aarudam (AR)**: Calculated based on the exact minute of the query (5-minute mapping).
  * **Udayam (UD)**: Calculated based on the Ascendant (Lagna) sign.
  * **Kavippu (KV)**: Derived from the interaction between Aarudam and Udayam.
  * **Jamakol Values**: Unique scoring system for Houses, Planets, Lagna, Surya, and Chandra.
* **Jama Grahas**: Accurate positions of the 8 invisible planets (Yama Shukra, Yama Guru, etc.) dynamically calculated based on time.
* **Live Clock**: Auto-updating timer for instant "right-now" queries.

### 🌌 Birth Chart Analysis
A full-featured horoscope generation engine powered by **Swiss Ephemeris**.
* **High Precision**: Planetary longitudes calculated to the second.
* **Ayanamsa Support**: Over 30+ Ayanamsas supported including:
  * Lahiri (Chitrapaksha)
  * KP (Krishnamurti)
  * Raman
  * Fagan-Bradley
  * True Chitra / True Revati
* **Divisional Charts (Vargas)**: Complete support for all major Vargas:
  * D1 (Rasi), D2 (Hora), D3 (Drekkana), D4 (Chaturthamsha), D7 (Saptamia), D9 (Navamsa)
  * D10 (Dasamsa), D12 (Dwadasamsa), D16 (Shodashamsa), D20, D24, D27, D30, D40, D45, D60.
* **Ashtakavarga**:
  * **Bhinna Ashtakavarga (BAV)** for all 7 planets.
  * **Samudaya Ashtakavarga (SAV)** total points.
* **Vimshottari Dasha**: Hierarchical view of Dasha, Bhukti, and Antara periods.
* **KP Astrology (Krishnamurti Paddhati)**:
  * KP Chart and Cusp details (Placidus House System).
  * Lord hierarchy: Sign Lord, Star Lord, Sub Lord, Sub-Sub Lord.
* **Amsha Devata**: Detailed analysis of Amsha Devata deities (e.g., Agni, Vayu, Indra for D60).
* **Jaimini Karakas**: Automatic calculation of 7 Karakas (Atmakaraka, Amatyakaraka, etc.).

### 🗓️ Panchanga (Almanac)
Real-time calculation of the five limbs of time:
1.  **Tithi**: Lunar day (e.g., Shukla Paksha Prathamai).
2.  **Vara**: Weekday.
3.  **Nakshatra**: Star constellation.
4.  **Yoga**: Nithya Yoga.
5.  **Karana**: Half-tithi.
* **Extras**: **Kala Hora**, **Nazhikai**, and **Tamil Month/Date** calculation.

### 🛠️ Tools & Utilities
* **Save & Load**: Store unlimited charts locally (JSON format).
* **Organization**: Group charts by **Categories** and **Tags** for easy retrieval.
* **Import/Export**: Backup your entire chart database to a single JSON file or share with others.
* **Settings & Customization**:
  * Visual: Adjustable font sizes for Tables, Charts, and Inputs.
  * Computation: Configurable Sunrise modes (Tip/Center, Apparent/True).
  * Usage: Toggle "Hide Degrees" for cleaner presentations.

## 🏗️ Technical Architecture

This project is built using **C# .NET 6.0 (WPF)** following the **MVVM** pattern.

### Project Structure
* **`MainWindow.xaml`**: The application shell, orchestrating navigation between tabs (Birth Chart vs. Jamakol).
* **`Controls/`**: Reusable UI components.
  * `JamakolChart.xaml`: The custom canvas-based drawing logic for the South Indian chart.
  * `BirthInputPanel.xaml`: Data entry form for birth details.
  * `*DetailsPanel.xaml`: Modular panels for displaying specific astrological data (KP, Ashtakavarga, etc.).
* **`Services/`**: The core business logic layer.
  * **`ChartOrchestratorService.cs`**: The main director that coordinates calls to all other calculators.
  * **`EphemerisService.cs`**: Integration wrapper for `SwissEphNet` (swedll32.dll/swedll64.dll).
  * **`JamakolCalculator.cs`**: Implements the unique arithmetic specific to Jamakol Prasanam.
  * **`PanchangaCalculator.cs`**: Logic for Tithi, Nakshatra, and Yoga calculations.
* **`Models/`**: Data structures.
  * `ChartData.cs`: The universal object holding all calculated planetary and house data.
  * `AppSettings.cs`: Manages user preferences and persistent storage.

### Data Storage
* Charts and user settings are stored in `%LOCALAPPDATA%\JamakolAstrology\`.
* Data format: **JSON**.

## 💻 Tech Stack
* **Language**: C# 10.0 / .NET 6.0
* **Framework**: WPF (Windows Presentation Foundation)
* **Libraries**:
  * `SwissEphNet`: .NET wrapper for the Swiss Ephemeris.
  * `System.Text.Json`: High-performance JSON serialization.

## 🚀 Installation & Build

1. **Clone the repository**:
   ```bash
   git clone https://github.com/nagarajan010/jamakol-desktop-app.git
   ```
2. **Prerequisites**:
   * Visual Studio 2022 (Community or higher).
   * .NET 6.0 Desktop Runtime.
3. **Ephemeris Files**:
   * The application requires standard Swiss Ephemeris files (`sea_*.se1`, `sefstars.txt`).
   * These should be placed in the `ephe/` directory in the build output or project root.
4. **Build**:
   * Open `JamakolAstrology.sln`.
   * select **Debug** or **Release** mode.
   * `Ctrl + Shift + B` to build.

## 📋 Requirements
* **OS**: Windows 10 or Windows 11 (64-bit recommended).
* **Screen Resolution**: Minimum 1366x768 (1920x1080 recommended for best experience).

---
*Developed by Nagarajan*
