# Danışma 001 girdi: Tepsi Simgesindeki Pil Rakamı: Daha Pürüzsüz Çizim Mümkün mü?

Ajana giden metin:

---

[[danisma:001]]

# Tepsi Simgesindeki Pil Rakamı: Daha Pürüzsüz Çizim Mümkün mü?

## Sahibin Cümleleri (aynen)

- "tray icondaki numara daha keskin basılmalı sanki kalite düşük gibi 100 güzel bir yeşil renginde olmalı 0 a gittikçe kırmızılaşmalı 50 mavi olabilir"
- "normal mi sence ne smooth ne düzgün çizilmiş"
- "hala smooth değil 144 p den 480 e çekmemiz lazım gibi yine idare ediyorsun da daha iyisi yok mu mümkün mü"
- "fable a sor bu konuyu uygulama içindeki yazıları bu fontla yapma yapacaksan bile"

## Olgular

- Program: HeadsetBatteryTray, .NET Framework 4 WinForms (C# 5, framework csc), tek dosya. Tepsi simgesi `NotifyIcon.Icon`, bitmap → `GetHicon`.
- Makine: 2560×1440, ekran ölçeği %100 (96 DPI), `SystemInformation.SmallIconSize` = 16×16, ClearType açık (`FontSmoothingType` = 2). Görev çubuğu koyu, ölçülen zemin #202A31. Windows 11 23H2.
- Simge pil yüzdesini 1-3 basamak rakamla gösterir (0-100, şarjda beyaz, "–" kapalı, "?" bağlanıyor). Renk %100 yeşil #66F09A → %50 mavi #75B9FF → %0 #FF9898; hepsi zemine karşı ≥ 7:1.
- Denenenler (görüntüler sırayla):
  1. v0.4.0: 32×32 bitmap, Bahnschrift Bold `GraphicsPath` 32 genişliğe gerilmiş, Windows 16'ya küçültüyor → bulanık.
  2. v0.4.1: gerçek 16 px'te 8× süper örnekleme + alfa kontrast eğrisi (sert 1,8) → sahip: "ne smooth ne düzgün".
  3. v0.4.2: Bahnschrift Condensed Bold, GDI+ `AntiAliasGridFit`, basamak başına mürekkep kutusu, 1 px ara, tam piksel yerleşim. Yedi seçenek kıyaslandı (Bahnschrift/Condensed/SemiCondensed, Segoe UI Variable Small, 1-bit). → sahip: "hala smooth değil".
  4. v0.4.3 (şu an çalışan, yayımlanmadı): 1-2 basamakta GDI+ `ClearTypeGridFit` siyah zemine beyaz; kanal başına örtü; alfa = en büyük kanal, renk = zemin + (hedef − zemin) · örtü_k / alfa (#202A31 zemine göre pişirilmiş). "100" gri tonlu kalır (ClearType ile üç basamak kısa kalıyordu). Üzerine gelince zemin açılır → saçak hafif sapar.
- Görüntüler: docs/ui-denetim/2026-09-27/simge-100-v040.png (v0.4.1), simge-100-v042.png, simge-100-sonra.png (v0.4.3), simge-secenekler.png (yedi seçenek). Sahibin ekran görüntüsünde "94" 16 px yükseklikte ~7 px genişlikte iki basamak.
- Kısıtlar: sistem ayarı değiştirilemez (ölçek sahibin kararı). Renkler token'lardan; kırmızı sahibin isteğiyle token dışı. Paneldeki yazılar Atkinson Hyperlegible Next ve Cascadia Mono (gömülü); tepsi rakamı Bahnschrift sahibin eski tercihi.

## Soru

1. 16×16 tepside, %100 ölçekte, 1-3 basamak rakamı en pürüzsüz ve düzgün gösterecek yol hangisi? Bizim denemediğimiz ne var: elle çizilmiş piksel rakam seti (ör. 7×16 / 5×16 kademeleri), DirectWrite/GDI `TextRenderer` ile yazı tipi ipuçlaması, farklı yazı tipi (hangisi, neden), gamma düzeltmeli karıştırma, 2 basamak için 1 px yerine 0 ara, `LoadIconMetric`/`LIM_SMALL` ile çok boyutlu .ico, vb.
2. ClearType'ı simgeye pişirmek (v0.4.3) doğru mu, yoksa tepside saçak/üzerine gelme sorunu yüzünden kaçınılmalı mı?
3. "144p'den 480'e" beklentisi 16 px'te karşılanabilir mi; karşılanamıyorsa sahibe dürüst cevap ne?
4. Sahibin cümlesi: "uygulama içindeki yazıları bu fontla yapma yapacaksan bile" — tepsi rakamı için önerdiğin yazı tipi panel yazılarına sızmamalı; bu ayrım için bir önerin var mı?

Kısa, uygulanabilir, sıralı öneri iste: önce en büyük etkili tek değişiklik.
