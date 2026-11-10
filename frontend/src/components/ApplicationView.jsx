import { useState } from "react";
import { api } from "../api.js";
import { usePolling, useTick, holdView, formatRemaining } from "../hooks.js";

export default function ApplicationView({ holdId, studentId, onChanged, onNotice }) {

    const [hold, setHold] = useState(null);
    const [error, setError] = useState(null);
    const [busy, setBusy] = useState(false);

    useTick();

    usePolling(async () => {
        try {
            setHold(await api.getHold(holdId, studentId));
            setError(null);
        } catch (e) {
            setError(e.message);
        }
    }, [holdId, studentId], 2000);

    const act = async (fn, okText) => {
        setBusy(true);
        try {
            setHold(await fn(holdId, studentId));
            onNotice({ kind: "ok", text: okText });
        } catch (e) {
            onNotice({ kind: "error", text: e.message });
        } finally {
            setBusy(false);
            onChanged();
        }
    };

    if (error && !hold) return <p className="empty">{error}</p>;
    if (!hold) return <p className="empty">Loading application…</p>;

    const { status, remaining } = holdView(hold);
    const label = { HELD: "Seat held", CONFIRMED: "Admission confirmed", EXPIRED: "Expired", CANCELLED: "Cancelled" }[status];

    return (
        <section className={`panel app app-${status.toLowerCase()}`}>
            <p className="app-class">{hold.className}</p>
            <h2>{label}</h2>

            {status === "HELD" && (
                <>
                    <p className="timer" aria-live="off">{formatRemaining(remaining)}</p>
                    <p className="hint">Pay the admission fee before the timer ends or the seat goes back to other applicants.</p>
                    <div className="buttons">
                        <button disabled={busy} onClick={() => act(api.confirmHold, "Payment received. Admission confirmed.")}>
                            Pay admission fee
                        </button>
                        <button className="danger" disabled={busy} onClick={() => act(api.cancelHold, "Application cancelled.")}>
                            Cancel application
                        </button>
                    </div>
                </>
            )}
            {status === "CONFIRMED" && <p>Your seat is confirmed. Nothing more to do.</p>}
            {status === "EXPIRED" && <p>The payment window closed and the seat was released. Go to Classes to apply again if seats remain.</p>}
            {status === "CANCELLED" && <p>You cancelled this application and the seat was released.</p>}
        </section>
    );
}