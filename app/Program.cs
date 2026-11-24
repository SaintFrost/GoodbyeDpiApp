using System.Text;

namespace GoodbyeDpiApp;

static class Program
{
    const string Title = "GoodbyeDPI Türkiye";

    [STAThread]
    static void Main(string[] args)
    {
        Mutex mutex;
        bool owned;
        try
        {
            mutex = new Mutex(false, "GoodbyeDPI-Turkey-App");
            // After "Yönetici olarak aç" the previous instance may still be closing.
            owned = mutex.WaitOne(TimeSpan.FromSeconds(3));
        }
        catch (AbandonedMutexException ex)
        {
            mutex = ex.Mutex!;
            owned = true;
        }
        catch (UnauthorizedAccessException)
        {
            // Held by an elevated instance.
            mutex = null!;
            owned = false;
        }
        if (!owned)
        {
            MessageBox.Show("Uygulama zaten açık. Sistem tepsisindeki simgeye bakın.", Title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (mutex)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ApplicationConfiguration.Initialize();
#if DEBUG
            if (args is ["--screenshot", var path, .. var rest])
            {
                MainForm.SaveScreenshot(path, rest is [var index] ? int.Parse(index) : -1);
                return;
            }
#endif
            Application.Run(new MainForm());
        }
    }
}
