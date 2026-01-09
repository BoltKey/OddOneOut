import React, { useState, useEffect } from "react";
import { UserStatsContext } from "../App";
import { api } from "../services/api";
import LoginPage from "./LoginPage";
import {
  FaReddit,
  FaMoon,
  FaSun,
  FaUser,
  FaSignOutAlt,
  FaRedo,
  FaUserShield,
  FaCoffee,
  FaHeart,
} from "react-icons/fa";
import "./SettingsTab.css";

interface SettingsTabProps {
  currentDisplayName: string;
}

export default function SettingsTab({ currentDisplayName }: SettingsTabProps) {
  const [nameInput, setNameInput] = useState(currentDisplayName);
  const [isSaved, setIsSaved] = useState(false);
  const [showResetConfirm, setShowResetConfirm] = useState(false);

  useEffect(() => {
    setNameInput(currentDisplayName);
  }, [currentDisplayName]);

  const {
    onDisplayNameChange,
    darkMode,
    setDarkMode,
    setLoggedOut,
    user,
    loadUser,
  } = React.useContext(UserStatsContext);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (nameInput.trim()) {
      if (await onDisplayNameChange(nameInput.trim())) {
        setIsSaved(true);
        setTimeout(() => setIsSaved(false), 2000);
      }
    }
  };

  const handleResetTutorials = () => {
    localStorage.removeItem("seenGuessingTutorial-" + user?.id);
    localStorage.removeItem("seenClueTutorial-" + user?.id);
    setShowResetConfirm(true);
    setTimeout(() => setShowResetConfirm(false), 2000);
  };

  const handleLogout = async () => {
    const isGuest = user?.isGuest;
    const guestUserId = isGuest ? user?.id : null;

    setLoggedOut(true);
    await api.logout();

    if (guestUserId) {
      localStorage.setItem("guestUserId", guestUserId);
    } else {
      localStorage.removeItem("guestUserId");
    }
  };

  return (
    <div className="settings-wrapper">
      <h2 className="settings-title">Settings</h2>

      {/* Display Name Section */}
      <div className="settings-section">
        <div className="settings-section-title">
          <FaUser /> Display Name
        </div>
        <form onSubmit={handleSubmit} className="settings-form">
          <div className="settings-input-group">
            <input
              type="text"
              id="displayName"
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              className="settings-input"
              placeholder="Enter your display name"
            />
            <button type="submit" className="settings-save-btn">
              Save
            </button>
          </div>
          {isSaved && (
            <span className="settings-success">✓ Saved successfully!</span>
          )}
        </form>
      </div>

      {/* Appearance Section */}
      <div className="settings-section">
        <div className="settings-section-title">
          {darkMode ? <FaMoon /> : <FaSun />} Appearance
        </div>
        <div className="settings-toggle-row">
          <span className="settings-toggle-label">Dark Mode</span>
          <button
            className={`settings-toggle ${darkMode ? "active" : ""}`}
            onClick={() => setDarkMode(!darkMode)}
            aria-label="Toggle dark mode"
          >
            <span className="toggle-slider" />
          </button>
        </div>
      </div>

      {/* Tutorial Section */}
      <div className="settings-section">
        <div className="settings-section-title">
          <FaRedo /> Replay Tutorials
        </div>
        <div className="settings-row">
          <button
            className="settings-secondary-btn"
            onClick={handleResetTutorials}
          >
            Reset Tutorials
          </button>
        </div>
        {showResetConfirm && (
          <span className="settings-success">✓ Tutorials will show again!</span>
        )}
      </div>

      {/* Guest Upgrade Section */}
      {user && user.isGuest && (
        <div className="settings-section settings-upgrade-section">
          <div className="settings-section-title">
            <FaUserShield /> Create an Account
          </div>
          <p className="settings-description">
            You're playing as a guest. Create an account to save your progress
            permanently and access your stats from any device!
          </p>
          <LoginPage
            onLoginSuccess={() => {
              loadUser();
            }}
            isGuest={true}
          />
        </div>
      )}

      {/* Logout Section */}
      <div className="settings-section settings-danger-section">
        <button className="settings-logout-btn" onClick={handleLogout}>
          <FaSignOutAlt /> Logout
        </button>
      </div>

      {/* Support & Community Section */}
      <div className="settings-section settings-support-section">
        <div className="settings-section-title">
          <FaHeart /> Support & Community
        </div>
        <div className="support-links">
          <a
            href="https://buymeacoffee.com/BoltKey"
            target="_blank"
            rel="noopener noreferrer"
            className="coffee-link"
          >
            <FaCoffee />
            <span>Enjoy the game? Help cover the server costs!</span>
          </a>
          <a
            href="https://www.reddit.com/r/misfitgame/"
            target="_blank"
            rel="noopener noreferrer"
            className="reddit-settings-link"
          >
            <FaReddit />
            <span>Join r/misfitgame</span>
          </a>
        </div>
      </div>
    </div>
  );
}
