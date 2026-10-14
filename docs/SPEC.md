# Bits Converter 規格書

| 項目 | 內容 |
|---|---|
| 版本 | 1.0 |
| 日期 | 2026-10-13 |
| 來源專案 | [Leo0862/Binary-Converter](https://github.com/Leo0862/Binary-Converter)（WPF、.NET 6、1 byte） |
| 目標專案 | [Leo0862/Bits-Converter](https://github.com/Leo0862/Bits-Converter)（Avalonia、.NET 8、1～4 byte） |

## 1. 目標

1. 將 WPF 版 Binary Converter 以 Avalonia 重構，支援 Windows / Linux / macOS。
2. 位寬由固定 8-bit 擴充為 8 / 16 / 24 / 32-bit 可選。
3. 新增 Bit-field 切割：將 `[W-1:0]` 切成多個欄位（例如 `[15:10] [9:3] [2:0]`），每個欄位獨立顯示並可編輯 HEX / DEC / BIN。

## 2. 技術選型

| 項目 | 選擇 |
|---|---|
| Runtime | .NET 8 LTS |
| UI | Avalonia 11.3.x，Fluent theme，跟隨系統淺色 / 深色 |
| MVVM | CommunityToolkit.Mvvm 8.4.x（source generator） |
| 測試 | xUnit：`BitsConverter.Core` 邏輯測試；Avalonia.Headless 的 ViewModel 測試 |
| CI | GitHub Actions：build + test；發佈 win-x64 / linux-x64 / osx-x64 / osx-arm64 single-file self-contained 執行檔（裁剪＋壓縮，約 20 MB） |
| JSON | System.Text.Json source generator（`JsonSerializerContext`），裁剪後仍可正常存取設定與 Preset |
| 介面語言 | 英文 |

## 3. 專案結構

```
Bits-Converter/
├─ BitsConverter.sln
├─ src/
│  ├─ BitsConverter.Core/        # Pure logic, no UI dependency
│  │  ├─ BitMath.cs              # mask / shift / rotate / field get & set
│  │  ├─ NumberFormatter.cs      # HEX / BIN / DEC / byte-array text
│  │  ├─ NumberParser.cs         # tolerant HEX / BIN / DEC parsing
│  │  ├─ BitField.cs             # field record
│  │  ├─ FieldLayout.cs          # validation, segments, split / merge
│  │  ├─ FieldLayoutParser.cs    # "OP[15:10], 9:3, 2:0" <-> fields
│  │  └─ AppState.cs             # persisted state / preset model
│  └─ BitsConverter/             # Avalonia app
│     ├─ ViewModels/
│     ├─ Views/
│     └─ Services/               # settings, clipboard, file dialogs
├─ tests/
│  ├─ BitsConverter.Core.Tests/  # Core logic tests
│  └─ BitsConverter.Tests/       # Headless view-model tests
├─ docs/
└─ .github/workflows/build.yml
```

## 4. 功能規格

### 4.1 位寬

- 選項：8 / 16 / 24 / 32 bit（RadioButton 群組）。
- 切換時保留目前值：變寬補 0，變窄截掉高位。
- 內部值一律用 `uint`，`mask = 2^W - 1`。

### 4.2 Bit 方格

- 一個 byte 一列，MSB byte 在最上面；每列左側標示範圍（如 `[15:8]`），右側顯示該 byte 的 HEX。
- 每個 bit 一格：上方為 bit index，下方為 CheckBox。
- 每個 nibble 之間用較深的分隔線。
- bit 背景色對應所屬欄位的顏色（見 4.6）。

### 4.3 主值輸入與顯示

| 元件 | 說明 |
|---|---|
| DEC 輸入框 | 無號十進位，範圍 `0 ~ 2^W-1` |
| HEX 輸入框 | 可帶 `0x` 前綴，忽略 `_` 與空白，值不可超過 `2^W-1` |
| BIN 輸入框 | 可帶 `0b` 前綴，忽略 `_` 與空白，值不可超過 `2^W-1` |

- 每次輸入即時解析；合法時更新整體數值，其他輸入框與方格同步更新。
- 格式錯誤或超出範圍：輸入框顯示紅框與錯誤提示，數值不變。
- 正在編輯的輸入框不被重新格式化，避免游標跳動。
- 複製按鈕：HEX 複製為 `0x` 開頭且不含空格（如 `0xDEADBEEF`），BIN 複製時不含空格。

顯示區（每個值旁有複製按鈕）：

| 列 | HEX | BIN | DEC |
|---|---|---|---|
| Data | 補零到 `W/4` 位，每 2 位（1 byte）空一格 | 每 4 bit 空一格 | 無號 |
| Inverted Data | `~value & mask` | 同上 | 同上 |

Byte 陣列列：

- Little-Endian：`78 56 34 12`
- Big-Endian：`12 34 56 78`

### 4.4 操作按鈕

| 按鈕 | 行為 |
|---|---|
| Set All | 全部設 1 |
| Clear All | 全部清 0 |
| Invert | 全部反相 |
| `<<` / `>>` | 邏輯左移 / 右移 N 位，移出捨棄、補 0 |
| ROL / ROR | 在 W 位內循環左移 / 右移 N 位 |
| N | NumericUpDown，`1 ~ W-1`，預設 1 |

### 4.5 Bit-field 定義

資料模型：`BitField(Msb, Lsb, Name)`，`0 ≤ Lsb ≤ Msb ≤ 31`。

規則：

- 欄位不可重疊。
- 允許空隙，未被定義的 bit 視為「未分配」（unassigned），在欄位表中以灰色列顯示。
- 依 Msb 由高到低排序。

三種定義方式，彼此同步：

1. **文字輸入**（按 Apply 或 Enter 套用）：
   - 以 `,`、`;` 或換行分隔。
   - 每項可寫成 `15:10`、`[15:10]`、`7`、`NAME[15:10]`、`NAME=15:10`。
   - `msb:lsb` 順序寫反時自動交換。
   - 解析錯誤時顯示錯誤訊息，不套用。
   - 套用成功後文字會正規化為 `NAME[15:10], [9:3], [2:0]` 格式。
2. **點擊方格**：每兩個相鄰 bit 之間有一條分隔線，點擊後：
   - 兩側屬於同一段（欄位或未分配區）→ 在該處切開。切開欄位時上半段保留名稱；切開未分配區時兩段都變成新欄位。
   - 兩側屬於不同段 → 合併成一個欄位（名稱取上半段，若空則取下半段）。
   - 例：16-bit 無欄位時，點 10|9，再點 3|2 → `[15:10] [9:3] [2:0]`。
3. **欄位表**：可直接編輯名稱；`Clear` 按鈕清空全部欄位。

欄位名稱：選填，不可包含 `, ; = : [ ]` 與換行，前後空白自動去除。

### 4.6 欄位表

| 欄 | 內容 |
|---|---|
| 色塊 | 與方格背景同色 |
| Name | 可編輯 |
| Range | `[15:10]` |
| Bits | 欄位寬度 |
| HEX / DEC / BIN | 可編輯，旁邊附複製按鈕；輸入合法時反寫回主值 `value = (value & ~fieldMask) | (fieldValue << lsb)` |

- 輸入規則同 4.3，但範圍以欄位寬度計算。
- 只顯示無號數。

### 4.7 位寬縮小時的欄位

- 欄位定義保留。
- `Msb ≥ W` 的欄位標示為 out of range：欄位表該列以紅色提示、數值欄停用；方格中屬於該欄位的 bit 以紅色底顯示。
- 與 out-of-range 欄位相鄰的方格分隔線停用，避免產生重疊。
- 位寬調回後自動恢復正常。

### 4.8 Dec-Bin-Hex 對照表

保留原版的 Expander，顯示 0～F 的 DEC / BIN / HEX 對照。

### 4.9 儲存

- **自動儲存**：關閉視窗時把位寬、數值、欄位、Expander 狀態寫入設定檔，啟動時讀回。
  - Windows：`%APPDATA%\BitsConverter\settings.json`
  - Linux：`~/.config/BitsConverter/settings.json`
  - macOS：`~/Library/Application Support/BitsConverter/settings.json`（實際以 .NET `SpecialFolder.ApplicationData` 為準）
- **Preset**：`Save Preset…` / `Load Preset…` 以 JSON 存取欄位配置與位寬：

```json
{
  "width": 16,
  "fields": [
    { "name": "OPCODE", "msb": 15, "lsb": 10 },
    { "name": "RS", "msb": 9, "lsb": 3 },
    { "name": "", "msb": 2, "lsb": 0 }
  ]
}
```

- 讀取失敗（格式錯誤、欄位重疊、位寬不合法）時顯示錯誤訊息，不改變目前狀態。

## 5. 畫面配置

```
┌───────────────────────────────────────────────────────────────┐
│ Width: (•)8 ( )16 ( )24 ( )32                                  │
├───────────────────────────────────────────────────────────────┤
│ [31:24]  31 30 29 28 ┃ 27 26 25 24     0x00                    │
│          ☐  ☐  ☐  ☐  ┃ ☐  ☐  ☐  ☐                              │
│ ...  (one row per byte, MSB byte first)                        │
├───────────────────────────────────────────────────────────────┤
│ DEC [      ]  HEX [      ]  BIN [                    ]         │
│ [Set All] [Clear All] [Invert]  [<<] [>>] [ROL] [ROR]  N [1]   │
├───────────────────────────────────────────────────────────────┤
│            HEX          BIN                    DEC             │
│ Data       00FF ⧉       0000 0000 1111 1111 ⧉  255 ⧉           │
│ Inverted   FF00 ⧉       1111 1111 0000 0000 ⧉  65280 ⧉         │
│ Bytes LE   FF 00 ⧉      Bytes BE  00 FF ⧉                      │
├───────────────────────────────────────────────────────────────┤
│ Bit Fields  [OP[15:10], [9:3], [2:0]      ] [Apply] [Clear]    │
│             [Load Preset…] [Save Preset…]                      │
│ ■ OP     [15:10]  6   HEX[3F]⧉  DEC[63]⧉  BIN[111111]⧉        │
│ ■        [9:3]    7   ...                                      │
├───────────────────────────────────────────────────────────────┤
│ ▸ Dec-Bin-Hex Table                                            │
└───────────────────────────────────────────────────────────────┘
```

## 6. 測試範圍

| 模組 | 測試重點 |
|---|---|
| BitMath | mask、shift、rotate 在 8/16/24/32 的邊界；GetField / SetField |
| NumberParser | 前綴、`_`、空白、空字串、非法字元、溢位 |
| NumberFormatter | 補零、分組、byte 陣列兩種 endian |
| FieldLayoutParser | 各種寫法、名稱、反向範圍、重疊、超過 31、Format 往返 |
| FieldLayout | segments 計算、split / merge、out-of-range 判斷 |
| StateSerializer | Preset / 設定檔往返、錯誤檔案、預設值回退 |
| MainWindowViewModel（headless） | 輸入同步、錯誤輸入、位寬切換、移位、點擊切割、欄位反寫、改名、縮小位寬 |

## 7. 與原版的差異

| 項目 | 原版 | 新版 |
|---|---|---|
| UI 框架 | WPF（Windows only） | Avalonia（跨平台） |
| 位寬 | 8 | 8 / 16 / 24 / 32 |
| 輸入 | DEC、HEX | DEC、HEX、BIN，支援前綴與分隔字元 |
| 錯誤輸入 | 取最後 2 個字元，失敗時設 255 | 紅框提示，值不變 |
| 移位 | 左移 / 右移 1 位 | 左移 / 右移 / ROL / ROR，N 位 |
| 其他 | — | Invert、byte 陣列、複製按鈕、Bit-field、Preset、自動儲存、深色模式 |
