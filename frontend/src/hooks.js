import { useEffect, useRef, useState } from "react";

// Runs `fn` now and then every `ms` milliseconds while the tab is visible.
export function usePolling(fn, deps, ms = 3000) {
    const saved = useRef(fn);
    saved.current = fn;

    useEffect(() => {
        const run = () => Promise.resolve(saved.current()).catch(() => { });
        run();
        const timer = setInterval(() => {
            if (!document.hidden) run();
        }, ms);
        return () => clearInterval(timer);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);
}

// Re-renders every second so countdowns tick.
export function useTick(ms = 1000) {
    const [, set] = useState(0);
    useEffect(() => {
        const t = setInterval(() => set((n) => n + 1), ms);
        return () => clearInterval(t);
    }, [ms]);
}


// Seconds left for a HELD hold, measured against the server's clock; also derives the effective status.
export function holdView(hold) {
    if (!hold) return null;
    if (hold.status !== "HELD") return { status: hold.status, remaining: 0 };
    const serverNow = Date.now() + (hold.clockOffset || 0);
    const remaining = Math.max(0, Math.ceil((Date.parse(hold.expiresAt) - serverNow) / 1000));
    return { status: remaining === 0 ? "EXPIRED" : "HELD", remaining };
}



export function formatRemaining(total) {
    const m = Math.floor(total / 60);
    const s = total % 60;
    return `${m}:${String(s).padStart(2, "0")}`;
}