import { useState, useEffect, createContext, useContext, use } from "react";
import LoginPage from "./components/LoginPage";
import GuessingTab from "./components/GuessingTab"; // Make sure this exists
import { api } from "./services/api";
import type { User } from "./types";
import "./App.css";
import ClueGivingTab from "./components/ClueGivingTab";
import GuessHistoryTab from "./components/GuessHistoryTab";
import ClueHistoryTab from "./components/ClueHistoryTab";
import LeaderboardTab from "./components/LeaderboardTab";
import SettingsTab from "./components/SettingsTab";
import { FaHistory, FaSearch, FaTrophy, FaReddit } from "react-icons/fa";
import { FaGear } from "react-icons/fa6";
import { RiUserSettingsFill } from "react-icons/ri";
import { Tooltip } from "@mui/material";
import { PiCardsThree } from "react-icons/pi";
import { TbReportSearch } from "react-icons/tb";
import {
  BiSolidMessageRounded,
  BiSolidMessageRoundedDetail,
} from "react-icons/bi";

export const UserStatsContext = createContext<{
  user: User | null;
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
  onDisplayNameChange: (newName: string) => Promise<boolean>;
  darkMode: boolean;
  setDarkMode: (darkMode: boolean) => void;
  setLoggedOut: (loggedOut: boolean) => void;
}>({
  user: null,
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
  onDisplayNameChange: async (newName: string) => false,
  darkMode: false,
  setDarkMode: () => {},
  setLoggedOut: () => {},
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
  const [darkMode, setDarkMode] = useState<boolean>(() => {
    const savedMode = localStorage.getItem("darkMode");
    if (savedMode !== null) {
      return savedMode === "true";
    }
    return window.matchMedia("(prefers-color-scheme: dark)").matches;
  });
  const [openModal, setOpenModal] = useState<
    null | "guessHistory" | "clueHistory" | "leaderboard" | "settings"
  >(null);
  useEffect(() => {
    localStorage.setItem("darkMode", darkMode ? "true" : "false");
    document.body.className = darkMode ? "dark-mode" : "";
  }, [darkMode]);

  const [selectedTab, setSelectedTab] = useState<"guessing" | "clueGiving">(
    "guessing"
  );

  // Function to load user data (called on mount AND after login)
  const loadUser = async () => {
    try {
      const userData = await api.getMe();
      setUser(userData);
      setGuessRating(userData.guessRating);
      setGuessEnergy(userData.guessEnergy);
      setClueEnergy(userData.clueEnergy);
      // Store guest user ID and display name in localStorage if it's a guest
      if (userData.isGuest) {
        localStorage.setItem("guestUserId", userData.id);
        if (userData.displayName) {
          localStorage.setItem("guestDisplayName", userData.displayName);
        }
      }
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
  const onDisplayNameChange = async (newName: string) => {
    // Handle display name change here
    console.log("New display name:", newName);
    try {
      await api.changeDisplayName(newName);
      await loadUser();
    } catch (error) {
      console.error("Failed to change display name:", error);
    }
    return true;
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
    <div className={`app-container ${darkMode ? "dark-mode" : ""}`}>
      <header className="header">
        <div className="logo"></div>
        <div className="tab-buttons">
          {[
            {
              key: "guessHistory",
              tooltip: "Guessing History",
              content: <TbReportSearch />,
            },
            {
              key: "clueHistory",
              tooltip: "Clue History",
              content: <BiSolidMessageRoundedDetail />,
            },
            {
              key: "leaderboard",
              tooltip: "Leaderboards",
              content: (
                <>
                  <FaTrophy />
                </>
              ),
            },
            {
              key: "settings",
              tooltip: "Settings",
              content: <RiUserSettingsFill />,
            },
          ].map(({ key, content, tooltip }) => (
            <Tooltip title={tooltip} id={tooltip}>
              <button
                key={key}
                className={(openModal === key ? "active" : "") + " nav-button"}
                onClick={() => setOpenModal(key as typeof openModal)}
                data-tooltip-content={tooltip}
                data-tooltip-id={tooltip}
              >
                {content}
              </button>
            </Tooltip>
          ))}
          <Tooltip title="Join us on Reddit" id="reddit">
            <a
              href="https://www.reddit.com/r/misfitgame/"
              target="_blank"
              rel="noopener noreferrer"
              className="reddit-header-link"
              data-tooltip-content="Join us on Reddit"
              data-tooltip-id="reddit"
            >
              <FaReddit />
            </a>
          </Tooltip>
        </div>
      </header>

      <main>
        <div className="main-nav-wrapper">
          {[
            {
              key: "guessing",
              content: (
                <>
                  <span>
                    <FaSearch />
                    {" Guess (" + (guessEnergy ?? "0") + ") "}
                  </span>
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
                  <Tooltip
                    title={
                      user?.canGiveClues
                        ? null
                        : `You need at least ${10} guesses to unlock clue giving.`
                    }
                    id="give-clues-tooltip"
                  >
                    <span>
                      <BiSolidMessageRounded />
                      {" Give Clues (" + (clueEnergy ?? "0") + ") "}
                    </span>
                  </Tooltip>
                  <RegenTimer
                    targetDate={nextClueRegenTime}
                    onExpire={() => {
                      loadUser();
                    }}
                  />
                </>
              ),
            },
          ].map(({ key, content }) => (
            <button
              key={key}
              className={selectedTab === key ? "active" : ""}
              onClick={() => setSelectedTab(key as "guessing" | "clueGiving")}
              disabled={key === "clueGiving" && !user?.canGiveClues}
            >
              {content}
            </button>
          ))}
        </div>
        <UserStatsContext.Provider
          value={{
            user,
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
            onDisplayNameChange,
            darkMode,
            setDarkMode,
            setLoggedOut,
          }}
        >
          {selectedTab === "guessing" ? (
            <GuessingTab userId={user.id} />
          ) : (
            <ClueGivingTab userId={user.id} />
          )}
          {openModal && (
            <div
              className="modal-overlay"
              onClick={() => setOpenModal(null)}
              style={{
                position: "fixed",
                top: 0,
                left: 0,
                right: 0,
                bottom: 0,
                backgroundColor: "rgba(0,0,0,0.7)",
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                zIndex: 1000,
              }}
            >
              <div
                className={`modal-content ${darkMode ? "dark-mode" : ""}`}
                onClick={(e) => {
                  e.stopPropagation();
                }}
                style={{
                  backgroundColor: darkMode ? "#1a1a1a" : "white",
                  color: darkMode ? "white" : "black",
                  padding: "20px",
                  borderRadius: "8px",
                  position: "relative",
                  maxWidth: "98%",
                  maxHeight: "90%",
                  overflow: "auto",
                  minWidth: "300px",
                  boxShadow: "0 4px 6px rgba(0, 0, 0, 0.1)",
                }}
              >
                <button
                  className="close-modal"
                  onClick={() => setOpenModal(null)}
                  style={{
                    position: "absolute",
                    top: "10px",
                    right: "10px",
                    background: "none",
                    border: "none",
                    fontSize: "1.5rem",
                    cursor: "pointer",
                    color: "inherit",
                    padding: "0 5px",
                  }}
                >
                  ×
                </button>
                {openModal === "guessHistory" && (
                  <GuessHistoryTab userId={user.id} />
                )}
                {openModal === "clueHistory" && (
                  <ClueHistoryTab userId={user.id} />
                )}
                {openModal === "leaderboard" && (
                  <LeaderboardTab userId={user.id} />
                )}
                {openModal === "settings" && (
                  <SettingsTab
                    currentDisplayName={
                      user.displayName || user.userName || "User"
                    }
                  />
                )}
              </div>
            </div>
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
