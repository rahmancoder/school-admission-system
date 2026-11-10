import { useState } from 'react';

function App() {
    const [count, setCount] = useState(0);

    return (
        <div style={{ padding: '20px', fontFamily: 'sans-serif' }}>
            <h1>Welcome to My Vite + React App</h1>
            <p>This is the root component (`src/App.jsx`).</p>

            <button onClick={() => setCount(count + 1)}>
                Count is: {count}
            </button>
        </div>
    );
}

export default App;