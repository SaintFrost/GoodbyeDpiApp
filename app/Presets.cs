namespace GoodbyeDpiApp;

/// <summary>A goodbyedpi.exe command line, taken from the release's .cmd scripts.</summary>
sealed record Preset(string Name, string Args, string Description)
{
    public override string ToString() => Name;
}

static class Presets
{
    const string YandexDns = "--dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253";

    public static readonly Preset Custom = new("Özel parametreler", "",
        "goodbyedpi.exe parametrelerini aşağıya kendiniz yazın.");

    public static readonly Preset[] All =
    [
        new("Standart (önerilen)", $"-5 --set-ttl 5 {YandexDns}",
            "Çoğu servis sağlayıcıda çalışan varsayılan yöntem (turkey_dnsredir.cmd)."),
        new("Alternatif 1 — Superonline", "--set-ttl 3",
            "Yalnızca TTL ayarı."),
        new("Alternatif 2 — Superonline", "-5",
            "TTL ayarı yok; TTL'li yöntemlerde siteler yavaş açılıyorsa deneyin."),
        new("Alternatif 3 — Superonline", $"--set-ttl 3 {YandexDns}",
            "TTL 3 ve Yandex DNS yönlendirmesi."),
        new("Alternatif 4 — Superonline", $"-5 {YandexDns}",
            "TTL ayarı yok, Yandex DNS yönlendirmesi var; siteler yavaş açılıyorsa önerilir."),
        new("Alternatif 5 — Superonline", $"-9 {YandexDns}",
            "-9 modu ve Yandex DNS yönlendirmesi."),
        new("Alternatif 6 — Superonline", "-9",
            "Yalnızca -9 modu."),
        Custom,
    ];

    /// <summary>Finds the preset with exactly these arguments, or <see cref="Custom"/>.</summary>
    public static Preset Match(string args)
    {
        string normalized = Normalize(args);
        return All.FirstOrDefault(p => p != Custom && Normalize(p.Args) == normalized) ?? Custom;
    }

    static string Normalize(string args) =>
        string.Join(' ', args.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
