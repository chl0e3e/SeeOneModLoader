using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SeeOneModLoader.ViewModels;
using System.Threading.Tasks;

namespace SeeOneModLoader.Views
{
    public partial class SelectOutputDirectoryView : ReactiveUserControl<SelectOutputDirectoryViewModel>
    {
        public SelectOutputDirectoryView()
        {
            InitializeComponent();
        }

        public bool CanProceed()
        {
            if (this.ViewModel != null)
            {
                return !this.ViewModel.Error;
            }
            else
            {
                return false;
            }
        }
    }
}