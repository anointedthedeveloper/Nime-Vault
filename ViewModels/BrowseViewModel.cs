using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class BrowseViewModel : BaseViewModel
    {
        private readonly IAnimeSearchService _search;
        private bool _isLoading;
        private string _topPeriod = "today";
        private string _latestTab = "updated";

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }
        public string TopPeriod { get => _topPeriod; set { SetField(ref _topPeriod, value); _ = LoadTopAsync(); } }
        public string LatestTab { get => _latestTab; set { SetField(ref _latestTab, value); _ = LoadLatestAsync(); } }

        public ObservableCollection<Anime> TopAnime      { get; } = new();
        public ObservableCollection<Anime> LatestAnime   { get; } = new();
        public ObservableCollection<Anime> NewRelease    { get; } = new();
        public ObservableCollection<Anime> NewAdded      { get; } = new();
        public ObservableCollection<Anime> JustCompleted { get; } = new();

        public ICommand LoadCommand           { get; }
        public ICommand SetTopTodayCommand    { get; }
        public ICommand SetTopWeekCommand     { get; }
        public ICommand SetTopMonthCommand    { get; }
        public ICommand SetLatestAllCommand   { get; }
        public ICommand SetLatestSubCommand   { get; }
        public ICommand SetLatestDubCommand   { get; }
        public ICommand SetLatestTrendingCommand { get; }
        public ICommand ViewDetailsCommand    { get; }

        public event EventHandler<Anime>? NavigationRequested;

        public BrowseViewModel(IAnimeSearchService search)
        {
            _search = search;
            LoadCommand              = new AsyncRelayCommand(LoadAllAsync);
            SetTopTodayCommand       = new RelayCommand(() => TopPeriod = "today");
            SetTopWeekCommand        = new RelayCommand(() => TopPeriod = "week");
            SetTopMonthCommand       = new RelayCommand(() => TopPeriod = "month");
            SetLatestAllCommand      = new RelayCommand(() => LatestTab = "updated");
            SetLatestSubCommand      = new RelayCommand(() => LatestTab = "subbed");
            SetLatestDubCommand      = new RelayCommand(() => LatestTab = "dubbed");
            SetLatestTrendingCommand = new RelayCommand(() => LatestTab = "trending");
            ViewDetailsCommand       = new RelayCommand<Anime>(a => { if (a != null) NavigationRequested?.Invoke(this, a); });
        }

        public async Task LoadAllAsync()
        {
            IsLoading = true;
            try
            {
                await Task.WhenAll(LoadTopAsync(), LoadLatestAsync(), LoadSectionsAsync());
            }
            finally { IsLoading = false; }
        }

        private async Task LoadTopAsync()
        {
            if (_search is not IBrowseProvider aws) return;
            var items = await aws.GetTopAnimeAsync(_topPeriod);
            TopAnime.Clear();
            foreach (var a in items) TopAnime.Add(a);
        }

        private async Task LoadLatestAsync()
        {
            if (_search is not IBrowseProvider aws) return;
            var items = await aws.GetLatestEpisodesAsync(_latestTab);
            LatestAnime.Clear();
            foreach (var a in items) LatestAnime.Add(a);
        }

        private async Task LoadSectionsAsync()
        {
            if (_search is not IBrowseProvider aws) return;
            var (nr, na, jc) = await (aws.GetNewReleaseAsync(), aws.GetNewAddedAsync(), aws.GetJustCompletedAsync())
                .WhenAll3();
            NewRelease.Clear();    foreach (var a in nr) NewRelease.Add(a);
            NewAdded.Clear();      foreach (var a in na) NewAdded.Add(a);
            JustCompleted.Clear(); foreach (var a in jc) JustCompleted.Add(a);
        }
    }

    // Helper to await 3 tasks
    internal static class TaskExtensions
    {
        public static async Task<(T1, T2, T3)> WhenAll3<T1, T2, T3>(
            this (Task<T1> t1, Task<T2> t2, Task<T3> t3) tasks)
        {
            await Task.WhenAll(tasks.t1, tasks.t2, tasks.t3);
            return (tasks.t1.Result, tasks.t2.Result, tasks.t3.Result);
        }
    }
}
