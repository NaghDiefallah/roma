using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Roma
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("App.OnLaunched: Creating MainWindow...");
            _window = new MainWindow();

            if (_window == null)
            {
                System.Diagnostics.Debug.WriteLine("App.OnLaunched: CRITICAL - MainWindow is null after construction!");
                throw new InvalidOperationException("MainWindow failed to initialize");
            }

            System.Diagnostics.Debug.WriteLine("App.OnLaunched: MainWindow created successfully, activating...");

            try
            {
                _window.Activate();
                System.Diagnostics.Debug.WriteLine("App.OnLaunched: Window activated successfully");
            }
            catch (Exception ex)
            {
                // Log the exception details
                System.Diagnostics.Debug.WriteLine($"App.OnLaunched: EXCEPTION during Activate()");
                System.Diagnostics.Debug.WriteLine($"Exception Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner exception type: {ex.InnerException.GetType().FullName}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }

                System.Diagnostics.Debug.WriteLine("App.OnLaunched: Continuing despite activation exception...");
            }

            // Give the window a moment to fully activate, then trigger initialization
            System.Diagnostics.Debug.WriteLine("App.OnLaunched: Scheduling post-activation setup...");
            _window.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("App.OnLaunched: Executing post-activation setup...");
                    (_window as MainWindow)?.InitializeAfterActivation();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"App.OnLaunched: Error in post-activation setup: {ex.Message}");
                }
            });
        }
    }
}
