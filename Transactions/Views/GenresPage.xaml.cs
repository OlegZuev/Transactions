using System.Windows.Controls;
using Transactions.ViewModels;

namespace Transactions.Views {
    /// <summary>
    /// Interaction logic for GenresPage.xaml
    /// </summary>
    public partial class GenresPage : Page {
        public GenresPage() {
            InitializeComponent();
            DataContext = new GenresViewModel();
        }
    }
}