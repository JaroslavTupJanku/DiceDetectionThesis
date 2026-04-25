# 🎯 Návod k použití - Detekce hracích kostek

## Rychlý start

### 1️⃣ Spuštění aplikace

Po spuštění uvidíte hlavní okno s následujícími sekcemi:

```
┌─────────────────────────────────────────────────────────────┐
│ 🎲 Detekce a analýza hracích kostek                         │
│ [📁 Načíst] [📷 Kamera] [📸 Zachytit] [🚀 AI] [🗑️ Vyčistit] │
├─────────────────────────────────────────────────────────────┤
│ [Statistiky: Počet | Součet | Confidence | Čas]             │
├─────────────────────────────────────────────────────────────┤
│                                  │                           │
│     [Obrazový viewport]          │  [Seznam detekcí]        │
│                                  │                           │
└─────────────────────────────────────────────────────────────┘
```

### 2️⃣ Metoda A: Načtení obrázku ze souboru

**Krok 1:** Klikněte na tlačítko **"📁 Načíst obrázek"**

**Krok 2:** V dialogu vyberte obrázek s kostkami
- Podporované formáty: `.jpg`, `.jpeg`, `.png`, `.bmp`
- Doporučené rozlišení: 640x640 px nebo více

**Krok 3:** Obrázek se zobrazí ve viewportu

**Krok 4:** Klikněte na **"🚀 Spustit AI"**

**Krok 5:** Sledujte progress bar a výsledky:
- Zelené boxy kolem detekovaných kostek
- Čísla hodnot (1-6) na každé kostce
- Detaily v pravém panelu

### 3️⃣ Metoda B: Použití kamery

**Krok 1:** Klikněte na **"📷 Kamera"**
- Spustí se live preview z kamery
- Status: "✓ Kamera aktivní"

**Krok 2:** Umístěte kostky do záběru kamery

**Krok 3:** Klikněte na **"📸 Zachytit sn��mek"**
- Kamera se zastaví
- Aktuální snímek zůstane zobrazen

**Krok 4:** Klikněte na **"🚀 Spustit AI"**

**Krok 5:** Zobrazí se výsledky detekce

### 4️⃣ Interpretace výsledků

#### Statistické karty (nahoře)

**🎲 POČET KOSTEK**
- Celkový počet detekovaných objektů
- Příklad: `3` = nalezeny 3 kostky

**∑ SOUČET HODNOT**
- Suma všech rozpoznaných hodnot
- Užitečné pro hry (např. Člověče nezlob se)
- Příklad: Kostky (4, 6, 2) → Součet: `12`

**✓ PRŮMĚRNÁ JISTOTA**
- Průměrná confidence všech detekcí
- Vyšší = spolehlivější detekce
- Rozsah: 0% - 100%
- Příklad: `87%` = velmi dobrá detekce

**⚡ ČAS INFERENCE**
- Doba zpracování AI modelů
- Nižší = rychlejší
- Typicky: 50-500 ms

#### Vizuální overlay

**Barevné boxy**
- Každá kostka má vlastní barvu
- Usnadňuje rozlišení jednotlivých objektů

**Labely**
- Zobrazují rozpoznanou hodnotu (1-6)
- Umístěny v levém horním rohu boxu

#### Seznam detekcí (vpravo)

Každá karta obsahuje:

```
┌────────────────────────┐
│ Kostka 1               │
│ 🎲 Hodnota: 6          │
│ ✓ Confidence: 0.95     │
│ 📐 Box: x=120, y=80... │
└────────────────────────┘
```

**Vysvětlení:**
- **Kostka N** - Pořadové číslo
- **Hodnota** - Rozpoznané číslo (1-6)
- **Confidence** - Jistota detekce (0.00 - 1.00)
  - 0.90+ = Velmi dobrá
  - 0.70-0.90 = Dobrá
  - <0.70 = Možná chyba
- **Box** - Souřadnice a velikost bbox
  - `x, y` = pozice levého horního rohu
  - `w, h` = šířka a výška

### 5️⃣ Tipy pro nejlepší výsledky

#### ✅ Doporučené podmínky

**Osvětlení**
- Dobré, rovnoměrné světlo
- Vyvarujte se stínů na kostkách
- Přirozené nebo bílé světlo

**Umístění kostek**
- Položte kostky na kontrastní podklad
- Ideálně: bílé kostky na tmavém pozadí nebo naopak
- Kostky by se neměly překrývat

**Vzdálenost a úhel**
- Pohled shora (90°) nebo mírný úhel
- Vzdálenost: všechny kostky musí být viditelné
- Ostrý obraz (ne rozmazaný)

**Kvalita obrázku**
- Minimálně 640x640 px
- Dobré rozlišení
- Bez motion blur

#### ❌ Vyhněte se

- Příliš tmavé nebo přeexponované foto
- Rozmazané obrázky
- Překrývající se kostky
- Příliš malé kostky v obraze (<50x50 px)
- Odlesky na kostkách

### 6️⃣ Ovládání aplikace

#### Klávesové zkratky (budoucí rozšíření)
```
Ctrl + O  → Načíst obrázek
Ctrl + R  → Spustit AI
Ctrl + C  → Zapnout/Vypnout kameru
Ctrl + N  → Vyčistit
Esc       → Zavřít aplikaci
```

#### Tlačítka

| Tlačítko | Funkce | Kdy aktivní |
|----------|--------|-------------|
| 📁 Načíst obrázek | Otevře file dialog | Vždy |
| 📷 Kamera | Zapne/Vypne kameru | Vždy |
| 📸 Zachytit | Zachytí snímek | Když je kamera aktivní |
| 🚀 Spustit AI | Spustí inferenci | Když je načten obrázek |
| 🗑️ Vyčistit | Reset aplikace | Vždy |

### 7️⃣ Troubleshooting

#### Problém: Kostky nejsou detekovány

**Řešení:**
1. Zkontrolujte osvětlení
2. Zlepšete kontrast (bílé kostky / černé pozadí)
3. Přibližte kameru / použijte větší obrázek
4. Ujistěte se, že kostky nejsou rozmazané

#### Problém: Špatná hodnota kostky

**Řešení:**
1. Ujistěte se, že tečky jsou jasně viditelné
2. Kostky by měly být rovně položené (ne nakloněné)
3. Použijte standardní 6-stěnné kostky
4. Zkuste lepší osvětlení

#### Problém: Nízká confidence

**Možné příčiny:**
- Nízká kvalita obrázku
- Špatné osvětlení
- Nestandardní kostky (barva, design)
- Částečně zakryté kostky

#### Problém: Pomalá inference

**Optimalizace:**
- Použijte menší obrázky (optimální: 640-1280 px)
- Zkontrolujte, zda ONNX Runtime používá GPU
- Zavřete ostatní aplikace

#### Problém: Kamera nefunguje

**Kontrola:**
1. Ujistěte se, že máte připojenou webkameru
2. Zkontrolujte, zda kamera není používána jinou aplikací
3. Restartujte aplikaci

### 8️⃣ Pokročilé použití

#### Export výsledků (budoucí feature)
- Uložení anotovaného obrázku
- Export do CSV (hodnoty, confidence, bbox)
- Historie detekcí

#### Batch processing (budoucí feature)
- Zpracování více obrázků najednou
- Složka → Výsledky

#### Vlastní modely (budoucí feature)
- Možnost načíst vlastní ONNX modely
- Konfigurace parametrů inference

---

## 📞 Podpora

Pokud narazíte na problémy:
1. Zkontrolujte tento návod
2. Přečtěte si ARCHITECTURE.md
3. Zkontrolujte konzoli pro error zprávy

---

**Příjemné používání! 🎲**
