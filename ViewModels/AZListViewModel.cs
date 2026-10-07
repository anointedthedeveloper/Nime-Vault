using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class AZListViewModel : BaseViewModel
    {
        private readonly IAnimeSearchService _search;
        private bool _isLoading;
        private string _selectedLetter = "A";

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public string SelectedLetter { get => _selectedLetter; set { SetField(ref _selectedLetter, value); _ = LoadAsync(); } }

        public ObservableCollection<Anime> Anime { get; } = new();
        public string[] Letters { get; } = { "A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z","#" };

        public ICommand SelectLetterCommand { get; }
        public ICommand ViewDetailsCommand  { get; }

        public event EventHandler<Anime>? NavigationRequested;

        public AZListViewModel(IAnimeSearchService search)
        {
            _search = search;
            SelectLetterCommand = new RelayCommand<string>(l => { if (l != null) SelectedLetter = l; });
            ViewDetailsCommand  = new RelayCommand<Anime>(a => { if (a != null) NavigationRequested?.Invoke(this, a); });
        }

        public async Task LoadAsync()
        {
            if (_search is not AniWavesSearchService aws) return;
            IsLoading = true;
            try
            {
                var items = await aws.GetAZListAsync(_selectedLetter);
                Anime.Clear();
                foreach (var a in items) Anime.Add(a);
            }
            finally { IsLoading = false; }
        }
    }
}
