# GoodbyeDPI Türkiye — Masaüstü Uygulaması

[GoodbyeDPI-Turkey](https://github.com/cagritaskn/GoodbyeDPI-Turkey) 0.2.3rc3 sürümünü `.cmd` dosyalarıyla uğraşmadan,
tek bir pencereden kullanmanızı sağlayan deneysel bir Windows uygulaması. GoodbyeDPI, servis sağlayıcıların derin paket
inceleme (DPI) ile uyguladığı erişim engellerini (ör. Discord) atlatır. VPN değildir, bağlantı hızını etkilemez.

## İndirme

**[Son sürümü indirin](https://github.com/SaintFrost/GoodbyeDpiApp/releases/latest)** → `GoodbyeDPI-Turkey.exe`

- Tek dosya, kurulum gerektirmez; .NET çalışma zamanı exe'nin içinde gelir (~50 MB).
- 64 bit Windows 10 / 11.
- Açılışta yönetici izni ister (WinDivert sürücüsü için gerekli).
- Exe imzalı olmadığı için Windows SmartScreen uyarı verebilir: **Ek bilgi → Yine de çalıştır**.

## Özellikler

- **Yöntem seçimi:** Standart ve 6 Superonline alternatifi ya da özel `goodbyedpi.exe` parametreleri.
- **Geçici oturum:** GoodbyeDPI uygulama açıkken çalışır; pencere kapatılınca sistem tepsisine iner.
- **Windows hizmeti:** tek tıkla kurma, güncelleme ve kaldırma; bilgisayar her açıldığında otomatik başlar.
  Dosyalar `C:\Program Files\GoodbyeDPI-Turkey\` altına kopyalanır, indirdiğiniz dosyayı taşımak hizmeti bozmaz.
- **Durum göstergesi:** hizmet, geçici oturum veya dışarıda başlatılmış `goodbyedpi.exe` algılanır.
- **Bağlantı testi:** DNS önbelleğini temizleyip google.com ve discord.com'a erişimi dener.
- Kaspersky çalışıyorsa uyarır (Kaspersky GoodbyeDPI'ın çalışmasını engelliyor).

## Kullanım

1. `GoodbyeDPI-Turkey.exe` dosyasını açın ve yönetici iznini onaylayın.
2. **Yöntem** listesinden "Standart (önerilen)" ile başlayın.
3. Denemek için **Başlat**'a, kalıcı kullanım için **Hizmet olarak kur**'a basın.
4. **Bağlantıyı test et** ile discord.com'a erişilip erişilmediğine bakın. Erişilemiyorsa durdurup başka bir yöntem deneyin.

DNS yönlendirmesi içermeyen yöntemlerde (Alternatif 1, 2 ve 6) Windows DNS sunucunuzu
77.88.8.8 / 77.88.8.1 (Yandex) veya 1.1.1.1 / 1.0.0.1 (Cloudflare) olarak değiştirmeniz gerekir; uygulama bunu ayrıca hatırlatır.

## Komut dosyalarıyla kullanım

Uygulama yerine orijinal `.cmd` dosyalarını kullanmak isterseniz bunlar [`scripts/`](scripts) klasöründe. Depoyu
(**Code → Download ZIP**) indirip klasör yapısını bozmadan kullanın; betikler `goodbyedpi.exe`'yi `x86/` ve `x86_64/`
klasörlerinde arar. Ayrıntılı rehber ve sık karşılaşılan sorunlar: [README-BENİ OKU.txt](README-BEN%C4%B0%20OKU.txt).

## Depo yapısı

| Klasör | İçerik |
| --- | --- |
| [`app/`](app) | Uygulamanın kaynak kodu (C# / WinForms, .NET 10) — derleme talimatları [app/README.md](app/README.md)'de |
| [`scripts/`](scripts) | GoodbyeDPI-Turkey'in `.cmd` betikleri |
| [`x86/`](x86), [`x86_64/`](x86_64) | `goodbyedpi.exe` ve WinDivert dosyaları (uygulama bunları exe'nin içine gömer) |
| [`licenses/`](licenses) | GoodbyeDPI, WinDivert ve diğer bileşenlerin lisansları |

## Derleme

[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ile depo kökünde:

```bash
dotnet publish app -c Release -o dist
```

Çıktı: `dist/GoodbyeDPI-Turkey.exe`.

## Teşekkür ve lisanslar

- [GoodbyeDPI](https://github.com/ValdikSS/GoodbyeDPI) — ValdikSS (Apache 2.0)
- [GoodbyeDPI-Turkey](https://github.com/cagritaskn/GoodbyeDPI-Turkey) — cagritaskn
- [WinDivert](https://github.com/basil00/WinDivert) — basil00 (LGPLv3 / GPLv2)

Lisans metinleri [`licenses/`](licenses) klasöründedir.

## Yasal uyarı

Bu proje yalnızca eğitim ve araştırma amaçlıdır. Kullanımından doğan her türlü sorumluluk kullanan kişiye aittir.
