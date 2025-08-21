using System.Windows;
using System.Windows.Controls;

namespace CeMCP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            // Handle button click event
            if (sender is Button button)
            {
                button.Content = "Clicked!";
            }
        }
    }
}
