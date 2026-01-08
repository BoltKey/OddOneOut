import React, { useState, useEffect } from "react";
import { UserStatsContext } from "../App";
import { api } from "../services/api";
import LoginPage from "./LoginPage";

interface SettingsTabProps {
  currentDisplayName: string;
}

export default function SettingsTab({ currentDisplayName }: SettingsTabProps) {
  const [nameInput, setNameInput] = useState(currentDisplayName);
  const [isSaved, setIsSaved] = useState(false);

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

  return (
    <div className="p-6 bg-white rounded-lg shadow-md max-w-md mx-auto mt-10">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Settings</h2>
      <div>
        <button onClick={() => setDarkMode(!darkMode)}>Dark mode💡</button>
      </div>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label
            htmlFor="displayName"
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            Display Name
          </label>
          <input
            type="text"
            id="displayName"
            value={nameInput}
            onChange={(e) => setNameInput(e.target.value)}
            className="w-full px-4 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 outline-none transition-colors"
            placeholder="Enter your display name"
          />
        </div>

        <div className="flex items-center justify-between">
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors"
          >
            Save Name
          </button>

          {isSaved && (
            <span className="text-green-600 text-sm font-medium animate-fade-in">
              Saved successfully!
            </span>
          )}
        </div>
      </form>
      {user && user.isGuest && (
        <LoginPage
          onLoginSuccess={() => {
            loadUser();
          }}
          isGuest={true}
        />
      )}
      <button
        onClick={async () => {
          // Preserve guest user ID on logout if it's a guest
          const isGuest = user?.isGuest;
          const guestUserId = isGuest ? user?.id : null;
          
          setLoggedOut(true);
          await api.logout();
          
          // Keep guest ID in localStorage for reuse
          if (guestUserId) {
            localStorage.setItem("guestUserId", guestUserId);
          } else {
            // Clear guest ID if logging out as a registered user
            localStorage.removeItem("guestUserId");
          }
        }}
      >
        Logout
      </button>
    </div>
  );
}
