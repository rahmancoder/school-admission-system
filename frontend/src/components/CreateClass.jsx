import { useState } from "react";
import { api } from "../api.js";

export default function CreateClass({ onCreated, onError }) {

    const [name, setName] = useState("");
    const [seats, setSeats] = useState("");
    const [busy, setBusy] = useState(false);


    const submit = async (e) => {
        e.preventDefault();
        setBusy(true);
        try {
            const created = await api.createClass(name.trim(), Number(seats));
            setName("");
            setSeats("");
            onCreated(created);
        } catch (err) {
            onError(err.message);
        } finally {
            setBusy(false);
        }
    };


    return (
        <form className="panel form" onSubmit={submit}>
            <h2>Create a class</h2>
            <p className="hint">Each shift is its own class, for example “Class 6 – Morning Shift”.</p>

            <label>
                Class and shift name
                <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Class 6 – Morning Shift" required maxLength={200} />
            </label>

            <label>
                Total seats
                <input type="number" min="1" step="1" value={seats} onChange={(e) => setSeats(e.target.value)} placeholder="40" required />
            </label>

            <button disabled={busy || !name.trim() || !(Number(seats) > 0)}>Create class</button>
        </form>
    );
}