import { useState, useEffect } from "react";
import LoginPage from "./components/LoginPage";
import GuessingTab from "./components/GuessingTab"; // Make sure this exists
import { api } from "./services/api";
import type { User } from "./types";
import "./App.css";
import ClueGivingTab from "./components/ClueGivingTab";
import GuessHistoryTab from "./components/GuessHistoryTab";
import ClueHistoryTab from "./components/ClueHistoryTab";

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [loggedOut, setLoggedOut] = useState(false);
  const [selectedTab, setSelectedTab] = useState<
    "guessing" | "clueGiving" | "guessHistory" | "clueHistory"
  >("guessing");

  // Function to load user data (called on mount AND after login)
  const loadUser = async () => {
    try {
      const userData = await api.getMe();
      setUser(userData);
      setLoggedOut(false);
    } catch (error) {
      console.log("User not logged in");
      setUser(null);
    } finally {
      setLoading(false);
    }
  };

  // Check login status on page load
  useEffect(() => {
    loadUser();
  }, []);

  if (loading) return <div>Loading...</div>;

  // If not logged in, show Login Page
  if (!user || loggedOut) {
    return (
      <LoginPage
        onLoginSuccess={() => {
          setLoading(true);
          loadUser();
        }}
      />
    );
  }

  // If logged in, show Game
  return (
    <div className="app-container">
      <header
        style={{
          padding: 10,
          background: "#eee",
          display: "flex",
          justifyContent: "space-between",
        }}
      >
        <span>User: {user.userName || user.id}</span>
        <button
          onClick={() => {
            setLoggedOut(true);
          }}
        >
          Logout
        </button>
        {/* Simple logout: Reload page -> API returns 401 -> shows Login */}
      </header>

      <main>
        <div className="tab-buttons">
          <button
            className={selectedTab === "guessing" ? "active" : ""}
            onClick={() => setSelectedTab("guessing")}
          >
            Guess
          </button>
          <button
            className={selectedTab === "clueGiving" ? "active" : ""}
            onClick={() => setSelectedTab("clueGiving")}
          >
            Give Clues
          </button>
          <button
            className={selectedTab === "guessHistory" ? "active" : ""}
            onClick={() => setSelectedTab("guessHistory")}
          >
            Guess History
          </button>
          <button
            className={selectedTab === "clueHistory" ? "active" : ""}
            onClick={() => setSelectedTab("clueHistory")}
          >
            Clue History
          </button>
        </div>
        {selectedTab === "guessing" && <GuessingTab userId={user.id} />}
        {selectedTab === "clueGiving" && <ClueGivingTab userId={user.id} />}
        {selectedTab === "guessHistory" && <GuessHistoryTab userId={user.id} />}
        {selectedTab === "clueHistory" && <ClueHistoryTab userId={user.id} />}
      </main>
    </div>
  );
}

export default App;
