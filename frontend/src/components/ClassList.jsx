import { useTick, holdView } from "../hooks.js";

export default function ClassList({ classes, myHolds, canApply, onApply, onOpen }) {
    useTick(); // keeps "already applied" state honest as holds expire
    if (classes === null) return <p className="empty">Loading classes…</p>;
    if (classes.length === 0)
        return <p className="empty">No classes yet. Use “Create class” to add the first one.</p>;

    const liveHoldFor = (classId) =>
        myHolds.find((h) => h.classId === classId && ["HELD", "CONFIRMED"].includes(holdView(h).status));

    return (
        <ul className="classes">
            {classes.map((c) => {
                const mine = liveHoldFor(c.id);
                const full = c.availableSeats === 0;
                const pct = (n) => `${(n / c.totalSeats) * 100}%`;
                return (
                    <li key={c.id} className="class">
                        <div className="class-head">
                            <h2>{c.name}</h2>
                            <div className="action">
                                {mine ? (
                                    <button className="secondary" onClick={() => onOpen(mine.id)}>
                                        {holdView(mine).status === "CONFIRMED" ? "Admitted — view" : "View application"}
                                    </button>
                                ) : (
                                    <button onClick={() => onApply(c.id)} disabled={!canApply || full}>
                                        {full ? "Full" : "Apply"}
                                    </button>
                                )}
                            </div>
                        </div>


                        <div className="seatbar" role="img"
                            aria-label={`${c.confirmedSeats} confirmed, ${c.heldSeats} held, ${c.availableSeats} available of ${c.totalSeats}`}>
                            <span className="confirmed" style={{ width: pct(c.confirmedSeats) }} />
                            <span className="held" style={{ width: pct(c.heldSeats) }} />
                        </div>


                        <dl className="counts">
                            <div><dt>Total</dt><dd>{c.totalSeats}</dd></div>
                            <div><dt>Confirmed</dt><dd className="c-confirmed">{c.confirmedSeats}</dd></div>
                            <div><dt>Held</dt><dd className="c-held">{c.heldSeats}</dd></div>
                            <div><dt>Available</dt><dd>{c.availableSeats}</dd></div>
                        </dl>
                        {!canApply && <p className="hint">Enter your birth registration number to apply.</p>}
                    </li>
                );
            })}
        </ul>
    );
}