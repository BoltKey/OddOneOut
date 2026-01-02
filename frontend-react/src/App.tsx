import { useState, useEffect, createContext, useContext } from "react";
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
  guessEnergy: number | null;
  setGuessEnergy: (energy: number) => void;
  clueEnergy: number | null;
  setClueEnergy: (energy: number) => void;
  nextGuessRegenTime: Date | null;
  setNextGuessRegenTime: (time: Date | null) => void;
  nextClueRegenTime: Date | null;
  setNextClueRegenTime: (time: Date | null) => void;
  loadUser: () => Promise<void>;
}>({
  guessRating: null,
  setGuessRating: () => {},
  guessRatingChange: null,
  setGuessRatingChange: () => {},
  guessEnergy: null,
  setGuessEnergy: () => {},
  clueEnergy: null,
  setClueEnergy: () => {},
  nextGuessRegenTime: null,
  setNextGuessRegenTime: () => {},
  nextClueRegenTime: null,
  setNextClueRegenTime: () => {},
  loadUser: async () => {},
});

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [guessRating, setGuessRating] = useState<number | null>(null);
  const [guessRatingChange, setGuessRatingChange] = useState<number | null>(
    null
  );
  const [loading, setLoading] = useState(true);
  const [loggedOut, setLoggedOut] = useState(false);
  const [guessEnergy, setGuessEnergy] = useState<number | null>(null);
  const [clueEnergy, setClueEnergy] = useState<number | null>(null);
  const [nextGuessRegenTime, setNextGuessRegenTime] = useState<Date | null>(
    null
  );
  const [nextClueRegenTime, setNextClueRegenTime] = useState<Date | null>(null);
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
      setGuessEnergy(userData.guessEnergy);
      setClueEnergy(userData.clueEnergy);
      // add 2 seconds to avoid timing issues
      setNextClueRegenTime(
        userData.nextClueRegenTime === null
          ? null
          : new Date(new Date(userData.nextClueRegenTime).getTime() + 2000)
      );
      setNextGuessRegenTime(
        userData.nextGuessRegenTime === null
          ? null
          : new Date(new Date(userData.nextGuessRegenTime).getTime() + 2000)
      );
      setLoggedOut(false);
    } catch (error) {
      console.log("User not logged in");
      console.log(error);
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
        <div className="logo"></div>
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
            {
              key: "guessing",
              content: (
                <>
                  <span>{"Guess (" + (guessEnergy ?? "0") + ") "}</span>
                  <RegenTimer
                    targetDate={nextGuessRegenTime}
                    onExpire={() => {
                      loadUser();
                    }}
                  />
                </>
              ),
            },
            {
              key: "clueGiving",
              content: (
                <>
                  <span>{"Give Clues (" + (clueEnergy ?? "0") + ") "}</span>
                  <RegenTimer
                    targetDate={nextClueRegenTime}
                    onExpire={() => {
                      loadUser();
                    }}
                  />
                </>
              ),
            },
            { key: "guessHistory", content: "Guess History" },
            { key: "clueHistory", content: "Clue History" },
            { key: "guessLeaderboard", content: "Guess Leaderboard" },
            { key: "clueLeaderboard", content: "Clue Leaderboard" },
          ].map(({ key, content }) => (
            <button
              key={key}
              className={selectedTab === key ? "active" : ""}
              onClick={() => setSelectedTab(key as typeof selectedTab)}
            >
              {content}
            </button>
          ))}
        </div>
        <UserStatsContext.Provider
          value={{
            guessRating,
            setGuessRating,
            guessRatingChange,
            setGuessRatingChange,
            guessEnergy,
            setGuessEnergy,
            clueEnergy,
            setClueEnergy,
            nextGuessRegenTime,
            setNextGuessRegenTime,
            nextClueRegenTime,
            setNextClueRegenTime,
            loadUser,
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

const RegenTimer = ({
  targetDate,
  onExpire,
}: {
  targetDate?: Date | null;
  onExpire: () => void;
}) => {
  const timeLeft = useCountdown(targetDate, onExpire);
  if (!targetDate) return <span className="text-green-500">Full!</span>;
  // This hook handles all the re-rendering logic internally

  return <span className="text-gray-500 text-sm">Next: {timeLeft}</span>;
};

export const useCountdown = (
  targetDate?: Date | null,
  onExpire?: () => void
) => {
  const [timeLeft, setTimeLeft] = useState<string>("0s");

  useEffect(() => {
    if (!targetDate) {
      setTimeLeft("");
      return;
    }

    const targetDateTime = new Date(targetDate).getTime();
    if (targetDateTime <= new Date().getTime()) {
      setTimeLeft("0s");
      return;
    }
    const tick = () => {
      const now = new Date().getTime();
      const difference = targetDateTime - now;

      if (difference <= 0) {
        setTimeLeft("0s");
        if (intervalId) {
          clearInterval(intervalId);
        }
        if (onExpire) onExpire();
      } else {
        setTimeLeft(formatRegenTime(Math.floor(difference / 1000)));
      }
    };
    const intervalId = setInterval(tick, 1000);

    return () => clearInterval(intervalId);
  }, [targetDate, onExpire]);

  return timeLeft;

  function formatRegenTime(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours > 0 ? hours + "h " : ""}${
      minutes > 0 ? minutes + "m " : ""
    }${hours > 0 ? "" : secs + "s"}`;
  }
};

export default App;
