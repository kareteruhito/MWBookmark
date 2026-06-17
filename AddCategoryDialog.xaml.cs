using System.Windows;

namespace MWBookmark;

public partial class AddCategoryDialog : Window
{
    public string CategoryName => CategoryTextBox.Text;

    public AddCategoryDialog(string textValue="", string title = "カテゴリ追加")
    {
        InitializeComponent();
        CategoryTextBox.Text = textValue;
        this.Title = title;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CategoryTextBox.Text))
        {
            MessageBox.Show("カテゴリ名を入力してください。");
            return;
        }

        DialogResult = true;
    }
}