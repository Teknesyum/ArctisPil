# Danışma 001: Tepsi Simgesindeki Pil Rakamı: Daha Pürüzsüz Çizim Mümkün mü?

claude-fable-5-1, 7.879 çıktı token, 144 sn. Girdi: [001-fable-tepsi-rakam-girdi.md](001-fable-tepsi-rakam-girdi.md). Cevap olduğu gibi:

---

# Danışma 001 Cevap: Tepsi Rakamı

## Kısa hüküm

16 piksel 16 pikseldir; "480p" olmaz. Ama "keskin ve düzgün" olur, ve bunun yolu yazı tipi değil: **elle çizilmiş piksel rakam seti**. Denenen yedi seçeneğin hepsi bir vektör hattını 16 piksele sığdırmaya çalışıyor; bu boyda her eğri 2-3 pikselle çizilir ve rasterleştirici hangi pikseli yarım bırakacağına kendi karar verir. Sahibin "ne smooth ne düzgün" dediği şey tam olarak bu kararsızlık: "94"te dikey 2 px, "8"in orta çizgisi 3 px, "?" kancası 1 px. Bir insan çizerse bu tutarsızlık yok olur.

## Sıralı öneri

**1. (En büyük etki) Piksel rakam seti, koda gömülü, yazı tipi yok.**
- İki kademe: iki basamak için **7×12** (2 px kalem, 1 px ara → 15 px), "100" için **1 = 3 px, 0 = 5 px** (1 px dikey, 2 px yatay, 1 px ara → 15 px). Yükseklik 12, üst/alt 2 px boşluk; 20 ve 24 px (%125/%150) için ayrı kademe çizilir, `SmallIconSize`'a göre seçilir.
- Her rakam bir `byte[12]` satır dizisi (bit = mürekkep). Köşe yumuşatma elle: 2, 4, 7'nin çaprazlarında basamak başına **tek** %50 piksel; başka yerde gri yok. Bu "pürüzsüz ama bulanık değil"in tarifidir; Tahoma 8 pt bitmap strike'ları ve Terminus böyle yapılır.
- Örnek "0" (7×12, `#`=tam, `+`=%50):
  ```
  .+###+.
  +#####+
  ##...##
  ##...##
  ##...##
  ##...##
  ##...##
  ##...##
  ##...##
  ##...##
  +#####+
  .+###+.
  ```
- Çizim: 16×16 ARGB bitmap, alfa = matris değeri, renk = hedef renk düz (zemine göre pişirme yok). `GetHicon` sonrası `DestroyIcon`. Üzerine gelince zemin açılsa da saçak çıkmaz çünkü örtü tek kanallı.

**2. ClearType pişirme (v0.4.3): vazgeç.** ClearType yalnızca yatay çözünürlük ekler; şikâyet dikey/çapraz basamaklarda. Buna karşılık alt piksel dizilimi varsayımı (harici/dönük ekranda ters gelir), üzerine gelme ve seçim zeminleri, bildirim/Görev Yöneticisi gibi başka zeminlerde görünme, `GetHicon` alfa birleştirmesinin kanal bazlı olmaması hepsi aleyhte. Gri tonlu kal; piksel seti gelince zaten gri ton da neredeyse kalmıyor.

**3. Denenmemişler içinde değmeyenler:**
- `TextRenderer`/GDI ipuçlaması: Segoe UI'nin delta ipuçları 9-10 pt'de iyidir ama 12 px kap yüksekliğinde kalın rakam için ipuçlama yine 2/3 px kalem arasında salınır. Bahnschrift'in ipuçları zayıf (değişken font, otomatik ipuç), zaten en kötü aday.
- Gamma düzeltmeli karıştırma: açık yazı/koyu zeminde kenar pikselleri biraz inceltir, ince ayar; piksel setinde alfa elle seçildiği için gereksiz.
- 2 basamakta 0 ara: "11" ve "00" birleşir, yapma. 1 px ara kalır.
- `LoadIconMetric`/çok boyutlu .ico: `NotifyIcon` tek HICON alır, Windows %100'de 16 ister; şu anki doğrudan-16 yaklaşımı doğru. .ico yalnızca 16/20/24 kademelerini dosyada taşımak istersen anlamlı; kodda dizi tutmak daha basit.

**4. "144p'den 480p'ye" — dürüst cevap:** "Tepsideki hücre 16×16; ekran ölçeği değişmeden piksel eklenemez. Saat yazısı pürüzsüz görünüyorsa küçük (8 px kap) ve ince (1 px kalem) olduğundandır; bizim rakam 12 px boyunda ve kalın, o yüzden her eğri 2-3 pikselle çizilmek zorunda. Yapabileceğimiz: her pikseli elle yerleştirmek, böylece kalem her rakamda aynı kalır ve kenarlar bilinçli olur. Bu, keskinlik tavanıdır." İstersen sahibe iki set göster: kalın (2 px) ve ince (1 px, saat gibi); seçim onun.

**5. Yazı tipi sızması:** Piksel seti ile tepside yazı tipi hiç olmaz, sorun kökten biter. Ayrım kuralı: tepsi çizimi tek bir `static class TrayGlyph` içinde, `System.Drawing.Font` ve `Ui.Fonts.*` kullanmaz; panel yalnızca `Ui.Fonts.*` üzerinden yazı tipi alır. Tarayıcıya tek kural: `TrayGlyph` dışında "Bahnschrift" geçerse hata. Sahibin cümlesi böylece yapısal olarak korunur.

## Uygulama sırası
1. 7×12 ve 3/5×12 setleri çiz (0-9, "–", "?"), 16 px'te 1:1 ekran görüntüsü al, sahibe göster.
2. Onay gelirse 20 ve 24 px kademeleri.
3. v0.4.3 ClearType kodu `trash/`'e.

Yok
