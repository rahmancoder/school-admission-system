import { useCallback, useEffect, useState } from "react";
import { api } from "./api.js";
import { usePolling } from "./hooks.js";
import ClassList from "./components/ClassList.jsx";

import CreateClass from "./components/CreateClass.jsx";

import ApplicationView from "./components/ApplicationView.jsx";

import MyApplications from "./components/MyApplications.jsx";

const STUDENT_KEY = "studentId";
const isValidId = (v) => /^\d{17}$/.test(v);

export default function App() {
    const [studentId, setStudentId] = useState(() => localStorage.getItem(STUDENT_KEY) || "");
    const [tab, setTab] = useState("classes");
    const [classes, setClasses] = useState(null);
    const [myHolds, setMyHolds] = useState([]);
    const [activeHoldId, setActiveHoldId] = useState(null);
    const [notice, setNotice] = useState(null);

    const valid = isValidId(studentId);

    useEffect(() => {
        localStorage.setItem(STUDENT_KEY, studentId);
    }, [studentId]);

    const refreshClasses = useCallback(async () => {
        try {
            setClasses(await api.listClasses());
        } catch (e) {
            setNotice({ kind: "error", text: e.message });
        }
    }, []);

    const refreshHolds = useCallback(async () => {
        if (!valid) {
            setMyHolds([]);
            return;
        }
        try {
            setMyHolds(await api.myHolds(studentId));
        } catch {
            /* transient; next poll retries */
        }
    }, [valid, studentId]);

    // Seat counts and the student's applications refresh every 3 seconds without a page reload.
    usePolling(refreshClasses, [], 3000);
    usePolling(refreshHolds, [valid, studentId], 3000);

    const refreshAll = () => Promise.all([refreshClasses(), refreshHolds()]);

    const apply = async (classId) => {
        try {
            const hold = await api.createHold(classId, studentId);
            setNotice({ kind: "ok", text: `Seat held in ${hold.className}. Pay the admission fee before time runs out.` });
            setActiveHoldId(hold.id);
            setTab("application");
            refreshAll();
        } catch (e) {
            setNotice({ kind: "error", text: e.message });
            refreshAll();
        }
    };

    const openApplication = (id) => {
        setActiveHoldId(id);
        setTab("application");
    };

    const tabs = [
        ["classes", "Classes"],
        ["mine", "My applications"],
        ["admin", "Create class"],
    ];
    if (activeHoldId) tabs.splice(1, 0, ["application", "Current application"]);

    return (
        <div className="shell">
            <header className="top">
                <h1>School admission</h1>
                <label className="student">
                    <span>Birth registration number</span>
                    <input
                        inputMode="numeric"
                        maxLength={17}
                        placeholder="17 digits"
                        value={studentId}
                        onChange={(e) => setStudentId(e.target.value.replace(/\D/g, ""))}
                        aria-invalid={studentId.length > 0 && !valid}
                    />
                    <small className={valid ? "good" : studentId ? "bad" : ""}>
                        {valid ? "Signed in as this student" : `${studentId.length}/17 digits`}
                    </small>
                </label>
            </header>

            <nav className="tabs" role="tablist">
                {tabs.map(([key, label]) => (
                    <button key={key} role="tab" aria-selected={tab === key} onClick={() => setTab(key)}>
                        {label}
                    </button>
                ))}
            </nav>

            {notice && (
                <div className={`notice ${notice.kind}`} role="status">
                    <span>{notice.text}</span>
                    <button onClick={() => setNotice(null)} aria-label="Dismiss">×</button>
                </div>
            )}



            <main>





                {tab === "classes" && (
                    <ClassList classes={classes} myHolds={myHolds} canApply={valid} onApply={apply} onOpen={openApplication} />
                )}


                {tab === "application" && activeHoldId && (
                    <ApplicationView
                        holdId={activeHoldId}
                        studentId={studentId}
                        onChanged={refreshAll}
                        onNotice={setNotice}
                    />
                )}

                {tab === "mine" &&
                    (valid ? (
                        <MyApplications holds={myHolds} onOpen={openApplication} />
                    ) : (
                        <p className="empty">Enter your 17-digit birth registration number above to see your applications.</p>
                    ))}



                {tab === "admin" && (
                    <CreateClass
                        onCreated={(c) => {
                            setNotice({ kind: "ok", text: `Created ${c.name} with ${c.totalSeats} seats.` });
                            refreshClasses();
                            setTab("classes");
                        }}
                        onError={(text) => setNotice({ kind: "error", text })}
                    />
                )}


            </main>


        </div>
    );
}