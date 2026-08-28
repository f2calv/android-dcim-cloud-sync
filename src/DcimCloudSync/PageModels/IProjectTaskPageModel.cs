using CommunityToolkit.Mvvm.Input;
using DcimCloudSync.Models;

namespace DcimCloudSync.PageModels;

public interface IProjectTaskPageModel
{
    IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
    bool IsBusy { get; }
}