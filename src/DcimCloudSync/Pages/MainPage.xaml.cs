using DcimCloudSync.Models;
using DcimCloudSync.PageModels;

namespace DcimCloudSync.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}