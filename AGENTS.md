# AGENTS.md — YuLauncher 開発ノウハウ

このファイルは、AI エージェント（および人間の開発者）が本プロジェクトで作業する際に、過去にハマった落とし穴とその解決策を記録したものです。同じ罠に嵌らないように、作業前に必ず読んでください。

## 目次

1. [WPF-UI の TextBlock とダークモード](#1-wpf-ui-の-textblock-とダークモード)
2. [ThemePagePatcher.PatchTheme の副作用](#2-themepagepatcherpatchtheme-の副作用)
3. [XAML 属性の設定タイミングとコンストラクタ](#3-xaml-属性の設定タイミングとコンストラクタ)
4. [アクセス修飾子の調整（GameButton）](#4-アクセス修飾子の調整gamebutton)
5. [ListBox 化と右クリック ContextMenu](#5-listbox-化と右クリック-contextmenu)
6. [デッドコードの除去](#6-デッドコードの除去)
7. [デバッグ用ローカルファイルの除外](#7-デバッグ用ローカルファイルの除外)
8. [Git ブランチ運用](#8-git-ブランチ運用)

---

## 1. WPF-UI の TextBlock とダークモード

### 現象

`ui:TextBlock`（`Wpf.Ui.Controls.TextBlock`）を XAML に配置しても、ダークモードでテキストが黒のままになり、背景に溶けて見えない。

### 原因

WPF-UI は `TextBlockMetadata.cs` で `System.Windows.Controls.TextBlock.ForegroundProperty` のメタデータを**グローバルにオーバーライド**し、デフォルトを `Brushes.Black` に強制しています：

```csharp
// wpfui/src/Wpf.Ui/Controls/TextBlock/TextBlockMetadata.cs:64-79
System.Windows.Controls.TextBlock.ForegroundProperty.OverrideMetadata(
    typeof(System.Windows.Controls.TextBlock),
    new FrameworkPropertyMetadata(
        Brushes.Black,  // ← デフォルト黒
        null,
        static (d, value) =>
        {
            if (d.GetValue(TextBlock.AppearanceForegroundProperty) is Brush brush)
                return brush;  // ← Appearance設定時のみテーマ追従
            return value is Brush ? value : Brushes.Black;  // ← 未設定なら黒
        }
    )
);
```

CoerceValue コールバックのロジック：
- `AppearanceForegroundProperty` が設定されている → そのブラシを返す（テーマ追従）
- ローカル値（`Foreground="White"` 等）がある → その値を返す
- 未設定 → `Brushes.Black`

`AppearanceForegroundProperty` は `Appearance` プロパティ（`TextColor` 列挙型）を設定した時だけ `OnAppearanceChanged` 経由でセットされます。

### 解決策

テーマ追従させたい `ui:TextBlock` には `Appearance` プロパティを設定する：

```xml
<!-- メインテキスト（ダーク=白、ライト=黒） -->
<ui:TextBlock Text="..." Appearance="Primary"/>

<!-- セカンダリテキスト（少しグレー） -->
<ui:TextBlock Text="..." Appearance="Secondary"/>
```

| `Appearance` の値 | マッピングされるリソースキー |
|---|---|
| `Primary` | `TextFillColorPrimaryBrush` |
| `Secondary` | `TextFillColorSecondaryBrush` |
| `Tertiary` | `TextFillColorTertiaryBrush` |
| `Disabled` | `TextFillColorDisabledBrush` |

### 注意

- `Foreground="{DynamicResource TextFillColorPrimaryBrush}"` でも動作するが、**非推奨**。リソースキー名への直接依存が発生し、ライブラリアップグレード時の耐性が下がる。公式 API である `Appearance` プロパティを使うこと。
- アクセント色背景上のテキスト（バッジ、チップ、アクセントボタン内）は `Appearance` を設定せず、明示的に `Foreground="White"` を指定する。CoerceValue コールバックがローカル値を優先するため、白が保持される。

---

## 2. ThemePagePatcher.PatchTheme の副作用

### 現象

`ThemePagePatcher.PatchTheme(this)` を呼ぶと、ページ内のすべての `TextBlock` の `Foreground` が強制上書きされ、`Foreground="White"` の明示指定も黒に変わる。

### 原因

`ThemePagePatcher.PatchTheme` は `System.Windows.Controls.TextBlock` を継承する**すべて**の `TextBlock`（`Wpf.Ui.Controls.TextBlock` 含む）を走査し、`Foreground` を強制上書きする実装：

```csharp
// YuLauncher/Core/lib/ThemePagePatcher.cs
public static void PatchTheme(Page page)
{
    var themeControl = new ThemeService().GetTheme();
    switch (themeControl)
    {
        case ApplicationTheme.Dark:
            var textBlocks = VisualTreeHelperExtensions.FindVisualChildren<TextBlock>(page);
            textBlocks.ToList().ForEach(x => x.Foreground = new SolidColorBrush(Colors.White));
            break;
        case ApplicationTheme.Light:
            // ...黒に上書き...
            break;
    }
}
```

問題点：
- `ThemeService().GetTheme()` が実際の `App.xaml` の `ThemesDictionary Theme="Dark"` と一致しない場合がある（`Light` を返す）
- その場合、ダークモードなのにすべての `TextBlock` が黒になる
- `Foreground="White"` の明示指定も上書きされるため、再設定が必要になる

### 解決策

**`PatchTheme` を使わず、`Appearance` プロパティを使用する。**

新しいページでは `PatchTheme` を呼ばず、各 `ui:TextBlock` に `Appearance="Primary"` / `Appearance="Secondary"` を設定する。これにより `App.xaml` の `ThemesDictionary Theme="Dark"` が自動的に効く。

### 既存ページの扱い

`General.xaml.cs` / `VideoGraphics.xaml.cs` は `PatchTheme` を使っているが、これらを修正する場合は `Appearance` プロパティに移行すること。`PatchTheme` を使う既存コードは徐々に置き換えていく。

---

## 3. XAML 属性の設定タイミングとコンストラクタ

### 現象

UserControl に `FileExtensionFilter` プロパティを XAML 属性で設定したが、コンストラクタ内でデータ読み込みを実行すると、常にデフォルト値が使われる。

### 原因

WPF の XAML 属性は**コンストラクタ実行後**に設定される。そのため、コンストラクタで `LoadGenre()` を呼ぶと、`FileExtensionFilter` がまだデフォルト値（`"All"`）のまま読み込みが走る。

```csharp
// NG: コンストラクタで LoadGenre を呼ぶ
public GameListPaneControl()
{
    InitializeComponent();
    _ = LoadGenre();  // ← FileExtensionFilter がまだ "All" のまま
}

// XAML で設定されるのはコンストラクタの後
// <local:GameListPaneControl FileExtensionFilter="WebGame"/>
```

### 解決策

`Loaded` イベントでデータ読み込みを実行する：

```csharp
public GameListPaneControl()
{
    InitializeComponent();
    // LoadGenre() は呼ばない
}

private void OnLoaded(object sender, RoutedEventArgs e)
{
    _ = LoadGenre();  // ← FileExtensionFilter が設定された後に実行
}
```

XAML に `Loaded="OnLoaded"` を設定する。

### 一般則

UserControl や Control にプロパティを XAML 属性で渡し、そのプロパティに依存する初期化処理がある場合は、**必ず `Loaded` イベントで初期化する**。コンストラクタでは XAML 属性がまだ設定されていない。

---

## 4. アクセス修飾子の調整（GameButton）

### 現象

`GameButton.LaunchApplication` と `GameButton.GetImage` が `private static` で、外部クラスから呼べない。

### 解決策

`internal static` に変更する：

```csharp
// YuLauncher/Core/lib/PageControlCreate.cs
internal static async Task LaunchApplication(JsonControl.ApplicationJsonData data) { ... }
internal static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData) { ... }
```

`internal` により、同じアセンブリ内からアクセス可能。`public` にする必要はない（外部ライブラリに公開しない）。

### 一般則

既存の `private` メソッドを別クラスから呼びたい場合、まず `internal` を検討する。`public` は API として外部に公開する場合のみ使う。

---

## 5. ListBox 化と右クリック ContextMenu

### 現象

`WrapPanel` + `GameButton`（`Button` 派生）の構成から `ListBox` + `DataTemplate` に移行した際、右クリックの ContextMenu が機能しなくなる。削除/メモ/プロパティのメニューが消え、追加メニューしか出ない。

### 原因

既存の `OnPreviewMouseRightButtonDown` は `Button` を探して `button.ContextMenu` を読んでいた。しかし `ListBox` 化後はリスト項目が `ListBoxItem` になるため、`source is Button` がマッチせず、常に else 節（追加のみのメニュー）にフォールスルーする。

### 解決策

`ListBox.ItemContainerStyle` で各 `ListBoxItem` の `Tag` に `ApplicationJsonData` をバインドし、`OnPreviewMouseRightButtonDown` で `ListBoxItem` を探す：

```xml
<ListBox x:Name="GameListBox">
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="Tag" Value="{Binding Data}"/>
        </Style>
    </ListBox.ItemContainerStyle>
    <!-- ItemTemplate ... -->
</ListBox>
```

```csharp
private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
{
    DependencyObject? source = e.OriginalSource as DependencyObject;
    while (source != null && source is not ListBoxItem)
    {
        source = VisualTreeHelper.GetParent(source);
    }

    if (source is ListBoxItem listItem && listItem.Tag is JsonControl.ApplicationJsonData data)
    {
        ContextMenu = PageControlCreate.GameListShowContextMenu(true, data);
    }
    else
    {
        ContextMenu = PageControlCreate.GameListShowContextMenu(false, new JsonControl.ApplicationJsonData());
    }
}
```

### 注意

`ApplicationJsonData` は `struct` なので、`Tag` には boxed される。`is` パターンマッチで正しく unbox できる。`as` は nullable-struct 問題で使えない。

### 一般則

UI 構成を変える（`Button` → `ListBox` 等）際は、イベントハンドラの `sender`/`OriginalSource` の型も変わることに注意。右クリックメニューだけでなく、ドラッグ＆ドロップ等の他のイベントも見直すこと。

---

## 6. デッドコードの除去

### 現象

`GameList.xaml.cs` に `GenreExeUpdate` / `GenreWebUpdate` / `Initialize` などのメソッドが定義されているが、呼び出し元がない。`GenreAllUpdate` のみが使われていた。

### 解決策

リファクタリング時にデッドコードを削除する。`grep` で呼び出し元を確認してから削除：

```bash
grep -rn "GenreExeUpdate\|GenreWebUpdate" --include="*.cs" YuLauncher/
```

呼び出し元がない場合は削除。ただし、外部からリフレクションで呼ばれている可能性もあるので、完全に安全か確認すること。

### 一般則

リファクタリング時は、変更対象のメソッドの呼び出し元を必ず `grep` で確認する。呼び出し元がないメソッドはデッドコードとして削除する。ただし、リフレクションや DI で動的に呼ばれている場合は残す。

---

## 7. デバッグ用ローカルファイルの除外

### 現象

`Games/Syani.json`、`gameList.toml`、`settings.toml` がデバッグ時のみ使用するローカルファイルなのに、Git のトラッキング対象になっていた。

### 解決策

`.gitignore` に追加してトラッキングから除外：

```gitignore
# Debug files
Games/Syani.json
gameList.toml
settings.toml
```

既にコミット済みのファイルは `git rm --cached` でトラッキングから削除：

```bash
git rm --cached Games/Syani.json gameList.toml settings.toml
```

### 一般則

デバッグ用、ローカル環境固有の設定ファイル、一時ファイルは `.gitignore` に追加する。特に、ユーザー名やパスワードを含むファイルは絶対にコミットしない。`.env`、`*.local.json`、`settings.local.toml` 等のパターンも検討する。

---

## 8. Git ブランチ運用

### 現在の運用

- **メインブランチ**: `main`
- **開発ブランチ**: `Dev4`（以降、機能別ではなく Dev 番号で管理）
- 以降の push は `origin/Dev4` に対して行う

### コマンド

```bash
# 現在のブランチ確認
git branch --show-current

# Dev4 に push
git push

# ブランチを切り替え
git checkout Dev4
```

### 一般則

機能別ブランチ（`feature/xxx`）ではなく、開発フェーズごとのブランチ（`Dev3`, `Dev4`, ...）で管理する。大きな機能追加でも新しい `DevN` ブランチを作るのではなく、現在の `Dev` ブランチに積み上げる。ある程度溜まったら `main` にマージする。

---

## その他の注意点

### wpfui リポジトリのクローン

WPF-UI のソースを調査するために `wpfui/` ディレクトリにクローンすることがあるが、これは `.gitignore` に追加してトラッキングしない：

```gitignore
wpfui/
```

### Localization

- リソースキーは `YuLauncher/LangString/Language.resx`（デフォルト/英語）と `Language.ja-JP.resx`（日本語）の両方に追加する
- XAML からは `lex:Loc` で参照する
- C# コードからは `Language.Xxx` 静的アクセッサを使わず、`lex:Loc` のみを使用する（`Language.Designer.cs` の再生成が不要）
