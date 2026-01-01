import { useState, useEffect, createContext } from "react";
import LoginPage from "./components/LoginPage";
import GuessingTab from "./components/GuessingTab"; // Make sure this exists
import { api } from "./services/api";
import type { User } from "./types";
import "./App.css";
import ClueGivingTab from "./components/ClueGivingTab";
import GuessHistoryTab from "./components/GuessHistoryTab";
import ClueHistoryTab from "./components/ClueHistoryTab";
import GuessLeaderboardTab from "./components/GuessLeaderboardTab";
import ClueLeaderboardTab from "./components/ClueLeaderboardTab";

export const UserStatsContext = createContext<{
  guessRating: number | null;
  setGuessRating: (rating: number) => void;
  guessRatingChange: number | null;
  setGuessRatingChange: (change: number) => void;
}>({
  guessRating: null,
  setGuessRating: () => {},
  guessRatingChange: null,
  setGuessRatingChange: () => {},
});

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [guessRating, setGuessRating] = useState<number | null>(null);
  const [guessRatingChange, setGuessRatingChange] = useState<number | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [loggedOut, setLoggedOut] = useState(false);
  const [selectedTab, setSelectedTab] = useState<
    | "guessing"
    | "clueGiving"
    | "guessHistory"
    | "clueHistory"
    | "guessLeaderboard"
    | "clueLeaderboard"
  >("guessing");

  // Function to load user data (called on mount AND after login)
  const loadUser = async () => {
    try {
      const userData = await api.getMe();
      setUser(userData);
      setGuessRating(userData.guessRating);
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
        <span>
          User: {user.userName || user.id}, Guess Rating: {guessRating}{" "}
          {guessRatingChange !== null && (
            <span
              className={
                "rating-change " +
                (guessRatingChange > 0
                  ? "positive"
                  : guessRatingChange < 0
                  ? "negative"
                  : "")
              }
            >
              {`(${guessRatingChange >= 0 ? "+" : ""}${guessRatingChange})`}
            </span>
          )}
        </span>
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
          {[
            { key: "guessing", label: "Guess" },
            { key: "clueGiving", label: "Give Clues" },
            { key: "guessHistory", label: "Guess History" },
            { key: "clueHistory", label: "Clue History" },
            { key: "guessLeaderboard", label: "Guess Leaderboard" },
            { key: "clueLeaderboard", label: "Clue Leaderboard" },
          ].map(({ key, label }) => (
            <button
              key={key}
              className={selectedTab === key ? "active" : ""}
              onClick={() => setSelectedTab(key as typeof selectedTab)}
            >
              {label}
            </button>
          ))}
        </div>
        <UserStatsContext.Provider
          value={{
            guessRating,
            setGuessRating,
            guessRatingChange,
            setGuessRatingChange,
          }}
        >
          {selectedTab === "guessing" && <GuessingTab userId={user.id} />}
          {selectedTab === "clueGiving" && <ClueGivingTab userId={user.id} />}
          {selectedTab === "guessHistory" && (
            <GuessHistoryTab userId={user.id} />
          )}
          {selectedTab === "clueHistory" && <ClueHistoryTab userId={user.id} />}
          {selectedTab === "guessLeaderboard" && (
            <GuessLeaderboardTab userId={user.id} />
          )}
          {selectedTab === "clueLeaderboard" && (
            <ClueLeaderboardTab userId={user.id} />
          )}
        </UserStatsContext.Provider>
      </main>
    </div>
  );
}

export default App;
