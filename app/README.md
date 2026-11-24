# GoodbyeDPI Türkiye — masaüstü uygulaması (deneysel)

Bu klasördeki `.cmd` betiklerinin yaptığı her şeyi tek bir pencereden yapan Windows uygulaması.
`goodbyedpi.exe`, WinDivert dosyaları ve lisanslar exe'nin içine gömülüdür; ayrı bir klasör taşımanız gerekmez.

## Neler yapar

- **Yöntem seçimi:** Standart ve 6 Superonline alternatifi (betiklerdeki parametrelerin aynısı) ya da özel parametreler.
  DNS yönlendirmesi içermeyen yöntemlerde Windows DNS ayarını değiştirmeniz için uyarı gösterir.
- **Geçici oturum** (`turkey_dnsredir*.cmd` karşılığı): `goodbyedpi.exe` gizli çalışır, çıktısı "Kayıt" bölümüne düşer.
  Pencere kapatılınca uygulama sistem tepsisine iner; tepsiden "Çıkış" seçilince ya da uygulama çökerse GoodbyeDPI da durur.
- **Windows hizmeti** (`service_install*.cmd` / `service_remove.cmd` karşılığı): aynı `sc` komutlarıyla `GoodbyeDPI`
  hizmetini kurar, günceller ya da kaldırır. Hizmet dosyaları `C:\Program Files\GoodbyeDPI-Turkey\` altına kopyalanır,
  böylece "klasörü taşımayın" sorunu ortadan kalkar.
- **Durum göstergesi:** hizmet, geçici oturum veya uygulama dışında başlatılmış `goodbyedpi.exe` algılanır.
- **Bağlantı testi:** DNS önbelleğini temizleyip google.com ve discord.com'a bağlanmayı dener.
- Açılışta Kaspersky çalışıyorsa uyarır (README-BENİ OKU.txt'deki nota bakın).

## Çalıştırma

`dist\GoodbyeDPI-Turkey.exe` dosyasını açın; yönetici izni ister (WinDivert için gerekli).
Exe .NET çalışma zamanını içinde taşır (self-contained, yaklaşık 50 MB); bilgisayarda .NET kurulu olması gerekmez.
64 bit Windows 10/11 içindir.

## Derleme

```bash
dotnet publish app -c Release -o dist
```

Daha küçük (~850 KB) ama [.NET 10 Masaüstü Çalışma Zamanı](https://dotnet.microsoft.com/download/dotnet/10.0) gerektiren bir exe için:

```bash
dotnet publish app -c Release -o dist --self-contained false
```

Debug derlemesi yönetici izni istemez; arayüzü yetkisiz incelemek için kullanılabilir.

## Dosyalar

| Dosya | İçerik |
| --- | --- |
| `MainForm.cs` | Pencere, sistem tepsisi, düğmeler ve bağlantı testi |
| `Presets.cs` | `.cmd` betiklerinden alınan yöntemler |
| `DpiProcess.cs` | Geçici oturum: gizli alt süreç ve kapanınca onu da sonlandıran iş nesnesi (job object) |
| `DpiService.cs` | Hizmet durumu (Windows API) ve kurma/kaldırma (`sc.exe`) |
| `Binaries.cs` | Gömülü dosyaları Program Files'a çıkarma |
| `Settings.cs` | Son seçilen yöntemi `%LOCALAPPDATA%\GoodbyeDPI-Turkey\settings.json` içinde saklar |
