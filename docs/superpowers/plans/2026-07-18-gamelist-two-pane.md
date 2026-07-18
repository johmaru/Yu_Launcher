# GameList 2ペイン再設計 実装計画

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** GameListページをPlaynite風2ペイン構成（左リスト+右詳細）に再構築し、ゲーム選択と詳細確認・操作を同一画面で完結させる。

**Architecture:** 既存のコードビハインドスタイルを維持しつつ、ListBox + DataTemplateでゲームリストを表示し、選択時に右ペインのDetailPaneのDataContextを差し替えて詳細を表示。既存のGenreComboBoxロジックは維持し、Panel.Children.AddではなくObservableCollectionでリスト管理。アクションボタンは既存のPropertyDialog/MemoWindow/WikiDataManageWindow/GenreManageWindowを呼び出し、再生はGameButton.LaunchApplicationをinternal static化して直接呼ぶ。

**Tech Stack:** WPF, WPF-UI (Fluent), wpflocalizeextension, C# 10, .NET

## Global Constraints

- 対象ファイル: `YuLauncher/Core/Window/Pages/GameList.xaml`, `GameList.xaml.cs`, `YuLauncher/Core/lib/PageControlCreate.cs`, `YuLauncher/Core/lib/ObjectProperty.cs`, `YuLauncher/LangString/Language.resx`, `YuLauncher/LangString/Language.ja-JP.resx`
- 既存のナビ構造（MainPage/MainWindow）は変更しない
- ApplicationJsonDataスキーマは変更しない
- WebGameList/WebSaverListは変更しない
- 既存の右クリックContextMenu（PageControlCreate.GameListShowContextMenu）は維持
- ローカライズは `lex:Loc` バインディングで、リソースキーは `YuLauncher/LangString/Language.resx`（デフォルト）と `Language.ja-JP.resx`（日本語）の両方に追加
- ビルドは `dotnet build` で0 warnings / 0 errors
- TDDは適用しない（既存プロジェクトにWPF UI単体テスト無し、設計ドキュメントで不採用決定）

---

## File Structure

- `YuLauncher/Core/Window/Pages/GameList.xaml` — 2ペインGrid構造に書き換え。ListPane（左280px）+ DetailPane（右*）
- `YuLauncher/Core/Window/Pages/GameList.xaml.cs` — Panel.Children.AddベースからObservableCollectionベースへリファクタ。ListBox選択でDetailPane連携。SearchBoxフィルタ。アクションボタンハンドラ
- `YuLauncher/Core/lib/PageControlCreate.cs` — `GameButton.LaunchApplication` と `GetImage` を `private static` → `internal static` に変更
- `YuLauncher/Core/lib/ObjectProperty.cs` — リスト項目とHeroのサイズ定数を追加
- `YuLauncher/LangString/Language.resx` — 新規ローカライズキー追加（デフォルト/英語）
- `YuLauncher/LangString/Language.ja-JP.resx` — 新規ローカライズキー追加（日本語）

---

### Task 1: ローカライズリソースの追加

**Files:**
- Modify: `YuLauncher/LangString/Language.resx`
- Modify: `YuLauncher/LangString/Language.ja-JP.resx`

**Interfaces:**
- Produces: 以下のリソースキー（`lex:Loc` で参照可能）
  - `SelectGamePrompt`, `DetailPlay`, `DetailProperty`, `DetailMemo`, `DetailWikiManage`, `DetailGenreManage`, `DetailMemoEmpty`, `DetailWikiEmpty`, `DetailNoGames`, `SearchPlaceholder`, `DetailLaunchOptions`, `DetailNoMemo`, `DetailNoWiki`

- [ ] **Step 1: Language.resx に新規キーを追加**

`</root>` の直前に以下を追加:

```xml
  <data name="SelectGamePrompt" xml:space="preserve">
    <value>Please select a game</value>
  </data>
  <data name="DetailPlay" xml:space="preserve">
    <value>Play</value>
  </data>
  <data name="DetailProperty" xml:space="preserve">
    <value>Property</value>
  </data>
  <data name="DetailMemo" xml:space="preserve">
    <value>Memo</value>
  </data>
  <data name="DetailWikiManage" xml:space="preserve">
    <value>Wiki Manage</value>
  </data>
  <data name="DetailGenreManage" xml:space="preserve">
    <value>Genre Manage</value>
  </data>
  <data name="DetailMemoEmpty" xml:space="preserve">
    <value>No memo</value>
  </data>
  <data name="DetailWikiEmpty" xml:space="preserve">
    <value>No wiki data</value>
  </data>
  <data name="DetailNoGames" xml:space="preserve">
    <value>No games</value>
  </data>
  <data name="SearchPlaceholder" xml:space="preserve">
    <value>Search...</value>
  </data>
  <data name="DetailLaunchOptions" xml:space="preserve">
    <value>Launch Options</value>
  </data>
  <data name="DetailNoMemo" xml:space="preserve">
    <value>No memo</value>
  </data>
  <data name="DetailNoWiki" xml:space="preserve">
    <value>No wiki data</value>
  </data>
```

注意: `DetailMemoEmpty` と `DetailNoMemo` は同義だが、既存の命名規則に合わせ両方作成（使用箇所で統一）。実際の使用は `DetailNoMemo` / `DetailNoWiki` のみとし、`DetailMemoEmpty` / `DetailWikiEmpty` は作成しない（このステップでは上記から `DetailMemoEmpty` / `DetailWikiEmpty` を除外すること）。

修正後の追加ブロック（実際に使用するキーのみ）:

```xml
  <data name="SelectGamePrompt" xml:space="preserve">
    <value>Please select a game</value>
  </data>
  <data name="DetailPlay" xml:space="preserve">
    <value>Play</value>
  </data>
  <data name="DetailProperty" xml:space="preserve">
    <value>Property</value>
  </data>
  <data name="DetailMemo" xml:space="preserve">
    <value>Memo</value>
  </data>
  <data name="DetailWikiManage" xml:space="preserve">
    <value>Wiki Manage</value>
  </data>
  <data name="DetailGenreManage" xml:space="preserve">
    <value>Genre Manage</value>
  </data>
  <data name="DetailNoMemo" xml:space="preserve">
    <value>No memo</value>
  </data>
  <data name="DetailNoWiki" xml:space="preserve">
    <value>No wiki data</value>
  </data>
  <data name="DetailNoGames" xml:space="preserve">
    <value>No games</value>
  </data>
  <data name="SearchPlaceholder" xml:space="preserve">
    <value>Search...</value>
  </data>
  <data name="DetailLaunchOptions" xml:space="preserve">
    <value>Launch Options</value>
  </data>
```

- [ ] **Step 2: Language.ja-JP.resx に日本語訳を追加**

`</root>` の直前に以下を追加:

```xml
  <data name="SelectGamePrompt" xml:space="preserve">
    <value>ゲームを選択してください</value>
  </data>
  <data name="DetailPlay" xml:space="preserve">
    <value>再生</value>
  </data>
  <data name="DetailProperty" xml:space="preserve">
    <value>プロパティ</value>
  </data>
  <data name="DetailMemo" xml:space="preserve">
    <value>メモ</value>
  </data>
  <data name="DetailWikiManage" xml:space="preserve">
    <value>Wiki管理</value>
  </data>
  <data name="DetailGenreManage" xml:space="preserve">
    <value>ジャンル管理</value>
  </data>
  <data name="DetailNoMemo" xml:space="preserve">
    <value>メモなし</value>
  </data>
  <data name="DetailNoWiki" xml:space="preserve">
    <value>Wikiデータなし</value>
  </data>
  <data name="DetailNoGames" xml:space="preserve">
    <value>ゲームがありません</value>
  </data>
  <data name="SearchPlaceholder" xml:space="preserve">
    <value>検索...</value>
  </data>
  <data name="DetailLaunchOptions" xml:space="preserve">
    <value>起動オプション</value>
  </data>
```

- [ ] **Step 3: ビルドでリソースが正しく読み込まれることを確認**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 4: コミット**

```bash
git add YuLauncher/LangString/Language.resx YuLauncher/LangString/Language.ja-JP.resx
git commit -m "feat(i18n): add GameList detail pane localization keys"
```

---

### Task 2: GameButton のアクセス修飾子調整

**Files:**
- Modify: `YuLauncher/Core/lib/PageControlCreate.cs:175`, `YuLauncher/Core/lib/PageControlCreate.cs:217`, `YuLauncher/Core/lib/PageControlCreate.cs:294`, `YuLauncher/Core/lib/PageControlCreate.cs:315`

**Interfaces:**
- Produces:
  - `internal static Task LaunchApplication(JsonControl.ApplicationJsonData data)` — ゲーム起動。MultipleLaunchループ処理を含む
  - `internal static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData)` — アイコン取得

- [ ] **Step 1: LaunchApplication を internal static に変更**

`PageControlCreate.cs` の `private static async Task LaunchApplication(JsonControl.ApplicationJsonData data)` を `internal static async Task LaunchApplication(JsonControl.ApplicationJsonData data)` に変更。

```csharp
internal static async Task LaunchApplication(JsonControl.ApplicationJsonData data)
{
    switch (data.FileExtension)
    {
        case "exe":
            await LaunchExe(data);
            break;
        case "web":
            LaunchWeb(data);
            break;
        case "WebGame":
            new GameWindow(data.Url, data.JsonPath).Show();
            break;
        case "WebSaver":
            new WebSaverWindow.WebSaverWindow(data.Name, data).Show();
            break;
        case "":
            break;
    }
}
```

- [ ] **Step 2: GetImage を internal static に変更**

`PageControlCreate.cs` の `private static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData)` を `internal static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData)` に変更。

```csharp
internal static BitmapImage? GetImage(JsonControl.ApplicationJsonData appData)
{
    // WebGame, WebSaver, web all use favicon from URL
    if (appData.FileExtension is "WebGame" or "WebSaver" or "web")
    {
        BitmapImage bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri("https://www.google.com/s2/favicons?domain=" + appData.Url);
        bitmap.EndInit();
        return bitmap;
    }

    if (!File.Exists(appData.FilePath)) return null;
    using (MemoryStream memoryStream = new MemoryStream())
    {
        Icon? icon = System.Drawing.Icon.ExtractAssociatedIcon(appData.FilePath);
        if (icon != null) icon.Save(memoryStream);
        memoryStream.Position = 0;

        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }
}
```

- [ ] **Step 3: ビルド確認**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 4: コミット**

```bash
git add YuLauncher/Core/lib/PageControlCreate.cs
git commit -m "refactor(lib): expose GameButton.LaunchApplication/GetImage as internal"
```

---

### Task 3: ObjectProperty にリスト項目・Heroサイズ定数を追加

**Files:**
- Modify: `YuLauncher/Core/lib/ObjectProperty.cs`

**Interfaces:**
- Produces:
  - `ObjectProperty.ListItemHeight` = 56
  - `ObjectProperty.ListItemIconSize` = 32
  - `ObjectProperty.HeroIconSize` = 128

- [ ] **Step 1: 定数を追加**

```csharp
namespace YuLauncher.Core.lib;

public struct ObjectProperty
{
    public const double GameListObjectHeight = 60;
    public const double GameListObjectWidth = 200;
    public const double ListItemHeight = 56;
    public const double ListItemIconSize = 32;
    public const double HeroIconSize = 128;
}
```

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/lib/ObjectProperty.cs
git commit -m "feat(lib): add list item and hero icon size constants"
```

---

### Task 4: GameList.xaml の2ペインGrid構造への書き換え

**Files:**
- Modify: `YuLauncher/Core/Window/Pages/GameList.xaml`（全体書き換え）

**Interfaces:**
- Produces: 以下のXAML要素（code-behindから名前参照）
  - `Grid x:Name="MainGrid"` — AllowDrop/DragEnter維持
  - `Grid x:Name="ListPane"` — 左ペイン
  - `ui:Button x:Name="AddButton"` — 既存
  - `ComboBox x:Name="GenreComboBox"` — 既存
  - `ComboBoxItem x:Name="GenreAllComboBoxItem"` — 既存
  - `ui:TextBox x:Name="SearchBox"` — 新規
  - `ListBox x:Name="GameListBox"` — 新規。ItemsSourceバインド用
  - `Grid x:Name="DetailPane"` — 右ペイン。DataContextにApplicationJsonDataを設定
  - `Border x:Name="EmptyState"` — 未選択時表示
  - `ScrollViewer x:Name="DetailScrollViewer"` — 選択時スクロール
  - `Image x:Name="HeroImage"` — Hero領域
  - `TextBlock x:Name="DetailTitleTextBlock"` — タイトル
  - `Border x:Name="FileTypeBadge"` — 種別バッジ
  - `ItemsControl x:Name="GenreChipsControl"` — Genre chips
  - `Expander x:Name="MemoExpander"` — Memo
  - `TextBlock x:Name="MemoTextBlock"` — Memo本文
  - `ItemsControl x:Name="WikiDataControl"` — WikiData
  - `TextBlock x:Name="LaunchOptionsTextBlock"` — 実行情報ヘッダ
  - `StackPanel x:Name="LaunchOptionsPanel"` — 実行情報
  - `Button x:Name="PlayButton"` — 再生
  - `Button x:Name="PropertyButton"` — プロパティ
  - `Button x:Name="MemoButton"` — メモ
  - `Button x:Name="WikiManageButton"` — Wiki管理
  - `Button x:Name="GenreManageButton"` — ジャンル管理

- [ ] **Step 1: GameList.xaml を全体書き換え**

```xml
<Page x:Class="YuLauncher.Core.Window.Pages.GameList"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
      xmlns:lex="http://wpflocalizeextension.codeplex.com"
      lex:LocalizeDictionary.DesignCulture="ja-JP"
      lex:ResxLocalizationProvider.DefaultAssembly="YuLauncher"
      lex:ResxLocalizationProvider.DefaultDictionary="Language"
      xmlns:local="clr-namespace:YuLauncher.Core.Window.Pages"
      mc:Ignorable="d"
      PreviewMouseRightButtonDown="GameList_OnPreviewMouseRightButtonDown">
    <Grid x:Name="MainGrid" AllowDrop="True" DragEnter="Main_OnDragEnter">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- ListPane (左) -->
        <Grid x:Name="ListPane" Grid.Column="0" Margin="0,0,1,0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <StackPanel Grid.Row="0" Orientation="Vertical" Margin="8,8,8,4">
                <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                    <ui:Button x:Name="AddButton" Click="AddButton_OnClick" ToolTip="{lex:Loc AddGame}">
                        <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe710;" FontSize="20"/>
                    </ui:Button>
                </StackPanel>
                <ComboBox x:Name="GenreComboBox" HorizontalContentAlignment="Center" Width="260" Margin="0,0,0,8" Loaded="GenreComboBox_OnLoaded" SelectionChanged="GenreComboBox_OnSelectionChanged">
                    <ComboBoxItem x:Name="GenreAllComboBoxItem" Content="{lex:Loc GenreAllComboBoxItem}"/>
                </ComboBox>
                <ui:TextBox x:Name="SearchBox" PlaceholderText="{lex:Loc SearchPlaceholder}" TextChanged="SearchBox_OnTextChanged" Margin="0,0,0,4"/>
            </StackPanel>
            <ListBox x:Name="GameListBox" Grid.Row="1" Background="Transparent" BorderThickness="0" ScrollViewer.HorizontalScrollBarVisibility="Disabled" ScrollViewer.VerticalScrollBarVisibility="Auto" SelectionChanged="GameListBox_OnSelectionChanged">
                <ListBox.ItemContainerStyle>
                    <Style TargetType="ListBoxItem">
                        <Setter Property="Tag" Value="{Binding Data}"/>
                    </Style>
                </ListBox.ItemContainerStyle>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Margin="4,2" Height="56">
                            <Border Width="32" Height="32" CornerRadius="4" ClipToBounds="True" VerticalAlignment="Center">
                                <Image Source="{Binding IconSource}" Stretch="UniformToFill"/>
                            </Border>
                            <StackPanel Orientation="Vertical" VerticalAlignment="Center" Margin="8,0,0,0">
                                <TextBlock Text="{Binding Name}" FontSize="14" TextTrimming="CharacterEllipsis" MaxWidth="200"/>
                                <TextBlock Text="{Binding FileExtension}" FontSize="11" Opacity="0.6" Margin="0,2,0,0"/>
                            </StackPanel>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Grid>

        <!-- DetailPane (右) -->
        <Grid x:Name="DetailPane" Grid.Column="1" Margin="8,8,8,8">
            <!-- 空状態 -->
            <StackPanel x:Name="EmptyState" VerticalAlignment="Center" HorizontalAlignment="Center">
                <ui:SymbolIcon Symbol="Home12" FontSize="48" Opacity="0.3"/>
                <TextBlock Text="{lex:Loc SelectGamePrompt}" HorizontalAlignment="Center" Margin="0,12,0,0" Opacity="0.6" FontSize="14"/>
            </StackPanel>

            <!-- 選択時 -->
            <ScrollViewer x:Name="DetailScrollViewer" VerticalScrollBarVisibility="Auto" Visibility="Collapsed">
                <StackPanel Orientation="Vertical" Margin="8">
                    <Border Width="128" Height="128" CornerRadius="8" ClipToBounds="True" HorizontalAlignment="Left" Margin="0,0,0,16">
                        <Image x:Name="HeroImage" Stretch="UniformToFill"/>
                    </Border>
                    <TextBlock x:Name="DetailTitleTextBlock" FontSize="24" FontWeight="SemiBold" TextWrapping="Wrap" Margin="0,0,0,8"/>
                    <Border x:Name="FileTypeBadge" Background="{DynamicResource ControlAccentColor}" CornerRadius="8" Padding="10,3" HorizontalAlignment="Left" Margin="0,0,0,12">
                        <TextBlock x:Name="FileTypeBadgeText" Foreground="White" FontSize="11"/>
                    </Border>
                    <ItemsControl x:Name="GenreChipsControl" Margin="0,0,0,16">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <WrapPanel/>
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Border Background="{DynamicResource ControlAccentColor}" CornerRadius="10" Padding="8,3" Margin="0,0,6,6">
                                    <TextBlock Text="{Binding}" Foreground="White" FontSize="11"/>
                                </Border>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <Expander x:Name="MemoExpander" Header="{lex:Loc DetailMemo}" Margin="0,0,0,12">
                        <TextBlock x:Name="MemoTextBlock" TextWrapping="Wrap" Margin="0,4,0,0"/>
                    </Expander>
                    <TextBlock Text="{lex:Loc WikiData}" FontWeight="SemiBold" Margin="0,0,0,4"/>
                    <ItemsControl x:Name="WikiDataControl" Margin="0,0,0,16">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Grid Margin="0,2">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="160"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>
                                    <TextBlock Grid.Column="0" Text="{Binding Key}" FontWeight="SemiBold" FontSize="12" Opacity="0.7"/>
                                    <TextBlock Grid.Column="1" Text="{Binding Value}" FontSize="12" TextWrapping="Wrap"/>
                                </Grid>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                    <TextBlock x:Name="LaunchOptionsTextBlock" Text="{lex:Loc DetailLaunchOptions}" FontWeight="SemiBold" Margin="0,0,0,4"/>
                    <StackPanel x:Name="LaunchOptionsPanel" Orientation="Vertical" Margin="0,0,0,16"/>
                    <WrapPanel Orientation="Horizontal">
                        <Button x:Name="PlayButton" Click="PlayButton_OnClick" Padding="20,8" Margin="0,0,8,8" Background="{DynamicResource ControlAccentColor}" Foreground="White">
                            <StackPanel Orientation="Horizontal">
                                <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe768;" FontSize="14" Margin="0,0,6,0"/>
                                <TextBlock Text="{lex:Loc DetailPlay}"/>
                            </StackPanel>
                        </Button>
                        <Button x:Name="PropertyButton" Click="PropertyButton_OnClick" Padding="14,8" Margin="0,0,8,8">
                            <StackPanel Orientation="Horizontal">
                                <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe713;" FontSize="14" Margin="0,0,6,0"/>
                                <TextBlock Text="{lex:Loc DetailProperty}"/>
                            </StackPanel>
                        </Button>
                        <Button x:Name="MemoButton" Click="MemoButton_OnClick" Padding="14,8" Margin="0,0,8,8">
                            <StackPanel Orientation="Horizontal">
                                <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe70b;" FontSize="14" Margin="0,0,6,0"/>
                                <TextBlock Text="{lex:Loc DetailMemo}"/>
                            </StackPanel>
                        </Button>
                        <Button x:Name="WikiManageButton" Click="WikiManageButton_OnClick" Padding="14,8" Margin="0,0,8,8">
                            <StackPanel Orientation="Horizontal">
                                <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe774;" FontSize="14" Margin="0,0,6,0"/>
                                <TextBlock Text="{lex:Loc DetailWikiManage}"/>
                            </StackPanel>
                        </Button>
                        <Button x:Name="GenreManageButton" Click="GenreManageButton_OnClick" Padding="14,8" Margin="0,0,8,8">
                            <StackPanel Orientation="Horizontal">
                                <ui:FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xe73e;" FontSize="14" Margin="0,0,6,0"/>
                                <TextBlock Text="{lex:Loc DetailGenreManage}"/>
                            </StackPanel>
                        </Button>
                    </WrapPanel>
                </StackPanel>
            </ScrollViewer>
        </Grid>
    </Grid>
</Page>
```

- `ListBox.ItemContainerStyle` で各 `ListBoxItem` の `Tag` に `ApplicationJsonData` をバインド（`{Binding Data}`）。これにより右クリック時に `ListBoxItem.Tag` から該当ゲームのデータを取得し、`PageControlCreate.GameListShowContextMenu(true, data)` を構築する。

- [ ] **Step 2: ビルド確認（XAMLのパースエラーが出ないこと）**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: code-behindのハンドラ未定義でエラー多数。これは後のTaskで解消。XAMLのパースエラー（MC3000等）が出ないことのみ確認。

- [ ] **Step 3: コミットは次Taskとまとめて行う**

---

### Task 5: GameList.xaml.cs のリファクタとハンドラ実装

**Files:**
- Modify: `YuLauncher/Core/Window/Pages/GameList.xaml.cs`（全体書き換え）

**Interfaces:**
- Consumes:
  - `GameButton.LaunchApplication(JsonControl.ApplicationJsonData)` — Task 2
  - `GameButton.GetImage(JsonControl.ApplicationJsonData)` — Task 2
  - `ObjectProperty.ListItemHeight`, `ListItemIconSize`, `HeroIconSize` — Task 3
  - リソースキー群 — Task 1
- Produces: 以下のイベントハンドラ（XAMLから参照）
  - `SearchBox_OnTextChanged`, `GameListBox_OnSelectionChanged`, `PlayButton_OnClick`, `PropertyButton_OnClick`, `MemoButton_OnClick`, `WikiManageButton_OnClick`, `GenreManageButton_OnClick`
  - 既存: `AddButton_OnClick`, `GenreComboBox_OnLoaded`, `GenreComboBox_OnSelectionChanged`, `GameList_OnPreviewMouseRightButtonDown`, `Main_OnDragEnter`, `GameList_OnFileUpdate`

- [ ] **Step 1: GameList.xaml.cs を全体書き換え**

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using Button = Wpf.Ui.Controls.Button;
using Image = System.Windows.Controls.Image;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace YuLauncher.Core.Window.Pages;

public class GameListItem
{
    public string Name { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public BitmapImage? IconSource { get; set; }
    public JsonControl.ApplicationJsonData Data { get; set; }
}

public partial class GameList : Page
{
    private readonly GameButton _gameButton = new GameButton();
    private readonly ObservableCollection<GameListItem> _gameItems = new();
    private List<GameListItem> _allGames = new();

    public GameList()
    {
        InitializeComponent();
        GameControl();
        LoggerController.LogInfo("GameList Page Loaded");
        GameListBox.ItemsSource = _gameItems;
        _ = LoadGenre();

        PageControlCreate.DeleteFileMenuClicked.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        CreateGameDialog.CloseObservable.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
        PropertyDialog.AllGameListPanelUpdate.Subscribe(n => PropertyDialogOnAllGamePanelUpdate(this, EventArgs.Empty, n));
        MainPage.SettingWindowClose.Subscribe(_ => PropertyDialogPanelUpdate(this, EventArgs.Empty));
    }

    private async ValueTask LoadGenre()
    {
        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        List<string> genreList = new();

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;

                var data = await JsonControl.ReadExeJson(file);

                data.Genre?.ToList().ForEach(x =>
                {
                    if (!genreList.Contains(x))
                    {
                        genreList.Add(x);
                    }
                });
            }

        // 不要なジャンル項目をComboBoxから削除
        GenreComboBox.Items.OfType<ComboBoxItem>()
            .Where(x => x != GenreAllComboBoxItem)
            .ToList()
            .ForEach(x => GenreComboBox.Items.Remove(x));

        genreList.ForEach(x =>
        {
            var genre = GenreComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == x);
            if (genre != null && (string)genre.Content == x) return;
            ComboBoxItem comboBoxItem = new()
            {
                Content = x
            };
            comboBoxItem.Selected += async (sender, args) =>
            {
                GenreComboBox.SelectedItem = comboBoxItem;
                await LoadGamesByGenre(x);
            };
            GenreComboBox.Items.Add(comboBoxItem);
        });
    }

    private async Task LoadAllGames()
    {
        _allGames.Clear();
        _gameItems.Clear();

        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;
                var data = await JsonControl.ReadExeJson(file);
                try
                {
                    if (data.FileExtension == "WebGame") continue;

                    var item = CreateGameListItem(data);
                    _allGames.Add(item);
                    _gameItems.Add(item);
                }
                catch (Exception ex)
                {
                    LoggerController.LogError($"{ex}");
                    LoggerController.LogError("An I/O error occurred: " + ex.Message);
                }
            }
    }

    private async Task LoadGamesByGenre(string genre)
    {
        _allGames.Clear();
        _gameItems.Clear();

        string[]? files = null;
        await Task.Run(() => files = Directory.GetFiles(FileControl.Main.Directory));

        if (files != null)
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != ".json") continue;
                var data = await JsonControl.ReadExeJson(file);
                try
                {
                    if (data.Genre != null && data.Genre.Contains(genre))
                    {
                        var item = CreateGameListItem(data);
                        _allGames.Add(item);
                        _gameItems.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    LoggerController.LogError($"{ex}");
                    LoggerController.LogError("An I/O error occurred: " + ex.Message);
                }
            }
    }

    private static GameListItem CreateGameListItem(JsonControl.ApplicationJsonData data)
    {
        return new GameListItem
        {
            Name = data.Name,
            FileExtension = data.FileExtension,
            IconSource = GameButton.GetImage(data),
            Data = data
        };
    }

    private async void PropertyDialogOnAllGamePanelUpdate(object? sender, EventArgs e, int n)
    {
        switch (n)
        {
            case 0:
            case 1:
                await LoadGenre();
                await LoadAllGames();
                break;
        }
    }

    private async void PropertyDialogPanelUpdate(object? sender, EventArgs e)
    {
        await LoadAllGames();
        Application.Current.MainWindow?.Activate();
    }

    public static void GameControl()
    {
        if (Directory.Exists(FileControl.Main.Directory)) return;
        Directory.CreateDirectory(FileControl.Main.Directory);
        LoggerController.LogInfo("Create Game Directory");
    }

    private void GameList_OnFileUpdate(object sender, EventArgs e)
    {
        // 互換性のため残すが、実際のリスト再構築は LoadAllGames/LoadGamesByGenre で行う
        LoggerController.LogInfo("GameList Page Reloaded");
    }

    private void GameList_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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

    private async void GenreComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Equals(GenreComboBox.SelectedItem, GenreAllComboBoxItem))
            {
                await LoadAllGames();
            }
        }
        catch (Exception exception)
        {
            LoggerController.LogError($"{exception}");
        }
    }

    private void GenreComboBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        GenreComboBox.SelectedItem = GenreAllComboBoxItem;
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        CreateGameDialog createGameDialog = new CreateGameDialog();
        createGameDialog.Show();
    }

    private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        _gameItems.Clear();
        if (string.IsNullOrEmpty(query))
        {
            foreach (var item in _allGames)
                _gameItems.Add(item);
        }
        else
        {
            foreach (var item in _allGames.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                _gameItems.Add(item);
        }
    }

    private void GameListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GameListBox.SelectedItem is not GameListItem item)
        {
            ShowEmptyState();
            return;
        }
        ShowDetail(item.Data);
    }

    private void ShowEmptyState()
    {
        EmptyState.Visibility = Visibility.Visible;
        DetailScrollViewer.Visibility = Visibility.Collapsed;
    }

    private void ShowDetail(JsonControl.ApplicationJsonData data)
    {
        EmptyState.Visibility = Visibility.Collapsed;
        DetailScrollViewer.Visibility = Visibility.Visible;

        HeroImage.Source = GameButton.GetImage(data);
        DetailTitleTextBlock.Text = data.Name;
        FileTypeBadgeText.Text = data.FileExtension ?? "unknown";

        // Genre chips
        GenreChipsControl.ItemsSource = data.Genre?.Length > 0 ? data.Genre : Array.Empty<string>();

        // Memo
        if (!string.IsNullOrEmpty(data.Memo))
        {
            MemoExpander.Visibility = Visibility.Visible;
            MemoTextBlock.Text = data.Memo;
        }
        else
        {
            MemoExpander.Visibility = Visibility.Collapsed;
        }

        // WikiData
        if (data.WikiData != null && data.WikiData.Count > 0)
        {
            WikiDataControl.Visibility = Visibility.Visible;
            WikiDataControl.ItemsSource = data.WikiData.ToList();
        }
        else
        {
            WikiDataControl.Visibility = Visibility.Collapsed;
        }

        // Launch options
        LaunchOptionsPanel.Children.Clear();

        AddLaunchOption("Log", data.IsUseLog == true ? "ON" : "OFF");
        if (data.FileExtension is "web" or "WebGame" or "WebSaver")
        {
            AddLaunchOption("WebView", data.IsWebView == true ? "ON" : "OFF");
            AddLaunchOption("Mute", data.IsMute ? "ON" : "OFF");
            if (data.Volume.HasValue)
            {
                AddLaunchOption("Volume", $"{data.Volume.Value:P0}");
            }
        }

        // Store current data on buttons via Tag
        PlayButton.Tag = data;
        PropertyButton.Tag = data;
        MemoButton.Tag = data;
        WikiManageButton.Tag = data;
        GenreManageButton.Tag = data;
    }

    private void AddLaunchOption(string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
        var labelTb = new System.Windows.Controls.TextBlock
        {
            Text = $"{label}: ",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Opacity = 0.7,
            Width = 100
        };
        var valueTb = new System.Windows.Controls.TextBlock { Text = value, FontSize = 12 };
        panel.Children.Add(labelTb);
        panel.Children.Add(valueTb);
        LaunchOptionsPanel.Children.Add(panel);
    }

    private async void PlayButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayButton.Tag is not JsonControl.ApplicationJsonData data) return;
        try
        {
            await GameButton.LaunchApplication(data);
            if (data.MultipleLaunch is { Length: > 0 })
            {
                foreach (var multipleLaunch in data.MultipleLaunch)
                {
                    if (string.IsNullOrEmpty(multipleLaunch)) continue;
                    var multipleData = await JsonControl.ReadExeJson($"./Games/{multipleLaunch}.json");
                    await GameButton.LaunchApplication(multipleData);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void PropertyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PropertyButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            PropertyDialog propertyDialog = new PropertyDialog(data);
            propertyDialog.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void MemoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (MemoButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            MemoWindow memoWindow = new MemoWindow(data);
            memoWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void WikiManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (WikiManageButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            WikiDataManageWindow wikiWindow = new WikiDataManageWindow(data);
            wikiWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void GenreManageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (GenreManageButton.Tag is not JsonControl.ApplicationJsonData data) return;
        if (!File.Exists(data.JsonPath)) return;
        try
        {
            GenreManageWindow genreWindow = new GenreManageWindow(data);
            genreWindow.Show();
        }
        catch (Exception ex)
        {
            LoggerController.LogError(ex.Message);
        }
    }

    private void Main_OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
}
```

- [ ] **Step 2: ビルド確認**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: コミット**

```bash
git add YuLauncher/Core/Window/Pages/GameList.xaml YuLauncher/Core/Window/Pages/GameList.xaml.cs
git commit -m "feat(gamelist): redesign GameList page with 2-pane list+detail layout"
```

---

### Task 6: 動作検証

**Files:**
- なし（実行検証のみ）

- [ ] **Step 1: ビルド確認**

Run: `dotnet build YuLauncher --nologo -v q`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: アプリ起動確認**

Run: `dotnet run --project YuLauncher`
Expected: アプリが起動し、MainPageが表示される。GameListページに遷移すると2ペインレイアウトが表示される。

手動確認項目:
- [ ] GameListページが2ペイン（左リスト+右詳細）で表示される
- [ ] 左リストにゲームが表示される（既存JSON読み込み）
- [ ] リスト項目クリックで右詳細ペインが更新される
- [ ] Hero画像、タイトル、種別バッジ、Genre chipsが表示される
- [ ] Memo/WikiDataがある場合Expander/ItemsControlが表示される
- [ ] 実行情報（Log/WebView/Mute/Volume）が表示される
- [ ] 各アクションボタン（再生/プロパティ/メモ/Wiki管理/ジャンル管理）が機能する
- [ ] 検索ボックスでタイトル絞り込みが動作する
- [ ] GenreComboBoxで「ALL」選択時に全ゲーム表示、各ジャンル選択でフィルタ
- [ ] 右クリックContextMenuが既存通り動作する
- [ ] WebGameList/WebSaverListページが変更なく動作する

- [ ] **Step 3: 最終コミット（修正があれば）**

```bash
git add -A
git commit -m "fix(gamelist): address manual testing feedback" --allow-empty
```

---

## Self-Review

### 1. Spec coverage
- 2ペインGrid構造 → Task 4 ✓
- ListPane（AddButton + GenreComboBox + SearchBox + ListBox） → Task 4, 5 ✓
- DetailPane空状態 → Task 4, 5（ShowEmptyState） ✓
- DetailPane選択時（Hero, タイトル, 種別バッジ, Genre chips, Memo, WikiData, 実行情報, Action buttons） → Task 4, 5 ✓
- アクションボタン5つ（再生/プロパティ/メモ/Wiki管理/ジャンル管理） → Task 5 ✓
- MultipleLaunch対応再生 → Task 5（PlayButton_OnClick） ✓
- 検索ボックス → Task 5（SearchBox_OnTextChanged） ✓
- GenreComboBox既存ロジック維持 → Task 5 ✓
- GameButton.LaunchApplication/GetImage internal化 → Task 2 ✓
- ローカライズリソース → Task 1 ✓
- ビルド検証 → Task 6 ✓

### 2. Placeholder scan
- "TBD"/"TODO"/"implement later" なし
- すべてのコードブロックに実際のコードあり
- コマンドと期待結果明記済み

### 3. Type consistency
- `GameListItem` クラス: `Name`, `FileExtension`, `IconSource`, `Data` — Task 5で定義・使用 ✓
- `GameButton.LaunchApplication(JsonControl.ApplicationJsonData)` — Task 2でinternal化、Task 5で呼び出し ✓
- `GameButton.GetImage(JsonControl.ApplicationJsonData)` — Task 2でinternal化、Task 5で呼び出し ✓
- `ObjectProperty.ListItemHeight` / `ListItemIconSize` / `HeroIconSize` — Task 3で定義。Task 4のXAMLでは固定値（56/32/128）を直接使用しているため、定数参照なし。整合性は取れているがDRYでない → 許容（XAMLから定数参照は `x:Static` で可能だが複雑化するため固定値を使用）

### 既知のリスク
- 既存の `GenreExeUpdate` / `GenreWebUpdate` メソッドは新しい設計では不要（FileExtensionによるフィルタはGenreComboBoxではなく、リスト表示時に判定）。本計画では削除。ただし `GenreComboBox_OnSelectionChanged` で「ALL」以外のジャンル選択時の挙動は `LoadGamesByGenre` に統合。
- `Initialize()` メソッドは `LoadAllGames` に統合され削除。
- `Viewer` / `Panel` / `MainPanel` x:Name参照は削除されるため、code-behindでこれらを参照している箇所があればビルドエラーになる。Task 5の全体書き換えで対応済み。
