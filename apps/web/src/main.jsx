import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.jsx";
import AdminAccountTools from "./AdminAccountTools.jsx";
import "./styles.css";
import "./ux.css";
import "./login.css";
import "./responsive-fixes.css";
import "./redesign.css";
import "./chat-redesign.css";
import "./metric-help-fix.css";
import "./cmc-theme.css";

ReactDOM.createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <App />
    <AdminAccountTools />
  </React.StrictMode>,
);