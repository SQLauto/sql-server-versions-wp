#define DEBUG_AGENT

using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using Microsoft.Phone.Shell;
using System.Linq;
using RepositoryCaller;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notifier
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private DeviceIntegrator _deviceIntegrator = new DeviceIntegrator();

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override async void OnInvoke(ScheduledTask task)
        {
            //TODO: Add code to perform your task in background

            //if (task is PeriodicTask)
            //{

            //}

            //ShellToast toast = new ShellToast();
            //toast.Title = "Background Agent Sample";
            //toast.Content = "testing";
            //toast.Show();

            int NewReleasesCount = (await _deviceIntegrator.GetUncheckedNewReleasesAsync()).Count();

            // debugging
            //
            //NewReleasesCount = (new Random()).Next(1, 10);

            IconicTileData TileUpdate = new IconicTileData();
            TileUpdate.Title = "SQL Versions";
            TileUpdate.Count = NewReleasesCount;
            //TileUpdate.WideContent1 = "wc1";
            //TileUpdate.WideContent2 = "wc2";
            //TileUpdate.WideContent3 = "wc3";

            ShellTile AppTile = ShellTile.ActiveTiles.First();
            if (AppTile != null)
                AppTile.Update(TileUpdate);
            
#if DEBUG_AGENT
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
#endif

            NotifyComplete();
        }
    }
}