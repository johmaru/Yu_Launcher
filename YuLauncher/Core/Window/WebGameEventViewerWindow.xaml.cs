using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Controls;
using YuLauncher.Core.lib;
using YuLauncher.Game;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using DataGrid = System.Windows.Controls.DataGrid;
using TreeViewItem = System.Windows.Controls.TreeViewItem;

namespace YuLauncher.Core.Window;

internal sealed class WebGameObservedEvent(string source, WebGameNetworkEvent? network, WebGameDomEvent? dom)
{
    public string Source { get; } = source;
    public WebGameNetworkEvent? Network { get; } = network;
    public WebGameDomEvent? Dom { get; } = dom;
    public string Time => (Network?.ObservedAtUtc ?? Dom!.ObservedAtUtc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string SourceLabel => Source;
    public string Url => Network?.Url ?? "";
    public string Method => Network?.Method ?? "";
    public int? Status => Network?.Status;
    public string ContentType => Network?.ContentType ?? "";
    public string Origin => Network?.DocumentOrigin ?? Dom!.SourceOrigin;
    public string EventName => Dom?.EventName ?? "";
    public string Target => Dom?.Selector ?? Dom?.TargetKind ?? "";
    internal void Release() { if (Network?.Response is { } response) { WebGameNetworkMonitor.ReleaseResponse(response); Network.Response = null; } }
}

public partial class WebGameEventViewerWindow : FluentWindow
{
    private readonly ObservableCollection<WebGameObservedEvent> _rows;
    private readonly ListCollectionView _networkView, _domView;
    private readonly Action<WebGameNetworkRule,string> _applyNetwork;
    private readonly Action<WebGameDomHook> _applyDom;
    private readonly Func<bool,bool,Task> _options;
    private readonly Func<WebGameObservedEvent,CancellationToken,Task<(WebGameObservedEvent Event,JsonDocument Json)>> _captureNext;
    private CancellationTokenSource? _captureCancellation;
    private JsonDocument? _json;
    private int _selectionVersion, _nodeCount;
    private bool _paused, _closed, _reading;
    private WebGameObservedEvent? Selected => (EventViewerTabs.SelectedIndex == 0 ? NetworkEventsGrid.SelectedItem : DomEventsGrid.SelectedItem) as WebGameObservedEvent;
    private static string L(string key) => LocalizeControl.GetLocalize<string>(key);

    internal WebGameEventViewerWindow(ObservableCollection<WebGameObservedEvent> rows, Action<WebGameNetworkRule,string> network,
        Action<WebGameDomHook> dom, Func<bool,bool,Task> options,
        Func<WebGameObservedEvent,CancellationToken,Task<(WebGameObservedEvent Event,JsonDocument Json)>> captureNext)
    {
        _rows=rows; _applyNetwork=network; _applyDom=dom; _options=options; _captureNext=captureNext;
        InitializeComponent();
        _networkView=new(rows); _domView=new(rows);
        _networkView.Filter=r=>Filter((WebGameObservedEvent)r,true); _domView.Filter=r=>Filter((WebGameObservedEvent)r,false);
        NetworkEventsGrid.ItemsSource=_networkView; DomEventsGrid.ItemsSource=_domView;
        foreach(var (property,key,width) in new[]{("Time","WebGameEventTime",175d),("SourceLabel","WebGameEventSource",120d),("Url","WebGameEventUrl",300d),("Method","WebGameEventMethod",90d),("Status","WebGameEventStatus",90d),("ContentType","WebGameEventContentType",150d)}) AddColumn(NetworkEventsGrid,property,key,width);
        foreach(var (property,key,width) in new[]{("Time","WebGameEventTime",175d),("SourceLabel","WebGameEventSource",120d),("Origin","WebGameLoginTargetOrigin",180d),("EventName","WebGameEventName",180d),("Target","WebGameEventTarget",250d)}) AddColumn(DomEventsGrid,property,key,width);
        _rows.CollectionChanged+=RowsChanged; RefreshCount(); UpdateSelection();
        foreach(var name in new[]{"EventViewerTabs","NetworkEventsGrid","DomEventsGrid","EventFilterBox","PauseEventObservationButton","ClearObservedEventsButton","CaptureEventDetailCheckBox","ReadEventJsonButton","EventJsonTree","ApplyObservedEventButton","EventViewerErrorBar","EventRuleUrlBox","EventRuleMethodBox","EventRuleStatusBox","EventRuleUseJsonCheckBox","EventRuleJsonPointerBox","EventRuleExpectedJsonBox"})
            if(FindName(name) is DependencyObject element) System.Windows.Automation.AutomationProperties.SetAutomationId(element,name);
    }
    private static void AddColumn(DataGrid grid,string property,string key,double width)
    {
        var factory=new FrameworkElementFactory(typeof(TextBlock));
        factory.SetValue(TextBlock.AppearanceProperty,TextColor.Primary);
        factory.SetValue(TextBlock.TextTrimmingProperty,TextTrimming.CharacterEllipsis);
        factory.SetBinding(TextBlock.TextProperty,new Binding(property)); factory.SetBinding(TextBlock.ToolTipProperty,new Binding(property));
        grid.Columns.Add(new DataGridTemplateColumn{Header=new TextBlock{Text=L(key),Appearance=TextColor.Primary},Width=width,CellTemplate=new DataTemplate{VisualTree=factory}});
    }
    private bool Filter(WebGameObservedEvent row,bool network)
    {
        if((row.Network is not null)!=network) return false;
        var text=EventFilterBox.Text;
        return text.Length==0 || row.Source.Contains(text,StringComparison.OrdinalIgnoreCase) ||
            (network ? row.Url.Contains(text,StringComparison.OrdinalIgnoreCase) || row.Method.Contains(text,StringComparison.OrdinalIgnoreCase) :
                row.EventName.Contains(text,StringComparison.OrdinalIgnoreCase) || row.Target.Contains(text,StringComparison.OrdinalIgnoreCase));
    }
    private void FilterChanged(object sender,TextChangedEventArgs e) { _networkView?.Refresh(); _domView?.Refresh(); RefreshCount(); }
    private void RefreshCount()
    {
        if(CountText is null || EmptyText is null) return;
        CountText.Text=string.Format(L("WebGameEventCount"),_rows.Count);
        EmptyText.Visibility=(EventViewerTabs.SelectedIndex==0 ? _networkView?.Count : _domView?.Count)==0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void RowsChanged(object? sender,NotifyCollectionChangedEventArgs e)
    { RefreshCount(); if(Selected is null || !_rows.Contains(Selected)) UpdateSelection(); }
    private void CancelCapture()
    {
        if(_captureCancellation is not { } cancellation) return;
        _captureCancellation=null; cancellation.Cancel(); cancellation.Dispose(); _reading=false;
        DetailStateText.Text="";
        ReadEventJsonButton.IsEnabled=!_paused && Selected?.Network?.Response is not null;
        ApplyObservedEventButton.IsEnabled=Selected?.Network is not null;
    }
    private void SelectionChanged(object sender,SelectionChangedEventArgs e) { if(EventJsonTree is not null) UpdateSelection(); }
    private void DisposeJson() { _selectionVersion++; _json?.Dispose(); _json=null; EventJsonTree.Items.Clear(); _nodeCount=0; }
    internal void SourceReleased(string source)
    {
        if (Selected?.Source != source) return;
        CancelCapture(); ReadEventJsonButton.IsEnabled=false;
        DisposeJson();
        DetailStateText.Text=L("WebGameEventExpired");
    }
    private void UpdateSelection()
    {
        CancelCapture();
        DisposeJson(); _reading=false; EventViewerErrorBar.IsOpen=false; DetailStateText.Text="";
        RefreshCount();
        EventRuleUseJsonCheckBox.IsChecked=false; EventRuleJsonPointerBox.Text=EventRuleExpectedJsonBox.Text="";
        var row=Selected; var network=EventViewerTabs.SelectedIndex==0;
        NetworkRulePanel.Visibility=network ? Visibility.Visible : Visibility.Collapsed;
        DomTargetText.Visibility=ExistingScriptNoticeText.Visibility=network ? Visibility.Collapsed : Visibility.Visible;
        ModeNoticeText.Text=L(network ? "WebGameEventNetworkNotice" : "WebGameEventDomNotice");
        ApplyObservedEventButton.IsEnabled=row is not null && (network || row.Dom!.TargetKind!="unsupported");
        ReadEventJsonButton.IsEnabled=!_paused && row?.Network?.Response is not null;
        if(row?.Network is { } n) { EventRuleUrlBox.Text=n.Url;EventRuleMethodBox.Text=n.Method;EventRuleStatusBox.Text=n.Status.ToString(CultureInfo.InvariantCulture); }
        if(row?.Dom is { } d)
        {
            DomTargetText.Text=d.EventName+"\n"+row.Target;
            if(d.TargetKind=="unsupported") DetailStateText.Text=L("WebGameEventUnsupportedTarget");
            else if(d.DetailState=="available" && d.DetailJson is not null) { _json=JsonDocument.Parse(d.DetailJson); ShowJson(); }
            else DetailStateText.Text=L(d.DetailState=="notCaptured" ? "WebGameEventDetailMissing" : "WebGameEventDetailUnavailable");
        }
    }
    private void Error(string key) { EventViewerErrorBar.Message=L(key); EventViewerErrorBar.IsOpen=true; }
    private async void ReadJson(object sender,RoutedEventArgs e)
    {
        if(_paused || _reading || Selected is not { Network.Response: not null } selected) return;
        DisposeJson();
        var version=_selectionVersion;
        var cancellation=new CancellationTokenSource(); var token=cancellation.Token;
        _captureCancellation=cancellation; _reading=true; ReadEventJsonButton.IsEnabled=false; ApplyObservedEventButton.IsEnabled=false;
        EventViewerErrorBar.IsOpen=false; DetailStateText.Text=L("WebGameEventWaitResponse");
        try
        {
            var (row,json)=await _captureNext(selected,token);
            if(_closed || version!=_selectionVersion || token.IsCancellationRequested) { json.Dispose();return; }
            if(!_networkView.Contains(row)) EventFilterBox.Text="";
            NetworkEventsGrid.SelectedItem=row;
            NetworkEventsGrid.ScrollIntoView(row);
            _json=json; ShowJson(); DetailStateText.Text="";
        }
        catch(OperationCanceledException) { }
        catch(WebGameResponseException ex) { if(!_closed && version==_selectionVersion) Error(ex.ResourceKey); }
        catch(Exception) { if(!_closed && version==_selectionVersion) Error("WebGameEventReadFailed"); }
        finally { if(ReferenceEquals(_captureCancellation,cancellation)) CancelCapture(); }
    }
    private sealed record JsonNode(string Pointer,JsonElement Value);
    private TreeViewItem Node(string label,string pointer,JsonElement value,bool sensitive=false)
    {
        _nodeCount++;
        var branch=value.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        var blocked=sensitive || WebGameEventJson.IsReserved(value);
        var text=label+": "+(sensitive ? "[redacted]" : branch ? value.ValueKind.ToString() : value.GetRawText());
        var item=new TreeViewItem{Header=new TextBlock{Text=text,Appearance=TextColor.Primary},Tag=blocked || branch ? null : new JsonNode(pointer,value),IsEnabled=!blocked};
        if(branch && !blocked)
        {
            item.Items.Add(new TreeViewItem());
            item.Expanded+=(_,e)=>{
                if(e.OriginalSource!=item || item.Items.Count!=1 || ((TreeViewItem)item.Items[0]).Header is not null) return;
                int count=0;bool truncated=false;
                bool Add(string key,JsonElement child)
                {
                    if(count>=200 || _nodeCount>=999) { truncated=true;return false; }
                    count++;
                    item.Items.Add(Node(key,pointer+"/"+key.Replace("~","~0").Replace("/","~1"),child,WebGameEventJson.IsSensitiveKey(key)));
                    return true;
                }
                if(value.ValueKind==JsonValueKind.Object)
                {
                    foreach(var p in value.EnumerateObject()) if(!Add(p.Name,p.Value)) break;
                }
                else
                {
                    int i=0;foreach(var child in value.EnumerateArray()) if(!Add((i++).ToString(CultureInfo.InvariantCulture),child)) break;
                }
                if(truncated)
                {
                    if(_nodeCount<1000)
                    {
                        _nodeCount++;
                        item.Items.Add(new TreeViewItem{Header=new TextBlock{Text="[truncated]",Appearance=TextColor.Secondary},IsEnabled=false});
                    }
                    else ((TextBlock)item.Header).Text+=" [truncated]";
                }
                item.Items.RemoveAt(0);
                // 親の測定キャッシュが展開前の高さを保持すると、子要素が切られる。
                if(System.Windows.Media.VisualTreeHelper.GetParent(item) is UIElement parent) parent.InvalidateMeasure();
            };
        }
        return item;
    }
    private void ShowJson() { EventJsonTree.Items.Clear();_nodeCount=0;EventJsonTree.Items.Add(Node("$","",_json!.RootElement)); }
    private void JsonSelected(object sender,RoutedPropertyChangedEventArgs<object> e)
    { if(e.NewValue is TreeViewItem{Tag:JsonNode n}) { EventRuleUseJsonCheckBox.IsChecked=true;EventRuleJsonPointerBox.Text=n.Pointer;EventRuleExpectedJsonBox.Text=n.Value.GetRawText(); } }
    private async Task SetOptions()
    {
        PauseEventObservationButton.IsEnabled=CaptureEventDetailCheckBox.IsEnabled=false;
        try { await _options(!_paused,CaptureEventDetailCheckBox.IsChecked==true); }
        catch(Exception) { Error("WebGameEventRuntimeUnsupported"); }
        finally { if(!_closed) PauseEventObservationButton.IsEnabled=CaptureEventDetailCheckBox.IsEnabled=true; }
        PauseEventObservationButton.Content=L(_paused ? "WebGameEventResume" : "WebGameEventPause");
    }
    private async void Pause(object sender,RoutedEventArgs e) { _paused=!_paused; if(_paused) CancelCapture(); await SetOptions();ReadEventJsonButton.IsEnabled=!_paused && Selected?.Network?.Response is not null; }
    private async void DetailChanged(object sender,RoutedEventArgs e) { if(_options is not null && IsLoaded) await SetOptions(); }
    private void Clear(object sender,RoutedEventArgs e) { foreach(var row in _rows) row.Release();_rows.Clear();UpdateSelection(); }
    private void Apply(object sender,RoutedEventArgs e)
    {
        if(Selected is not { } row) return;
        try
        {
            var pointer=EventRuleUseJsonCheckBox.IsChecked==true ? EventRuleJsonPointerBox.Text : null;
            var expected=EventRuleUseJsonCheckBox.IsChecked==true ? EventRuleExpectedJsonBox.Text : null;
            if(row.Network is not null)
            {
                if(!int.TryParse(EventRuleStatusBox.Text,NumberStyles.None,CultureInfo.InvariantCulture,out var status)) throw new ArgumentException();
                var rule=WebGameNetworkConditions.Normalize(new(EventRuleUrlBox.Text,EventRuleMethodBox.Text,status,pointer,expected));
                _applyNetwork(rule,row.Origin);
            }
            else
            {
                var d=row.Dom!;
                var hook=new WebGameDomHook(d.SourceOrigin,d.EventName,d.TargetKind,d.ListenerKind,d.Selector,pointer,expected);
                BuildDomScript(hook);_applyDom(hook);
            }
            EventViewerErrorBar.IsOpen=false;
        }
        catch(Exception) { Error("WebGameEventInvalidRule"); }
    }
    internal static string BuildDomScript(WebGameDomHook hook)
    {
        WebGameLoginRepository.GetOrigin(hook.SourceOrigin);
        if(hook.EventName.Length is 0 or >256) throw new ArgumentException();
        var target=hook.TargetKind switch { "window"=>"window", "document"=>"document", "element" when !string.IsNullOrWhiteSpace(hook.Selector) && hook.Selector.Length<=4096=>JsonSerializer.Serialize(hook.Selector), _=>throw new ArgumentException() };
        // 捕捉した監視先とevent.targetは一致しないことがある（ページ全体のload/pageshow等）。
        var listener=hook.ListenerKind switch { "window"=>"window", "document"=>"document", "element" when hook.TargetKind=="element"=>target, _=>throw new ArgumentException() };
        var predicate=hook.TargetKind=="element" ? "e.target instanceof Element && e.target.matches("+target+")" : "e.target === "+target;
        if(hook.JsonPointer is not null || hook.ExpectedJson is not null)
        {
            WebGameNetworkConditions.Normalize(new("https://example.test/","GET",200,hook.JsonPointer,hook.ExpectedJson));
            using var expected=JsonDocument.Parse(hook.ExpectedJson!);
            if(WebGameEventJson.IsReserved(expected.RootElement)) throw new ArgumentException();
            var segments=WebGameEventJson.ParsePointer(hook.JsonPointer!);
            if(segments.Any(WebGameEventJson.IsSensitiveKey)) throw new ArgumentException();
            var access="e.detail"+string.Concat(segments.Select(s=>"?.["+JsonSerializer.Serialize(s)+"]"));
            predicate+=" && "+access+" === "+JsonSerializer.Serialize(expected.RootElement);
        }
        return "yuLogin.onEvent("+listener+", "+JsonSerializer.Serialize(hook.EventName)+", e => "+predicate+");";
    }
    private void OnClosed(object? sender,EventArgs e) { _closed=true;CancelCapture();DisposeJson();_rows.CollectionChanged-=RowsChanged;foreach(var row in _rows) row.Release(); }
}
