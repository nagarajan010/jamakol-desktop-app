using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JamakolAstrology.Models;
using JamakolAstrology.Services;
using ICSharpCode.AvalonEdit;

namespace JamakolAstrology.Controls
{
    public partial class LearningModulePanel : UserControl
    {
        private LearningNotesService _notesService;
        private List<LearningCategory> _categories;
        private LearningNote _currentNote;
        private bool _isEditMode = false;
        private bool _isDirty = false;

        public LearningModulePanel()
        {
            InitializeComponent();
            _notesService = new LearningNotesService();
            _categories = new List<LearningCategory>();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            ClearEditor();
            _categories = _notesService.LoadCategories();
            CategoriesCombo.ItemsSource = null;
            CategoriesCombo.ItemsSource = _categories;
            if (_categories.Any())
            {
                CategoriesCombo.SelectedIndex = 0;
            }
        }

        private void CategoriesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesCombo.SelectedItem is LearningCategory selectedCategory)
            {
                NotesCombo.ItemsSource = null;
                NotesCombo.ItemsSource = selectedCategory.Notes;
                if (selectedCategory.Notes.Any())
                {
                    NotesCombo.SelectedIndex = 0;
                }
                else
                {
                    ClearEditor();
                }
            }
            else
            {
                NotesCombo.ItemsSource = null;
                ClearEditor();
            }
        }

        private void NotesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotesCombo.SelectedItem is LearningNote note)
            {
                SaveCurrentNoteIfDirty();
                _currentNote = note;
                LoadNoteToEditor(note);
            }
            else
            {
                ClearEditor();
            }
        }

        private void LoadNoteToEditor(LearningNote note)
        {
            if (NoteDetailsArea != null) NoteDetailsArea.IsEnabled = true;
            if (EditorArea != null) EditorArea.IsEnabled = true;
            TxtNoteTitle.Text = note.Title;
            MarkdownEditor.Text = note.Content;
            UpdatePreview();
            _isDirty = false;
        }

        private void ClearEditor()
        {
            _currentNote = null;
            TxtNoteTitle.Text = string.Empty;
            MarkdownEditor.Text = string.Empty;
            MarkdownPreview.Markdown = "Select a note to view or edit.";
            if (NoteDetailsArea != null) NoteDetailsArea.IsEnabled = false;
            if (EditorArea != null) EditorArea.IsEnabled = false;
            _isDirty = false;
        }

        private void UpdatePreview()
        {
            MarkdownPreview.Markdown = string.IsNullOrWhiteSpace(MarkdownEditor.Text) ? " " : MarkdownEditor.Text;
        }

        private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = !_isEditMode;
            if (_isEditMode)
            {
                BtnToggleEdit.Content = "Preview";
                MarkdownPreview.Visibility = Visibility.Collapsed;
                EditGrid.Visibility = Visibility.Visible;
            }
            else
            {
                BtnToggleEdit.Content = "Edit";
                UpdatePreview();
                MarkdownPreview.Visibility = Visibility.Visible;
                EditGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void MarkdownEditor_TextChanged(object sender, EventArgs e)
        {
            if (_currentNote != null)
            {
                _isDirty = true;
            }
        }

        private void TxtNoteTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentNote != null)
            {
                _isDirty = true;
            }
        }

        private void SaveCurrentNoteIfDirty()
        {
            if (_isDirty && _currentNote != null)
            {
                _currentNote.Title = TxtNoteTitle.Text;
                _currentNote.Content = MarkdownEditor.Text;
                _currentNote.UpdatedAt = DateTime.Now;
                _notesService.SaveCategories(_categories);
                _isDirty = false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNote != null)
            {
                SaveCurrentNoteIfDirty();
                
                // Refresh Combo to show updated title
                NotesCombo.Items.Refresh();
                MessageBox.Show("Note saved successfully.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            // Ideally an input dialog, but creating default for simplicity
            var newCategory = new LearningCategory { Name = "New Category " + (_categories.Count + 1) };
            _categories.Add(newCategory);
            _notesService.SaveCategories(_categories);
            
            CategoriesCombo.ItemsSource = null;
            CategoriesCombo.ItemsSource = _categories;
            CategoriesCombo.SelectedItem = newCategory;
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            LearningCategory targetCategory = CategoriesCombo.SelectedItem as LearningCategory;

            if (targetCategory == null)
            {
                if (_categories.Count == 0)
                {
                    BtnAddCategory_Click(null, null);
                }
                targetCategory = _categories.First();
                CategoriesCombo.SelectedItem = targetCategory;
            }

            var newNote = new LearningNote { Title = "New Note", Content = "# New Note" };
            targetCategory.Notes.Add(newNote);
            _notesService.SaveCategories(_categories);

            NotesCombo.ItemsSource = null;
            NotesCombo.ItemsSource = targetCategory.Notes;
            NotesCombo.SelectedItem = newNote;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (NotesCombo.SelectedItem is LearningNote note)
            {
                var result = MessageBox.Show($"Delete note '{note.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    var parentCategory = CategoriesCombo.SelectedItem as LearningCategory;
                    if (parentCategory != null)
                    {
                        parentCategory.Notes.Remove(note);
                        _notesService.SaveCategories(_categories);
                        
                        NotesCombo.ItemsSource = null;
                        NotesCombo.ItemsSource = parentCategory.Notes;
                        
                        if (parentCategory.Notes.Any())
                            NotesCombo.SelectedIndex = 0;
                        else
                            ClearEditor();
                    }
                }
            }
            else if (CategoriesCombo.SelectedItem is LearningCategory cat)
            {
                var result = MessageBox.Show($"Delete category '{cat.Name}' and all its notes?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _categories.Remove(cat);
                    _notesService.SaveCategories(_categories);
                    LoadData();
                }
            }
        }

        // --- Toolbar Formatting Handlers ---
        private void InsertMarkdown(string prefix, string suffix, string defaultText)
        {
            var editor = MarkdownEditor;
            
            if (editor.SelectionLength > 0)
            {
                string selectedText = editor.SelectedText;
                string replacement = prefix + selectedText + suffix;
                editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, replacement);
            }
            else
            {
                string textToInsert = prefix + defaultText + suffix;
                editor.Document.Insert(editor.SelectionStart, textToInsert);
                editor.Select(editor.SelectionStart - suffix.Length - defaultText.Length, defaultText.Length);
            }
            editor.Focus();
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e) => InsertMarkdown("**", "**", "bold text");
        private void BtnItalic_Click(object sender, RoutedEventArgs e) => InsertMarkdown("*", "*", "italic text");
        private void BtnHeading_Click(object sender, RoutedEventArgs e) => InsertMarkdown("# ", "", "Heading");
        private void BtnList_Click(object sender, RoutedEventArgs e) => InsertMarkdown("- ", "", "Item");
        private void BtnLink_Click(object sender, RoutedEventArgs e) => InsertMarkdown("[", "](http://url)", "link text");
    }
}
