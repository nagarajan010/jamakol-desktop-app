using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JamakolAstrology.Models;

namespace JamakolAstrology.Services
{
    public class LearningNotesService
    {
        private readonly string _dataDirectory;
        private readonly string _filePath;

        public LearningNotesService()
        {
            _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JamakolAstrology");
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            _filePath = Path.Combine(_dataDirectory, "LearningNotes.json");
        }

        public List<LearningCategory> LoadCategories()
        {
            if (!File.Exists(_filePath))
            {
                // Create a default category if the file doesn't exist
                var defaultCategories = new List<LearningCategory>
                {
                    new LearningCategory
                    {
                        Name = "General Notes",
                        Notes = new List<LearningNote>
                        {
                            new LearningNote
                            {
                                Title = "Welcome to Learning Notes",
                                Content = "# Welcome!\n\nThis is a sample learning note. You can write your notes in **Markdown** format.\n\nEnjoy structured learning right beside your chart analysis!"
                            }
                        }
                    }
                };
                SaveCategories(defaultCategories);
                return defaultCategories;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<LearningCategory>>(json) ?? new List<LearningCategory>();
            }
            catch (Exception)
            {
                // In case of error (corrupted JSON, etc), return empty
                return new List<LearningCategory>();
            }
        }

        public void SaveCategories(List<LearningCategory> categories)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(categories, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // Optionally log the exception here
                Console.WriteLine($"Error saving learning notes: {ex.Message}");
            }
        }
    }
}
