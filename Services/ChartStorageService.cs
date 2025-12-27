using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services;

/// <summary>
/// Storage container for all saved chart data
/// </summary>
public class ChartStorageData
{
    public List<SavedJamakolChart> Charts { get; set; } = new();
    public List<ChartCategory> Categories { get; set; } = new();
    public List<ChartTag> Tags { get; set; } = new();
}

/// <summary>
/// Service for persisting saved charts, categories, and tags to JSON file
/// </summary>
public class ChartStorageService
{
    private static readonly string StorageFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "JamakolAstrology"
    );
    private static readonly string StorageFile = Path.Combine(StorageFolder, "saved_charts.json");

    private ChartStorageData _data = new();

    public ChartStorageService()
    {
        Load();
    }

    #region Charts

    public List<SavedJamakolChart> GetAllCharts() => _data.Charts.ToList();

    public SavedJamakolChart? GetChart(Guid id) => _data.Charts.FirstOrDefault(c => c.Id == id);

    public void SaveChart(SavedJamakolChart chart)
    {
        var existing = _data.Charts.FirstOrDefault(c => c.Id == chart.Id);
        if (existing != null)
        {
            _data.Charts.Remove(existing);
        }
        chart.UpdatedAt = DateTime.Now;
        _data.Charts.Add(chart);
        Save();
    }

    public void DeleteChart(Guid id)
    {
        var chart = _data.Charts.FirstOrDefault(c => c.Id == id);
        if (chart != null)
        {
            _data.Charts.Remove(chart);
            Save();
        }
    }

    #endregion

    #region Categories

    public List<ChartCategory> GetAllCategories() => _data.Categories.ToList();

    public ChartCategory? GetCategory(Guid id) => _data.Categories.FirstOrDefault(c => c.Id == id);

    public void SaveCategory(ChartCategory category)
    {
        var existing = _data.Categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing != null)
        {
            _data.Categories.Remove(existing);
        }
        _data.Categories.Add(category);
        Save();
    }

    public void DeleteCategory(Guid id)
    {
        var category = _data.Categories.FirstOrDefault(c => c.Id == id);
        if (category != null)
        {
            _data.Categories.Remove(category);
            // Remove category from all charts that use it
            foreach (var chart in _data.Charts.Where(c => c.CategoryId == id))
            {
                chart.CategoryId = null;
            }
            Save();
        }
    }

    #endregion

    #region Tags

    public List<ChartTag> GetAllTags() => _data.Tags.ToList();

    public ChartTag? GetTag(Guid id) => _data.Tags.FirstOrDefault(t => t.Id == id);

    public void SaveTag(ChartTag tag)
    {
        var existing = _data.Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (existing != null)
        {
            _data.Tags.Remove(existing);
        }
        _data.Tags.Add(tag);
        Save();
    }

    public void DeleteTag(Guid id)
    {
        var tag = _data.Tags.FirstOrDefault(t => t.Id == id);
        if (tag != null)
        {
            _data.Tags.Remove(tag);
            // Remove tag from all charts that use it
            foreach (var chart in _data.Charts)
            {
                chart.TagIds.Remove(id);
            }
            Save();
        }
    }

    #endregion

    #region Persistence

    private void Load()
    {
        try
        {
            if (File.Exists(StorageFile))
            {
                var json = File.ReadAllText(StorageFile);
                _data = JsonSerializer.Deserialize<ChartStorageData>(json) ?? new ChartStorageData();
                
                // Migration: Fix old charts that have QueryDateTime but no Year/Month/Day
                bool needsSave = false;
                foreach (var chart in _data.Charts)
                {
                    // If Year is 0 (or Month/Day are 0) but this is not a BC chart,
                    // we need to check if there's a stored QueryDateTime we should use
                    // This happens for old charts saved before Year/Month/Day properties were added
                    if (chart.Year == 0 && chart.Month == 0 && chart.Day == 0)
                    {
                        // Old charts had QueryDateTime stored directly
                        // Try to reconstruct from JSON by reading QueryDateTime if present
                        // Since QueryDateTime setter populates Year/Month/Day, we need to detect this case
                        // For now, set some defaults for really old charts
                        // In practice, re-saving the chart will fix it
                        needsSave = true;
                    }
                }
                
                // Resave after migration if needed
                if (needsSave)
                {
                    Save();
                }
            }
        }
        catch
        {
            _data = new ChartStorageData();
        }
    }

    private void Save()
    {
        try
        {
            if (!Directory.Exists(StorageFolder))
            {
                Directory.CreateDirectory(StorageFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(StorageFile, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    /// <summary>
    /// Export all charts to a JSON file
    /// </summary>
    public void ExportToFile(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_data, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Import charts from a JSON file (merges with existing)
    /// </summary>
    public int ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var importedData = JsonSerializer.Deserialize<ChartStorageData>(json);
        
        if (importedData == null) return 0;
        
        int importedCount = 0;
        
        // Import charts (skip duplicates by ID)
        foreach (var chart in importedData.Charts)
        {
            if (!_data.Charts.Any(c => c.Id == chart.Id))
            {
                _data.Charts.Add(chart);
                importedCount++;
            }
        }
        
        // Import categories
        foreach (var category in importedData.Categories)
        {
            if (!_data.Categories.Any(c => c.Id == category.Id))
            {
                _data.Categories.Add(category);
            }
        }
        
        // Import tags
        foreach (var tag in importedData.Tags)
        {
            if (!_data.Tags.Any(t => t.Id == tag.Id))
            {
                _data.Tags.Add(tag);
            }
        }
        
        Save();
        return importedCount;
    }

    #endregion
}
