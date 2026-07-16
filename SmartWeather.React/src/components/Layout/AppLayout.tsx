// src/components/Layout/AppLayout.tsx
import React from "react";
import NavTabs from "./NavTabs";

type AppLayoutProps = {
    children: React.ReactNode;
};

const AppLayout: React.FC<AppLayoutProps> = ({ children }) => {
    return (
        <div style={{ maxWidth: "960px", margin: "0 auto", padding: "1rem" }}>
            <header>
                <h1>SmartWeather</h1>
            </header>
            <NavTabs />
            <main style={{ marginTop: "1rem" }}>{children}</main>
        </div>
    );
};

export default AppLayout;