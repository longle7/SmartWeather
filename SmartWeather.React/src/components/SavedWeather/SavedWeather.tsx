// src/components/SavedWeather/SavedWeather.tsx
import React, { useEffect, useState } from "react";
import { getWeatherSnapshots, type WeatherSnapshot } from "../../api/weatherSnapshotsApi";

const SavedWeather: React.FC = () => {
    const [snapshots, setSnapshots] = useState<WeatherSnapshot[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setLoading(true);
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setError(null);

        getWeatherSnapshots()
            .then(setSnapshots)
            .catch((err) => setError(err.message))
            .finally(() => setLoading(false));
    }, []);

    if (loading) {
        return <p>Loading saved weather...</p>;
    }

    if (error) {
        return <p style={{ color: "red" }}>Error: {error}</p>;
    }

    if (snapshots.length === 0) {
        return <p>No saved weather snapshots yet.</p>;
    }

    return (
        <section>
            <h2>Saved Weather Snapshots</h2>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                    <tr>
                        <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>City</th>
                        <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>Condition</th>
                        <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>Temperature (°C)</th>
                        <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>Retrieved (UTC)</th>
                    </tr>
                </thead>
                <tbody>
                    {snapshots.map((s) => (
                        <tr key={s.id}>
                            <td style={{ padding: "0.25rem 0" }}>{s.city}</td>
                            <td style={{ padding: "0.25rem 0" }}>{s.conditionText}</td>
                            <td style={{ padding: "0.25rem 0" }}>{s.temperatureC}</td>
                            <td style={{ padding: "0.25rem 0" }}>
                                {new Date(s.retrievedAtUtc).toLocaleString()}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    );
};

export default SavedWeather;