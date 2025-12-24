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

    #endregion
}
