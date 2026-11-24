namespace GoodbyeDpiApp;

/// <summary>
/// goodbyedpi.exe and WinDivert are embedded in this exe and copied to Program Files before use.
/// The service runs them as SYSTEM, so they must live where only administrators can write.
/// </summary>
static class Binaries
{
    public static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GoodbyeDPI-Turkey");

    static string Arch => Environment.Is64BitOperatingSystem ? "x86_64" : "x86";

    public static string ExePath => Path.Combine(Root, Arch, "goodbyedpi.exe");

    /// <summary>Copies the embedded files to <see cref="Root"/> and returns the goodbyedpi.exe path.</summary>
    public static string Prepare(Action<string> log)
    {
        Extract($"bin/{Arch}/", Path.Combine(Root, Arch), log);
        Extract("licenses/", Path.Combine(Root, "licenses"), log);
        if (!File.Exists(ExePath))
            throw new FileNotFoundException(
                $"goodbyedpi.exe bulunamadı. Antivirüs programınız dosyayı silmiş olabilir; \"{Root}\" klasörünü dışlamalara ekleyip tekrar deneyin.");
        return ExePath;
    }

    static void Extract(string prefix, string dir, Action<string> log)
    {
        var assembly = typeof(Binaries).Assembly;
        Directory.CreateDirectory(dir);
        foreach (string name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal)))
        {
            string target = Path.Combine(dir, name[prefix.Length..]);
            using var stream = assembly.GetManifestResourceStream(name)!;
            var data = new byte[stream.Length];
            stream.ReadExactly(data);

            // A loaded driver or running service keeps its files open, so only write what changed.
            if (File.Exists(target) && File.ReadAllBytes(target).AsSpan().SequenceEqual(data))
                continue;
            try
            {
                File.WriteAllBytes(target, data);
                log($"Kopyalandı: {target}");
            }
            catch (IOException ex)
            {
                log($"Uyarı: {target} güncellenemedi ({ex.Message})");
            }
        }
    }
}
