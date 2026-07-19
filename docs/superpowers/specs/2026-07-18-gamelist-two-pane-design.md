# GameList 2ペイン再設計 — 設計ドキュメント

**日付**: 2026-07-18
**対象**: `YuLauncher/Core/Window/Pages/GameList.xaml` / `GameList.xaml.cs`
**アプローチ**: C（GameListのみ漸進的2ペイン化）

## 背景

現状のGameListページは「AddButton + GenreComboBox + WrapPanel」のみの単一ペイン構成で、ゲームボタンはアイコン+テキスト横並びのリスト項目のみ。情報の視覚的階層がなく、各ゲームの詳細（Memo, Genre, WikiData, 実行オプション）を参照するにはContextMenu経由で別ウィンドウを開く必要がある。

Playnite風の2ペイン構成（リスト + 詳細）にすることで、ゲーム選択と詳細確認を同一画面で完結させる。

## スコープ

### 対象
- `YuLauncher/Core/Window/Pages/GameList.xaml`
- `YuLauncher/Core/Window/Pages/GameList.xaml.cs`

### 対象外（本設計では変更しない）
- `WebGameList.xaml` / `WebSaverList.xaml` — 漸進的展開の対象外。後続フェーズで対応
- `MainPage.xaml` / `MainWindow.xaml` — ナビ構造維持
- `JsonControl.ApplicationJsonData` スキーマ — 変更なし
- `PageControlCreate.GameListShowContextMenu` — 既存ContextMenu経路は維持

### 既知の妥協
- 漸進的展開のため、GameListのみ2ペイン化しWebGameList/WebSaverListは従来UIのまま。一時的にUI不揃い（ユーザー承認済み）

## レイアウト

```
GameList.xaml (Page)
└── Grid (2列: ListPane 280px固定 / DetailPane *)
    ├── ListPane (左, 幅280固定)
    │   ├── Header (縦積み)
    │   │   ├── AddButton (アイコンのみ)
    │   │   ├── GenreComboBox (既存再利用)
    │   │   └── SearchBox (タイトル部分一致絞り込み)
    │   └── ScrollViewer
    │       └── ListBox (ゲームリスト, ItemsSource=ObservableCollection)
    │           各項目: StackPanel(Horizontal)
    │             ├── Image (32x32, 角丸)
    │             └── StackPanel(Vertical)
    │                 ├── TextBlock (タイトル, 14px)
    │                 └── TextBlock (種別, 11px, アクセント色)
    │           選択状態: 左2pxアクセントボーダー + 背景薄
    │           ホバー: 背景のみ
    └── DetailPane (右, *)
        ├── 未選択時: 空状態
        │   └── Centered StackPanel
        │       ├── ui:SymbolIcon (Home12, 48px, 薄色)
        │       └── TextBlock "ゲームを選択してください"
        └── 選択時: ScrollViewer
            ├── Hero領域
            │   └── Image (アイコン拡大 128x128, 角丸8)
            ├── TextBlock (タイトル, 24px, Bold)
            ├── 種別バッジ (FileExtension, chip)
            ├── Genre chips (WrapPanel, 全ジャンル, アクセント色chip)
            ├── Memo (Expander, 折りたたみ)
            ├── WikiData (ItemsControl, key-value)
            ├── 実行情報 (StackPanel)
            │   ├── IsUseLog (アイコン+ON/OFF)
            │   ├── IsWebView (アイコン+ON/OFF)
            │   ├── IsMute (アイコン+ON/OFF)
            │   └── Volume (値表示)
            └── Action buttons (横並び, Wrap)
                ├── ▶ 再生 (primary, accent fill)
                ├── ⚙ プロパティ (PropertyDialog)
                ├── 📝 メモ (MemoWindow)
                ├── 📖 Wiki管理 (WikiDataManageWindow)
                └── 🏷 ジャンル管理 (GenreManageWindow)
```

## データフロー

### リスト項目選択 → 詳細表示

1. ユーザーがリスト項目クリック
2. `ListBox.SelectionChanged` 発火
3. 選択された `ApplicationJsonData` を `DetailPane.DataContext` に設定
4. DetailPane内の各要素がBindingで表示更新

### 検索ボックス

1. `SearchBox.TextChanged` 発火
2. 入力テキストで `_allGames` (List<ApplicationJsonData>) をタイトル部分一致でフィルタ
3. `GameListBox.ItemsSource` を更新

### GenreComboBox選択

既存の `GenreComboBox_OnSelectionChanged` / `GenreComboBox_OnLoaded` / `LoadGenre` / `GenreAllUpdate` のロジックを維持。ただし `Panel.Children.Add(_gameButton.GameButtonShow(...))` で直接Buttonを追加していた箇所を、`_allGames` リストに `ApplicationJsonData` を追加する形に変更。実際のUI要素生成はListBoxのItemTemplateに任せる。`GenreExeUpdate` / `GenreWebUpdate` はデッドコード（呼び出し元なし）のため削除。`Initialize()` は `LoadAllGames()` に統合して削除。

## コンポーネント詳細

### ListPane

- **AddButton**: 既存 `AddButton_OnClick` 再利用（CreateGameDialog表示）
- **GenreComboBox**: 既存のコードビハインドロジック全て維持
- **SearchBox**: `ui:TextBox` 使用。`TextChanged` でフィルタ実行
- **ListBox**: `ItemsSource` に `ObservableCollection<ApplicationJsonData>` をバインド。`SelectedIndex`/`SelectedItem` で詳細ペイン連携
  - ItemTemplate: `DataTemplate` で `Image + TextBlock` 構成
  - 選択状態: `ListBoxItem` の既定スタイルをオーバーライドし、左2pxアクセントボーダー + 背景薄を適用

### DetailPane

- **空状態**: `ContentControl` または `DataTemplate` の切替で、`DataContext == null` 時にプレースホルダ表示
- **Hero Image**: 既存 `GameButton.GetImage` と同じロジックでアイコン取得。128px角丸で表示
- **Genre chips**: `ItemsControl` + `WrapPanel` で `Genre[]` を横並び。各chipは `ui:Badge` または `Border` ベース
- **Memo**: `ui:Expander` または `Expander`。`string.IsNullOrEmpty(Memo)` 時は非表示
- **WikiData**: `ItemsControl` で `Dictionary<string,string>` をkey-value表示
- **実行情報**: 各boolフィールドをアイコン+状態テキストで表示。null許容のものは非表示
- **Action buttons**:
  - **再生**: 既存 `GameButton.LaunchApplication(data)` を呼ぶ。`async void` ハンドラ。`MultipleLaunch` が設定されている場合は既存 `GameButtonShow` のClickハンドラと同様にループで同時起動する（既存ロジックを抽出・再利用）
  - **プロパティ**: 既存 `PropertyDialog` を開く
  - **メモ**: 既存 `MemoWindow` を開く
  - **Wiki管理**: 既存 `WikiDataManageWindow` を開く
  - **ジャンル管理**: 既存 `GenreManageWindow` を開く

## 既存ロジックの再利用

| 処理 | 既存実装 | 再利用方法 |
|------|---------|-----------|
| ゲーム起動 | `GameButton.LaunchApplication` (private static) | `internal static` に変更。`GameList` から直接呼ぶ |
| アイコン取得 | `GameButton.GetImage` (private static) | `internal static` に変更。`GameList` から直接呼ぶ |
| ジャンル読み込み | `LoadGenre`, `GenreAllUpdate` | `LoadGenre` のロジック維持。`GenreAllUpdate` / `GenreExeUpdate` / `GenreWebUpdate` / `Initialize` は新しい `LoadAllGames` / `LoadGamesByGenre` に統合し削除 |
| ファイル更新通知 | `PageControlCreate.DeleteFileMenuClicked`, `CreateGameDialog.CloseObservable`, `PropertyDialog.AllGameListPanelUpdate` | Subscribe維持 |
| ContextMenu | `PageControlCreate.GameListShowContextMenu` | ListBoxの `ItemContainerStyle` で各 `ListBoxItem` に `ContextMenu` を設定（`Tag` = `ApplicationJsonData`）。`GameList_OnPreviewMouseRightButtonDown` は `ListBoxItem` を探すよう書き直し、`Tag` から `GameListShowContextMenu(true, data)` を構築 |

### アクセス修飾子の調整

`GameButton.LaunchApplication` と `GetImage` は現在 `private static`。`GameList` クラスから呼ぶには同じ `PageControlCreate.cs` 内での `internal` 化、または public メソッドの追加が必要。設計方針：`LaunchApplication` を `internal static` に変更し、`GameButton` インスタンス経由ではなく直接呼べるようにする。

## 未選択時の表示

空状態では以下を中央表示:
- `ui:SymbolIcon Symbol="Home12"` (48px, 薄色)
- ローカライズテキスト "ゲームを選択してください" (リソース追加必要)

## ローカライズ

新規追加が必要なリソースキー（`Properties/Language.resx` / `LangString.resx`）:
- `SelectGamePrompt` — "ゲームを選択してください"
- `DetailPlay` — "再生"
- `DetailProperty` — "プロパティ"
- `DetailMemo` — "メモ"
- `DetailWikiManage` — "Wiki管理"
- `DetailGenreManage` — "ジャンル管理"
- `DetailMemoEmpty` — "メモなし"
- `DetailWikiEmpty` — "Wikiデータなし"
- `DetailNoGames` — "ゲームがありません"
- `SearchPlaceholder` — "検索..."

既存の `lex:Loc` バインディングで使用。

## エラー処理

- `JsonControl.ReadExeJson` 例外: 既存通り `LoggerController.LogError` で記録、スキップ
- リスト項目クリック時の `LaunchApplication` 例外: 既存 `GameButton.GameButtonShow` 内のtry-catchを維持
- `GenreComboBox` 操作の例外: 既存try-catch維持
- アクションボタンからのウィンドウオープン失敗: 各ハンドラでtry-catch

## テスト方針

本変更はUIレイアウト変更のため、既存のテストスイートが通ることを確認。新規自動テストは追加しない（WPF UIの単体テストは既存プロジェクトに無く、導入しない）。

検証項目:
- [ ] ビルド成功（0 warnings, 0 errors）
- [ ] アプリ起動後、GameListページが表示される
- [ ] ゲームリストが表示される（既存JSON読み込み）
- [ ] リスト項目クリックで詳細ペインが更新される
- [ ] 各アクションボタンが機能する
- [ ] 検索ボックスで絞り込みが動作する
- [ ] GenreComboBoxのフィルタが動作する
- [ ] ContextMenu（右クリック）が既存通り動作する
- [ ] WebGameList/WebSaverListページが変更なく動作する

## 実装順序（実装計画で詳細化）

1. GameList.xaml の2ペインGrid構造への書き換え
2. ListPane の実装（AddButton + GenreComboBox + SearchBox + ListBox）
3. DetailPane の空状態実装
4. DetailPane の選択時レイアウト実装（Hero, タイトル, chips, Memo, WikiData, 実行情報）
5. Action buttons の実装（既存ウィンドウ呼び出し）
6. GameList.xaml.cs のリファクタ（Panel.Children.Add → ObservableCollection）
7. SearchBox フィルタロジック実装
8. `GameButton.LaunchApplication` / `GetImage` のアクセス修飾子調整
9. ローカライズリソース追加
10. ビルド検証 + 手動動作確認

## 非目標

- WebGameList / WebSaverList の2ペイン化（後続フェーズ）
- ViewModel / MVVM パターンの導入（既存コードビハインドスタイル維持）
- ApplicationJsonData スキーマ変更（ヒーロー画像フィールド等は追加しない）
- アニメーション / トランジション効果の追加
- アクセントカラーやテーマの動的変更機能
