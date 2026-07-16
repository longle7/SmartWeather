// src/components/Reports/Reports.tsx
import React, { useEffect, useState } from "react";
import {
  getAverageTemperatures,
  getSnapshotCounts,
  type CityTemperatureReport,
  type CitySnapshotCountReport,
} from "../../api/weatherReportsApi";

const Reports: React.FC = () => {
  const [avgTemps, setAvgTemps] = useState<CityTemperatureReport[]>([]);
  const [snapshotCounts, setSnapshotCounts] = useState<CitySnapshotCountReport[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isActive = true;

      // eslint-disable-next-line react-hooks/set-state-in-effect
      setLoading(true);
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setError(null);

    Promise.all([getAverageTemperatures(), getSnapshotCounts()])
      .then(([avgTempsData, snapshotCountData]) => {
        if (!isActive) return;
        setAvgTemps(avgTempsData);
        setSnapshotCounts(snapshotCountData);
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
  }, []);

  if (loading) {
    return <p>Loading reports...</p>;
  }

  if (error) {
    return <p style={{ color: "red" }}>Error: {error}</p>;
  }

  return (
    <section>
      <h2>Weather Reports</h2>

      <h3>Average Temperature by City</h3>
      {avgTemps.length === 0 ? (
        <p>No data available.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", marginBottom: "1rem" }}>
          <thead>
            <tr>
              <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>City</th>
              <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>
                Average Temperature (°C)
              </th>
              <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>Snapshot Count</th>
            </tr>
          </thead>
          <tbody>
            {avgTemps.map((row) => (
              <tr key={row.city}>
                <td style={{ padding: "0.25rem 0" }}>{row.city}</td>
                <td style={{ padding: "0.25rem 0" }}>{row.averageTemperatureC.toFixed(1)}</td>
                <td style={{ padding: "0.25rem 0" }}>{row.snapshotCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <h3>Snapshot Counts by City</h3>
      {snapshotCounts.length === 0 ? (
        <p>No data available.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr>
              <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>City</th>
              <th style={{ borderBottom: "1px solid #ccc", textAlign: "left" }}>Snapshot Count</th>
            </tr>
          </thead>
          <tbody>
            {snapshotCounts.map((row) => (
              <tr key={row.city}>
                <td style={{ padding: "0.25rem 0" }}>{row.city}</td>
                <td style={{ padding: "0.25rem 0" }}>{row.snapshotCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
};

export default Reports;