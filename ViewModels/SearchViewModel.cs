using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly IAnimeSearchService _searchService;

        private string _searchQuery = string.Empty;
        private bool _isLoading;
        private bool _hasSearched;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private CancellationTokenSource? _cts;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                SetField(ref _searchQuery, value);
                OnPropertyChanged(nameof(HasSearchText));
            }
        }

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public bool HasSearched { get => _hasSearched; set => SetField(ref _hasSearched, value); }
        public bool HasError { get => _hasError; set => SetField(ref _hasError, value); }
        public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }
        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchQuery);
        public bool HasResults => SearchResults.Count > 0;
        public bool NoResults => HasSearched && !IsLoading && !HasError && SearchResults.Count == 0;

        public ObservableCollection<Anime> SearchResults { get; } = new();

        public ICommand SearchCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SelectAnimeCommand { get; }

        public event EventHandler<Anime>? AnimeSelected;

        public SearchViewModel(IAnimeSearchService searchService)
        {
            _searchService = searchService;
            SearchCommand = new AsyncRelayCommand(DoSearchAsync);
            ClearCommand = new RelayCommand(ClearSearch);
            SelectAnimeCommand = new RelayCommand<Anime>(SelectAnime);
        }

        private async Task DoSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            IsLoading = true;
            HasError = false;
            HasSearched = true;
            SearchResults.Clear();
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(NoResults));

            try
            {
                var results = await _searchService.SearchAsync(SearchQuery, _cts.Token);
                SearchResults.Clear();
                foreach (var a in results) SearchResults.Add(a);
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(NoResults));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(NoResults));
            }
        }

        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            SearchResults.Clear();
            HasSearched = false;
            HasError = false;
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(NoResults));
        }

        private void SelectAnime(Anime? anime)
        {
            if (anime != null)
                AnimeSelected?.Invoke(this, anime);
        }
    }
}
