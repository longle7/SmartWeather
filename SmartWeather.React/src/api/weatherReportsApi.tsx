// src/api/weatherReportsApi.ts
const API_BASE_URL = "http://localhost:5205"; // or 8080 if using Docker

export type CityTemperatureReport = {
    city: string;
    averageTemperatureC: number;
    snapshotCount: number;
};

export type CitySnapshotCountReport = {
    city: string;
    snapshotCount: number;
};

export async function getAverageTemperatures(): Promise<CityTemperatureReport[]> {
    const response = await fetch(`${API_BASE_URL}/api/WeatherReports/average-temperatures`);

    if (!response.ok) {
        throw new Error(`Average temperatures request failed with status ${response.status}`);
    }

    return await response.json();
}

export async function getSnapshotCounts(): Promise<CitySnapshotCountReport[]> {
    const response = await fetch(`${API_BASE_URL}/api/WeatherReports/snapshot-counts`);

    if (!response.ok) {
        throw new Error(`Snapshot counts request failed with status ${response.status}`);
    }

    return await response.json();
}