import { useTick, holdView, formatRemaining } from "../hooks.js";

const fmt = (iso) => new Date(iso).toLocaleString();

export default function MyApplications({ holds, onOpen }) {
    useTick();
    if (holds.length === 0) return <p className="empty">You haven’t applied to any class yet.</p>;

    return (
        <table className="history">
            <thead>
                <tr><th>Class</th><th>Status</th><th>Applied</th><th>Time left</th><th /></tr>
            </thead>
            <tbody>
                {holds.map((h) => {
                    const v = holdView(h);
                    return (
                        <tr key={h.id}>
                            <td>{h.className}</td>
                            <td><span className={`badge ${v.status.toLowerCase()}`}>{v.status}</span></td>
                            <td>{fmt(h.createdAt)}</td>
                            <td>{v.status === "HELD" ? formatRemaining(v.remaining) : "–"}</td>
                            <td><button className="secondary" onClick={() => onOpen(h.id)}>Open</button></td>
                        </tr>
                    );
                })}
            </tbody>
        </table>
    );
}