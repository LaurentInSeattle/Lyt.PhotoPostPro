using Avalonia;
using System;
using System.Threading;

// MUST be placed after 'using Avalonia'
namespace Lyt.PhotoPostPro.Desktop; 

internal class Program
{
    private static Mutex? SingleInstanceMutex;
    private const string MutexUniqueName = "Lyt.PhotoPostPro.Desktop"; 
        
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Dont bother for single instance when debugging 
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
#else
        Program.SingleInstanceMutex = new Mutex(true, Program.MutexUniqueName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running; exit gracefully
            return;
        }
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Program.SingleInstanceMutex.ReleaseMutex(); 
        } 
#endif 
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>(()=> new App(ImplementationsProvider.MtpService()))
            .UsePlatformDetect()
            .With(new SkiaOptions() { MaxGpuResourceSizeBytes = 2L * 1024L * 1024L * 1024L }) // 2 GB 
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}