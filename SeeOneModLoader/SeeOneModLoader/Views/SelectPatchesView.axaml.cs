using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SeeOneModLoader.ViewModels;
using System.Threading.Tasks;

namespace SeeOneModLoader.Views
{
    public partial class SelectPatchesView : ReactiveUserControl<SelectPatchesViewModel>
    {
        public SelectPatchesView()
        {
            InitializeComponent();
        }
    }
}