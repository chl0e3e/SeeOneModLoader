using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SeeOneModLoader.Views;
using System;
using System.Linq;
using System.Reactive;

namespace SeeOneModLoader.ViewModels
{
    public class SelectModsViewModel : ViewModelBase
    {
        public string Title => "Select which mods to use";
    }
}