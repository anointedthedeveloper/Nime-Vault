using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NimeVault.Models;
using NimeVault.Services.Interfaces;

namespace NimeVault.ViewModels
{
    public class PlayerViewModel : BaseViewModel
    {
        private readonly IStreamService _streamService;
        private readonly INotificationService _notifications;

        private string _animeTitle = string.Empty;
        private int _episodeNumber;
        private string _language = "sub";
        private string? _streamUrl;
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private CancellationTokenSource? _cts;

        public string AnimeTitle    { get => _animeTitle;    set => SetField(ref _animeTitle,    value); }
        public int EpisodeNumber    { get => _episodeNumber; set => SetField(ref _episodeNumber, value); }
        public string Language      { get => _language;      set => SetField(ref _language,      value); }
        public string? StreamUrl    { get => _streamUrl;     set => SetField(ref _streamUrl,     value); }
        public bool IsLoading       { get => _isLoading;     set => SetField(ref _isLoading,     value); }
        public bool HasError        { get => _hasError;      set => SetField(ref _hasError,       value); }
        public string ErrorMessage  { get => _errorMessage;  set => SetField(ref _errorMessage,  value); }

        public bool HasStream => !string.IsNullOrEmpty(StreamUrl);

        public ICommand CloseCommand { get; }
        public ICommand RetryCommand { get; }

        public event EventHandler? CloseRequested;

        public PlayerViewModel(IStreamService streamService, INotificationService notifications)
        {
            _streamService = streamService;
            _notifications = notifications;
            CloseCommand = new RelayCommand(Close);
            RetryCommand = new AsyncRelayCommand(ResolveAsync);
        }

        public async Task LoadAsync(string animeId, int episodeNumber, string language, string animeTitle)
        {
            AnimeTitle = animeTitle;
            EpisodeNumber = episodeNumber;
            Language = language;
            StreamUrl = null;
            HasError = false;
            IsLoading = true;
            OnPropertyChanged(nameof(HasStream));

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                var url = await _streamService.ResolveStreamUrlAsync(animeId, episodeNumber, language, _cts.Token);
                if (string.IsNullOrEmpty(url))
                {
                    HasError = true;
                    ErrorMessage = "Stream URL could not be resolved. The episode may be unavailable.";
                }
                else
                {
                    StreamUrl = url;
                    OnPropertyChanged(nameof(HasStream));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
                _notifications.ShowError($"Stream error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ResolveAsync()
        {
            if (string.IsNullOrEmpty(AnimeTitle)) return;
            // Re-trigger load with same params — caller must have set them
            HasError = false;
            IsLoading = true;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            try
            {
                var url = await _streamService.ResolveStreamUrlAsync(AnimeTitle, EpisodeNumber, Language, _cts.Token);
                StreamUrl = url;
                OnPropertyChanged(nameof(HasStream));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
            finally { IsLoading = false; }
        }

        private void Close()
        {
            _cts?.Cancel();
            StreamUrl = null;
            OnPropertyChanged(nameof(HasStream));
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
