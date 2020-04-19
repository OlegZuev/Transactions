using System.Windows.Controls;
using Transactions.ViewModels;

namespace Transactions.Views {
    /// <summary>
    /// Interaction logic for LiteratureTypesPage.xaml
    /// </summary>
    public partial class LiteratureTypesPage : Page {
        public LiteratureTypesPage() {
            InitializeComponent();
            DataContext = new LiteratureTypesViewModel();
        }
    }
}