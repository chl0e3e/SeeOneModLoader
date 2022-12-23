using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SeeOneModLoader.Patch;
using SeeOneModLoader.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace SeeOneModLoader.ViewModels
{
    public class SelectPatchesViewModel : ViewModelBase
    {
        public class Patch : ReactiveObject
        {
            public Patch(string Name, bool IsChecked)
            {
                this.name = Name;
                this.isChecked = IsChecked;
            }

            private string name;
            public string Name
            {
                get => name;
                set => this.RaiseAndSetIfChanged(ref name, value);
            }

            private bool isChecked;
            public bool IsChecked
            {
                get => isChecked;
                set => this.RaiseAndSetIfChanged(ref isChecked, value);
            }
        }

        public string Title => "Select which patches to use";

        public ObservableCollection<Patch> Items { get; }

        public SelectPatchesViewModel()
        {
            Items = new ObservableCollection<Patch>();

            foreach (string patchName in Patcher.PATCHES.Keys)
            {
                Items.Add(new Patch(patchName, true));
            }
        }
    }
}