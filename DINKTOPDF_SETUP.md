# DinkToPdf Integration Guide

## De?i?iklikler

### WkhtmlConverterService
- **Önceki Durum**: Harici bir API'ye HTTP ça?r?s? yaparak PDF dönü?ümü gerçekle?tiriyordu
- **Yeni Durum**: DinkToPdf kütüphanesi ile do?rudan process içinde PDF dönü?ümü yap?yor

### Avantajlar
1. **Performans**: Network latency ortadan kalkt?, daha h?zl? dönü?üm
2. **Güvenilirlik**: Harici servis ba??ml?l??? yok
3. **Maliyet**: Ayr? bir service instance'a gerek yok
4. **Basitlik**: Tek bir servis ile tüm i?lemler halloluyor

## Gereksinimler

### Windows Deployment
1. Download the native libraries:
   - [32-bit](https://github.com/rdvojmoc/DinkToPdf/tree/master/v0.12.4/32%20bit)
   - [64-bit](https://github.com/rdvojmoc/DinkToPdf/tree/master/v0.12.4/64%20bit)

2. Copy `libwkhtmltox.dll` to:
   - Application root directory
   - Or system PATH

### Linux/Docker Deployment
wkhtmltopdf native kütüphaneleri Dockerfile'da otomatik olarak yüklenmektedir:
```bash
# Dockerfile içinde:
wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb
gdebi --n wkhtmltox_0.12.6.1-2.bullseye_amd64.deb
```

## Kullan?m

### DI Registration (Program.cs)
```csharp
using DinkToPdf;
using DinkToPdf.Contracts;

// Singleton olarak kay?t
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
```

### WkhtmlConverterService
Service art?k `IConverter` dependency'si al?yor:
```csharp
public WkhtmlConverterService(
    IServiceLogger logger, 
    IImageResizer imageResizer, 
    ICompressService compressService, 
    IPdfTransactionCollection pdfTransactionCollection, 
    IConverter converter)
{
    _converter = converter;
    // ...
}
```

## Konfigürasyon

### PDF Settings
```csharp
var globalSettings = new GlobalSettings
{
    ColorMode = ColorMode.Color,
    Orientation = Orientation.Portrait, // or Landscape
    PaperSize = PaperKind.A4,
    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
};

var objectSettings = new ObjectSettings
{
    PagesCount = true,
    HtmlContent = htmlContent,
    WebSettings = { DefaultEncoding = "utf-8" }
};
```

## Test

### Local Test
```bash
dotnet run --project eLogo.PdfService.Api
```

### Docker Test
```bash
docker build -t pdfservice:latest -f eLogo.PdfService.Api/Dockerfile .
docker run -p 8100:8100 pdfservice:latest
```

## Troubleshooting

### Windows: "Unable to load DLL 'libwkhtmltox'"
- `libwkhtmltox.dll` dosyas?n?n uygulama klasöründe oldu?undan emin olun
- Visual C++ Redistributable yüklü olmal?

### Linux: "libwkhtmltox.so not found"
- Dockerfile'daki symlink'in do?ru oldu?undan emin olun:
  ```bash
  ln -s /usr/local/lib/libwkhtmltox.so /usr/lib/libwkhtmltox.so
  ```

### Memory Issues
- DinkToPdf `SynchronizedConverter` kullan?yor (thread-safe)
- Büyük dosyalar için memory ayarlar?n? kontrol edin

## Migration Checklist

- [x] DinkToPdf NuGet paketi eklendi
- [x] WkhtmlConverterService güncellendi
- [x] HttpClient dependency kald?r?ld?
- [x] IConverter DI'a eklendi
- [x] Dockerfile güncellendi (wkhtmltopdf 0.12.6.1)
- [x] Helper metodlar eklendi (GetOrientation, GetPaperSize)
- [x] Build ba?ar?l?

## Notlar

- Zoom parametresi ?u anda kullan?lm?yor (DinkToPdf'te farkl? ?ekilde implemente edilmeli)
- Attachments henüz implemente edilmedi (gerekirse ileride eklenebilir)
- Custom properties (SourceId, VKN, UserAccountRef) tracking için kullan?l?yor
