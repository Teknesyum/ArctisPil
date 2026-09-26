<!-- lang -->

[<img src="assets/badge-lang.tr.svg" alt="Türkçe seçili, switch to English" width="124" height="44">](README.md)

# ArctisPil

Arctis Nova Pro Wireless için tepsi kontrolü.

| Ölçü | Değer |
|---|---|
| İncelenen benzer proje | 37 |
| Kullanılan HID komutu, her biri 2+ açık kaynakta | 7 |
| Gerçek cihazda denenen komut (PID 12E0) | 3 (`06 B0`, `06 20`, `06 25`) |
| Bağımlılık | 0 (.NET Framework 4, Windows ile gelir) |

## Nedir

ArctisPil, SteelSeries Arctis Nova Pro Wireless taban istasyonu için tek parça bir Windows tepsi programıdır. Tepsi simgesi pil yüzdesini gösterir. Sol tık bir panel açar: pil, kulaklık sesi, Windows sesi, gürültü engelleme, şeffaflık (yalnız şeffaf modda görünür) ve mikrofon seviyesi; pil dışındaki her satır panelden değiştirilebilir. Taban istasyonuyla HID üzerinden konuşur, SteelSeries GG'nin açık olmasına gerek yoktur.

## Bunu SteelSeries GG Zaten Yapmıyor Mu?

GG bunların hepsini ve fazlasını yapar: Sonar, uygulama başına karışım, EQ düzenleme, firmware güncellemesi. ArctisPil'in eklediği:

- **Tepside pil**, sayı olarak; seviyeye göre renkli, şarjda mavi, %25'te uyarı.
- **İki ses tek panelde.** Kulaklık sesi ile Windows sesi yan yana; ikisi de sürüklenir ya da tekerlekle değişir.
- **Ses aktarma (20-80).** Kulaklık %20 ile %80 arasında kalır. Düğme %80'i geçerse kulaklık %80'e döner, fazlası Windows sesine eklenir; %20'nin altına inerse kulaklık %20'ye döner, eksik Windows sesinden düşer.
- **Yuvarlak sayılar.** İki ses çubuğu da sürüklemede ve tekerlekte %5 adımlara oturur.
- **Arka plan paketi yok.** Tek 30 KB exe, servis yok, hesap yok.

## Özellikler

- **Pil simgesi** — yüzde tepsi simgesine çizilir, dakikada bir ve her kulaklık olayında yenilenir.
- **Kulaklık sesi** — açılışta okunur, düğmeyi izler, panelden ayarlanır (`06 25`).
- **Gürültü engelleme** — Kapalı / Şeffaf / ANC, ve şeffaflık seviyesi 1-10.
- **Mikrofon seviyesi** — 1-10.
- **Ayarlar cihaza kaydedilir** — son değişiklikten 0,8 sn sonra taban istasyonuna yazılır (`06 09`), güç kesilince kaybolmaz.

## Yapmadıkları

- EQ düzenleme, Sonar, ChatMix kontrolü, firmware güncellemesi yok.
- Yalnız Arctis Nova Pro Wireless PC taban istasyonu (VID 1038, PID 12E0) destekleniyor. Diğer kulaklıklar planda, bkz. [istek şablonu](../../issues/new?template=device.yml).
- Fabrika ayarı komutunu (`06 FD`) asla göndermez.
- Yalnız Windows.

## Kurulum

`ArctisPil.exe`'yi [son sürümden](../../releases/latest) indirip çalıştır. Kendini başlangıca ekler; istemezsen sağ tık menüsünde "Windows ile başlat" işaretini kaldır.

## Nasıl Çalışır

Taban istasyonu üreticiye özel bir HID arayüzü açar (arayüz 4). Komutlar `0xFFC0` koleksiyonuna `06` ile başlayan 64 baytlık raporlar olarak gider; istenmeden gelen olaylar `0xFF00` üzerinden `07` ile başlar.

| Komut | Anlamı |
|---|---|
| `06 B0` | durum: pil `[6]`, kulaklık hali `[15]`, şeffaflık `[8]`, ANC modu `[10]` |
| `06 20` | ses durumu: ses `[3]`, mikrofon `[17]` |
| `06 25 v` | kulaklık sesi, 0 = en yüksek, 56 = sessiz |
| `06 BD m` | 0 kapalı, 1 şeffaf, 2 ANC |
| `06 B9 l` | şeffaflık seviyesi 1-10 |
| `06 37 l` | mikrofon seviyesi 1-10 |
| `06 09` | ayarları cihaza kaydet |

Düğme dışındaki her `07 xx` olayı yeni bir `06 B0` / `06 20` okuması başlatır; kulaklıktaki ANC tuşuna basınca panel de değişir.

## Program Ne Yaptığını Gösterir

![Panel: pil, kulaklık sesi, Windows sesi, gürültü engelleme, şeffaflık ve mikrofon satırları.](assets/panel.png)

## Geliştirme

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Programın tamamı `src/ArctisPil.cs`. `test/komut-dene.ps1 -Komut 06-B0` tek bir ham rapor gönderir ve cevabı yazar. `test/onizleme.ps1` simgeyi ve paneli PNG'ye çizer.

## Katkı

Önce issue aç: [hata bildirimi](../../issues/new?template=bug.yml) ya da [kulaklık desteği isteği](../../issues/new?template=device.yml). PR'lar küçük olsun. Depo dili İngilizce. Katkılar proje lisansı altında kabul edilir. ArctisPil işine yarıyorsa [sponsor olmak](https://github.com/sponsors/Teknesyum) sürmesini sağlar.

## Lisans

AGPL-3.0-or-later. Bkz. [LICENSE](LICENSE).

<!-- signature -->
<div align="center">

<a href="https://github.com/sponsors/Teknesyum"><img src="assets/badge-sponsor.svg" alt="Support Teknesyum" height="38"></a>
&nbsp;
<a href="LICENSE"><img src="assets/badge-license.svg" alt="License AGPL-3.0" height="38"></a>

</div>
