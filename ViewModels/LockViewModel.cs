using System;
using System.Windows.Input;

namespace NimeVault.ViewModels
{
    public class LockViewModel : BaseViewModel
    {
        public ICommand UnlockCommand { get; }

        public event EventHandler? UnlockRequested;

        public LockViewModel()
        {
            UnlockCommand = new RelayCommand(() => UnlockRequested?.Invoke(this, EventArgs.Empty));
        }
    }
}
