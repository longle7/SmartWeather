// src/api/weatherSnapshotsApi.ts
const API_BASE_URL = "http://localhost:5205";

export type WeatherSnapshot = {
    id: number;
    city: string;
    conditionText: string;
    temperatureC: number;
    retrievedAtUtc: string;
};

export async function getWeatherSnapshots(): Promise<WeatherSnapshot[]> {
    const response = await fetch(`${API_BASE_URL}/api/WeatherSnapshots`);

    if (!response.ok) {
        throw new Error(`WeatherSnapshots request failed with status ${response.status}`);
    }

    return await response.json();
}