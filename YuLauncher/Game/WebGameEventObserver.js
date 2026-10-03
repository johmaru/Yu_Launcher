(function(options) {
    if (!/^https?:$/.test(location.protocol)) return;
    window.__yuLauncherEventObserverDispose?.();
    if (!options.enabled) return;
    const original = EventTarget.prototype.dispatchEvent;
    const active = new Set();
    const nativeDetail = Object.getOwnPropertyDescriptor(CustomEvent.prototype, 'detail').get;
    const encoder = new TextEncoder();
    const sensitive = /password|passwd|secret|token|cookie|authorization|session|email|user|birthday/i;
    const types = ['click','dblclick','change','submit','DOMContentLoaded','load','pageshow'];
    function copy(value, depth, seen) {
        if (value === null || typeof value === 'string' || typeof value === 'boolean') return value;
        if (typeof value === 'number') return Number.isFinite(value) ? value : '[unavailable]';
        if (typeof value !== 'object') return '[unavailable]';
        if (depth >= 4) return '[truncated]';
        if (seen.has(value)) return '[unavailable]';
        seen.add(value);
        try {
            const array = Array.isArray(value), result = array ? [] : Object.create(null);
            const descriptors = Object.getOwnPropertyDescriptors(value);
            const keys = array ? Array.from({length:Math.min(descriptors.length.value,20)},(_,i)=>String(i)) :
                Object.keys(descriptors).filter(k => descriptors[k].enumerable);
            if (array) Object.setPrototypeOf(result, null);
            for (const key of keys.slice(0,20)) {
                const d = descriptors[key];
                result[key] = sensitive.test(key) ? '[redacted]' : !d || !('value' in d) ? '[unavailable]' : copy(d.value, depth+1, seen);
            }
            if(array ? descriptors.length.value > 20 : keys.length > 20) {
                if(array) result[result.length]='[truncated]'; else result['[truncated]']='[truncated]';
            }
            return result;
        } finally { seen.delete(value); }
    }
    function targetInfo(target) {
        if(target === window) return {targetKind:'window',selector:null};
        if(target === document) return {targetKind:'document',selector:null};
        if(!(target instanceof Element) || target.getRootNode() !== document) return {targetKind:'unsupported',selector:null};
        let selector;
        if(target.id) {
            const id='#'+CSS.escape(target.id);
            if(document.querySelectorAll(id).length === 1) selector=id;
        }
        if(!selector) {
            const parts=[];
            for(let node=target;node;node=node.parentElement) {
                let index=1;
                for(let sibling=node.previousElementSibling;sibling;sibling=sibling.previousElementSibling) if(sibling.localName===node.localName) index++;
                parts.unshift(CSS.escape(node.localName)+':nth-of-type('+index+')');
            }
            selector=parts.join(' > ');
        }
        if(selector.length > 4096) return {targetKind:'unsupported',selector:null};
        const matches=document.querySelectorAll(selector);
        return matches.length===1 && matches[0]===target ? {targetKind:'element',selector} : {targetKind:'unsupported',selector:null};
    }
    function observe(event,target,listenerKind) {
        try {
            if(typeof event.type !== 'string' || event.type.length===0 || event.type.length>256) return;
            const info=targetInfo(target);
            const message={channel:'yuLauncher.events',token:options.token,type:'dom',eventName:event.type,...info,listenerKind:listenerKind ?? info.targetKind,detailJson:null,detailState:'notCaptured'};
            if(options.captureDetail) {
                message.detailState='unavailable';
                if(event instanceof CustomEvent) {
                    try {
                        const json=JSON.stringify(copy(nativeDetail.call(event),0,new Set()));
                        if(encoder.encode(json).length<=2048) {message.detailJson=json;message.detailState='available';}
                    } catch { }
                }
            }
            if(encoder.encode(JSON.stringify(message)).length<=8192) chrome.webview.postMessage(message);
        } catch { }
    }
    function wrapper(event) {
        const alreadyActive = active.has(event);
        active.add(event);
        let result;
        try { result=Reflect.apply(original,this,arguments); }
        finally { if (!alreadyActive) active.delete(event); }
        observe(event,this);
        return result;
    }
    const capture=e=>{if(!active.has(e)) observe(e,e.target,'window');};
    EventTarget.prototype.dispatchEvent=wrapper;
    for(const type of types) window.addEventListener(type,capture,true);
    window.__yuLauncherEventObserverDispose=()=>{
        if(EventTarget.prototype.dispatchEvent===wrapper) EventTarget.prototype.dispatchEvent=original;
        for(const type of types) window.removeEventListener(type,capture,true);
        active.clear();
    };
})
