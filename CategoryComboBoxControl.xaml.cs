using System.Windows;
using System.Windows.Controls;

namespace MWBookmark;

public partial class CategoryComboBoxControl : UserControl
{
    public CategoryComboBoxControl()
    {
        InitializeComponent();
    }
    private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddCategoryDialog
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var name = dialog.CategoryName;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.AddCategoryCommand.Execute(name);
        }
    }
    private void UpdateCategoryButton_Click(object sender, RoutedEventArgs e)
    {
        var x = CategoryComboBox.SelectedItem as CategoryEntity;
        if ( x is null ) return;
        string name = x.Name;

        var dialog = new AddCategoryDialog(name, "カテゴリ変更")
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }
        string newName = dialog.CategoryName;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.UpdateCategoryCommand.Execute(newName);
        }
    }
    private void CategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (sender is not ComboBox comboBox) return;
        if (comboBox.SelectedItem is not CategoryEntity category)
        {
            UpdateCategoryButton.IsEnabled = false;
            DeleteCategoryButton.IsEnabled = false;
            return;
        }

        if (category.Name == CategoryEntity.DefaultName)
        {
            UpdateCategoryButton.IsEnabled = false;
            DeleteCategoryButton.IsEnabled = false;
            return;            
        }

        UpdateCategoryButton.IsEnabled = true;
        DeleteCategoryButton.IsEnabled = true;
    }
}