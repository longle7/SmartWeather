// src/api/smartForecastApi.ts
const API_BASE_URL = "http://localhost:5205";

export type SmartForecastResponse = {
    city: string;
    summary: string;
    temperatureC: number;
    retrievedAtUtc: string;
};

export async function getSmartForecast(city: string): Promise<SmartForecastResponse> {
    const response = await fetch(`${API_BASE_URL}/api/SmartForecast/${encodeURIComponent(city)}`);

    if (!response.ok) {
        throw new Error(`SmartForecast request failed with status ${response.status}`);
    }

    return await response.json();
}