(function (cfg) {
    "use strict";
    if (location.origin !== cfg.targetOrigin) return;
    const key = "__yuLauncherLoginDispose";
    window[key]?.();
    let disposed = false, completed = false;
    const cleanups = new Set();
    const notify = (type, extra = {}) => {
        if (!disposed) chrome.webview.postMessage({ channel: "yuLauncher.login", token: cfg.token, type, ...extra });
    };
    const reportError = error => notify("error", { message: String(error?.message ?? error).slice(0, 1024) });
    const dispose = () => {
        disposed = true;
        for (const cleanup of [...cleanups]) cleanup();
        cleanups.clear();
    };
    window[key] = dispose;
    const watch = cleanup => {
        let active = true;
        const stop = () => { if (active) { active = false; cleanup(); cleanups.delete(stop); } };
        cleanups.add(stop);
        if (disposed || completed) stop();
        return stop;
    };
    const complete = () => {
        if (disposed || completed) return;
        completed = true;
        notify("complete");
        for (const cleanup of [...cleanups]) cleanup();
    };
    const onEvent = (target, eventName, predicate) => {
        try {
            const selector = typeof target === "string" ? target : null;
            if (selector !== null) document.querySelector(selector);
            const owner = selector !== null ? document : target;
            if (!(owner instanceof EventTarget)) throw new Error("target must be an EventTarget or CSS selector");
            const listener = event => {
                try {
                    if (selector !== null && !(event.target instanceof Element && event.target.closest(selector))) return;
                    if (predicate === undefined || predicate(event) === true) complete();
                } catch (error) { reportError(error); }
            };
            owner.addEventListener(eventName, listener, true);
            return watch(() => owner.removeEventListener(eventName, listener, true));
        } catch (error) { reportError(error); return () => {}; }
    };
    const whenVisible = selector => {
        let observer, raf = 0;
        try {
            document.querySelector(selector);
            const check = () => {
                raf = 0;
                if (disposed || completed) return;
                const element = document.querySelector(selector);
                if (!element) return;
                const rect = element.getBoundingClientRect();
                if (rect.width <= 0 || rect.height <= 0) return;
                for (let node = element; node instanceof Element; node = node.parentElement) {
                    const style = getComputedStyle(node);
                    if (style.display === "none" || style.visibility === "hidden" || style.visibility === "collapse" || Number(style.opacity) === 0) return;
                }
                complete();
            };
            const schedule = () => { if (!raf && !disposed && !completed) raf = requestAnimationFrame(check); };
            observer = new MutationObserver(schedule);
            observer.observe(document, { subtree: true, childList: true, attributes: true });
            document.addEventListener("DOMContentLoaded", schedule);
            document.addEventListener("transitionend", schedule, true);
            document.addEventListener("animationend", schedule, true);
            window.addEventListener("resize", schedule);
            const stop = watch(() => {
                observer.disconnect(); cancelAnimationFrame(raf);
                document.removeEventListener("DOMContentLoaded", schedule);
                document.removeEventListener("transitionend", schedule, true);
                document.removeEventListener("animationend", schedule, true);
                window.removeEventListener("resize", schedule);
            });
            check();
            return stop;
        } catch (error) { observer?.disconnect(); reportError(error); return () => {}; }
    };
    const whenUrl = url => {
        try {
            const expected = new URL(url);
            if (!/^https?:$/.test(expected.protocol)) throw new Error("HTTP(S) URL required");
            const check = () => { if (location.origin === expected.origin && location.pathname.startsWith(expected.pathname)) complete(); };
            const originals = {}, wrappers = {};
            for (const name of ["pushState", "replaceState"]) {
                originals[name] = history[name];
                wrappers[name] = function (...args) { const result = originals[name].apply(this, args); check(); return result; };
                history[name] = wrappers[name];
            }
            window.addEventListener("popstate", check);
            window.addEventListener("hashchange", check);
            const stop = watch(() => {
                window.removeEventListener("popstate", check); window.removeEventListener("hashchange", check);
                for (const name of Object.keys(originals)) if (history[name] === wrappers[name]) history[name] = originals[name];
            });
            check();
            return stop;
        } catch (error) { reportError(error); return () => {}; }
    };
    const api = Object.freeze({ complete, onEvent, whenVisible, whenUrl });
    window.yuLogin = api;
    try {
        if (cfg.mode === "Url") whenUrl(cfg.successUrl);
        else if (cfg.mode === "Element") whenVisible(cfg.successSelector);
        else if (cfg.mode === "JavaScript") {
            const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
            new AsyncFunction("yuLogin", cfg.javascript)(api).catch(reportError);
        }
    } catch (error) { reportError(error); }
})
