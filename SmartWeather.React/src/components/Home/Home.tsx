// src/components/Home/Home.tsx
import React, { useEffect, useState } from "react";
import { getSmartForecast, type SmartForecastResponse } from "../../api/smartForecastApi";

const Home: React.FC = () => {
    const [city] = useState("Boston");
    const [data, setData] = useState<SmartForecastResponse | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let isActive = true;
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setLoading(true);
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setError(null);

        getSmartForecast(city)
            .then((result) => {
                if (!isActive) return;
                setData(result);
            })
            .catch((err) => {
                if (!isActive) return;
                setError(err.message);
            })
            .finally(() => {
                if (!isActive) return;
                setLoading(false);
            });

        return () => {
            isActive = false;
        };
    }, [city]);

    if (loading) {
        return <p>Loading smart forecast...</p>;
    }

    if (error) {
        return <p style={{ color: "red" }}>Error: {error}</p>;
    }

    if (!data) {
        return <p>No data available.</p>;
    }

    return (
        <section>
            <h2>Current Smart Forecast for {data.city}</h2>
            <p>
                <strong>Summary:</strong> {data.summary}
            </p>
            <p>
                <strong>Temperature:</strong> {data.temperatureC} °C
            </p>
            <p>
                <strong>Retrieved at (UTC):</strong> {new Date(data.retrievedAtUtc).toLocaleString()}
            </p>
        </section>
    );
};

export default Home;