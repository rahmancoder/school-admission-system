const BASE = import.meta.env.VITE_API_URL || "http://localhost:8080";

export class ApiError extends Error {
    constructor(status, body) {
        super(body?.error || `Request failed (${status})`);
        this.status = status;
        this.code = body?.code;
    }
}

async function request(path, { method = "GET", studentId, body } = {}) {

    const headers = {};

    if (studentId) headers["X-Student-Id"] = studentId;
    if (body !== undefined) headers["Content-Type"] = "application/json";

    let res;

    try {
        res = await fetch(BASE + path, {
            method,
            headers,
            body: body !== undefined ? JSON.stringify(body) : undefined,
        });
    }

    catch {
        throw new ApiError(0, { error: "Cannot reach the server. Is the backend running?" });
    }

    let data = null;
    const text = await res.text();
    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = null;
        }
    }
    if (!res.ok) throw new ApiError(res.status, data);
    return data;
}


// Remember the gap between the server clock and this browser's clock so countdowns stay accurate.
const withClock = (hold) => ({
    ...hold,
    clockOffset: Date.parse(hold.serverTime) - Date.now(),
});



export const api = {
    listClasses: () => request("/classes"),

    createClass: (name, totalSeats) => request("/classes", { method: "POST", body: { name, totalSeats } }),

    createHold: async (classId, studentId) =>
        withClock(await request(`/classes/${classId}/holds`, { method: "POST", studentId })),
    getHold: async (holdId, studentId) => withClock(await request(`/holds/${holdId}`, { studentId })),

    confirmHold: async (holdId, studentId) =>
        withClock(await request(`/holds/${holdId}/confirm`, { method: "POST", studentId })),

    cancelHold: async (holdId, studentId) =>
        withClock(await request(`/holds/${holdId}`, { method: "DELETE", studentId })),

    myHolds: async (studentId) => (await request("/me/holds", { studentId })).map(withClock),
};