using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QldtSdh.Wpf.ViewModels
{
    public partial class SnapshotHistoryViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        public SnapshotHistoryViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
