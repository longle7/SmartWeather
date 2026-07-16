// src/routes/AppRoutes.tsx
import React from "react";
import { Routes, Route } from "react-router-dom";
import Home from "../components/Home/Home";
import SavedWeather from "../components/SavedWeather/SavedWeather";
import Reports from "../components/Reports/Reports";
import AppLayout from "../components/Layout/AppLayout";

const AppRoutes: React.FC = () => {
    return (
        <AppLayout>
            <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/saved" element={<SavedWeather />} />
                <Route path="/reports" element={<Reports />} />
            </Routes>
        </AppLayout>
    );
};

export default AppRoutes;