// src/components/Layout/NavTabs.tsx
import React from "react";
import { NavLink } from "react-router-dom";

const NavTabs: React.FC = () => {
    return (
        <nav style={{ borderBottom: "1px solid #ccc", paddingBottom: "0.5rem" }}>
            <NavLink
                to="/"
                end
                style={({ isActive }) => ({
                    marginRight: "1rem",
                    textDecoration: "none",
                    fontWeight: isActive ? "bold" : "normal",
                })}
            >
                Home
            </NavLink>
            <NavLink
                to="/saved"
                style={({ isActive }) => ({
                    marginRight: "1rem",
                    textDecoration: "none",
                    fontWeight: isActive ? "bold" : "normal",
                })}
            >
                Saved Weather
            </NavLink>
        </nav>
    );
};

export default NavTabs;