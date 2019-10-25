using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LandRoverApp
{
    public partial class App : Application
    {
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MTYxODM0QDMxMzcyZTMzMmUzMGRDUHRvZ3ZnMmdGbU00ZUdZcmdwbzhReFdoQVpYWTVSVHRVVHhTYXlvSlE9");

            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
