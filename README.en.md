# YuLauncher

[日本語](README.md) | [English](README.en.md)

A Windows launcher for managing PC games, web games, and websites in one place.

## Features
- **PC games and apps**: Register and launch apps, view their icons, and identify missing files.
- **Web games and websites**: Open them in the default or built-in browser, and play web games in a dedicated window.
- **Saved web pages**: Save pages and view them locally.
- **Web-game login history**: View today's recording status and per-game timestamps and detection methods.
- **Library organization**: Search entries, organize them by genre, and manage per-entry notes and wiki data.
- **Display customization**: Switch between Japanese and English, and configure window sizes, fullscreen, note text size, and divider color.

## Usage
1. Select “Add” in the list, then choose an entry type from the dialog tabs.
2. Set the name and file or URL in the input card, then select “Create” at the bottom right.
3. Select an item in the list and select “Launch”.

Combine name search with a genre filter. Press `Ctrl+F` to focus search, or `Esc` in the search box to clear the query. Select an item in the list and press `Enter`, or double-click it to launch. Launch and Property stay visible while scrolling the details.

Search, genre, and selection are preserved after editing. Selection is cleared if the edited item no longer matches the filters. Use “Clear filters” when no items match, or “Reload” if loading fails.

Properties are grouped into Basic information, Launch settings, and Related settings. Expand MultipleLaunch to select other entries. Save changes stays visible while scrolling the form.

### Web-game login history
The web-game list shows today's recording status. Expand “Login history” in the selected game's details to browse records in pages of 50. “No record” does not mean authentication failed or that you are logged out.

Open “Login history settings” from the game's properties or its window's “Application” menu.
- **Connection (estimated)**: Records a successful connection to the target site, not verified authentication.
- **Success URL**: Matches the exact origin and path prefix, ignoring query strings and fragments.
- **Visible element**: Pick an indicator shown after login in the preview, including inside iframes. Individual canvas contents and Shadow DOM elements cannot be picked.
- **JavaScript event**: Run only code you trust. Samples use `yuLogin.complete()`, `onEvent()`, `whenVisible()` and `whenUrl()`. Clicks and submits do not verify authentication.
- **Network condition**: Matches the URL, HTTP method, status and optionally a JSON value in responses received by the whole preview WebView. HTTP success alone does not verify authentication.

“Test detection” in the preview does not write history. Settings apply on the next independent game launch. Reloads and child windows share a launch and record at most once. Detection errors never automatically switch to connection mode.

Open “Observe requests and events” to view live traffic and DOM events in a separate window. Select an entry and choose “Apply to detection rule”. For JSON, select a request, choose “Capture next JSON response”, and repeat the action in the preview. Only the next response in the same window with the same URL, method and status is captured; the new entry is selected and its body displayed. No request is sent automatically. Bodies are limited to 1 MiB, and selecting a displayed value adds a JSON condition. DOM details are captured only for future events after you enable capture. Bodies may contain personal information, and redaction cannot guarantee complete removal. Applying a rule does not save it: test in the preview, then save. DOM rules append to existing JavaScript, so existing conditions can still detect success. HAR import, binary decoding and private game emitters are not supported.

For page-lifecycle DOM events such as `pageshow` and `load`, reload the preview after “Test detection”. Past events are not replayed. These events also fire before login, so they record page display rather than verified authentication. Previously generated and saved scripts are not updated automatically; apply the event again from the corrected viewer.

## Settings
- **General**: Divider color, data transfer, and app information. Select “Apply” to change the color immediately.
- **Display & language**: Language, window sizes, and memo text size. Select “Save changes” to save them. Window sizes apply the next time you open a window.
- The fullscreen setting is saved when you change it.

Import overwrites the current data and settings.

## Planned features
- Frequently used software
