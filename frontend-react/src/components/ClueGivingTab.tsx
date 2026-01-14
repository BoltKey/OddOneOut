import { useCallback, useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext, useCountdown } from "../App";
import { BiSolidMessageRoundedDetail } from "react-icons/bi";
import { FaSearch } from "react-icons/fa";
import "./ClueGivingTab.css";

export default function ClueGivingTab({ userId }: { userId: string }) {
  const [currentCards, setCurrentCards] = useState<string[] | null>(null);
  const [wordSetId, setWordSetId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [clue, setClue] = useState<string>("");
  const [submitStatus, setSubmitStatus] = useState<string | null>(null);
  const [submitStats, setSubmitStats] = useState<{
    totalClueGiversCount: number;
    differentCluesCount: number;
  } | null>(null);
  const [selectedCardIndex, setSelectedCardIndex] = useState<number | null>(
    null
  );
  const [tutorialStep, setTutorialStep] = useState<number>(0);
  const [tutorialMessage, setTutorialMessage] =
    useState<React.ReactNode | null>(null);
  const [outOfClues, setOutOfClues] = useState(false);

  const { loadUser, clueEnergy, nextClueRegenTime, navigateToTab, openModal } =
    useContext(UserStatsContext);

  // Track previous clueEnergy to detect when it increases
  const [prevClueEnergy, setPrevClueEnergy] = useState<number | null>(null);

  // Countdown hook for when out of clues
  const countdownText = useCountdown(
    clueEnergy === 0 ? nextClueRegenTime : null,
    () => {
      loadUser();
    }
  );

  const isWaitingForEnergy = clueEnergy === 0 && nextClueRegenTime;

  const fetchAssignedGame = async () => {
    try {
      const assigned = await api.assignedClueGiving();
      let { id, words } = assigned;
      setCurrentCards(words);
      setWordSetId(id);
      setMessage(null);
      setOutOfClues(false);
      await loadUser();
    } catch (err: any) {
      // Check if user is out of clues
      const errorMsg = err.message?.toLowerCase() || "";
      if (
        errorMsg.includes("no clue") ||
        errorMsg.includes("out of") ||
        errorMsg.includes("energy")
      ) {
        setOutOfClues(true);
        setMessage(null);
      } else {
        setMessage(err.message);
        setOutOfClues(false);
      }
      await loadUser();
    }
  };

  useEffect(() => {
    fetchAssignedGame();
  }, []);

  // When countdown expires and clueEnergy increases, auto-fetch new game
  useEffect(() => {
    if (
      prevClueEnergy === 0 &&
      clueEnergy !== null &&
      clueEnergy > 0 &&
      outOfClues
    ) {
      fetchAssignedGame();
    }
    setPrevClueEnergy(clueEnergy);
  }, [clueEnergy, outOfClues]);

  useEffect(() => {
    let storageKey = "seenClueTutorial-" + userId;
    let forceTutorial = false;
    if (localStorage.getItem(storageKey) && !forceTutorial) {
      setTutorialStep(6);
    } else {
      setTutorialStep(1);
    }
  }, []);

  useEffect(() => {
    if (tutorialStep === 0) return;
    let tutorialMessages: { [key: number]: React.ReactNode } = {
      1: (
        <div>
          <div style={{ marginBottom: "10px" }}>
            Let's learn how to create your own clues!
          </div>
          <button onClick={advanceTutorial}>Let's go!</button>
        </div>
      ),
      2: (
        <>
          <div style={{ marginBottom: "10px" }}>
            You are given <strong>5 words</strong>. Pick one as the{" "}
            <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong>. The
            other 4 words become{" "}
            <strong style={{ color: "var(--insetcolor)" }}>Matches</strong>.
            Create a clue that connects them, but <strong>not</strong> the
            Misfit. You have quite limited number of clues you can give, so take
            your time!
          </div>
        </>
      ),
      3: (
        <>
          <div style={{ marginBottom: "10px" }}>
            You can check how your clue is doing in the Clue History tab. Either
            somebody else already had the same clue (in which case there may
            already be some guesses), or you were the first to give it, in which
            case you will have to wait for other players to guess.
          </div>
        </>
      ),
      4: null,
    };
    setTutorialMessage(tutorialMessages[tutorialStep]);
  }, [tutorialStep, currentCards]);

  const advanceTutorial = useCallback(() => {
    let storageKey = "seenClueTutorial-" + userId;
    if (tutorialStep === 2) {
      localStorage.setItem(storageKey, "true");
    }
    if (tutorialStep === 3) {
      // Complete tutorial - jump to step 6
      setTutorialStep(4);
      localStorage.setItem(storageKey, "true");
    } else {
      setTutorialStep(tutorialStep + 1);
    }
  }, [tutorialStep, userId]);

  const handleCardClick = (index: number) => {
    setSelectedCardIndex(index);
  };

  return (
    <div className="clue-giving-wrapper">
      {tutorialMessage && (
        <div className="tutorial-message">{tutorialMessage}</div>
      )}
      {message && !outOfClues && <div className="error-message">{message}</div>}
      {outOfClues && (
        <div className="out-of-clues-screen">
          <div className="out-of-clues-icon">⏳</div>
          <h2 className="out-of-clues-title">
            You are done for now, wait for guessers and check back later in the
            clue history how you did!
          </h2>
          <div className="out-of-clues-stats">
            {nextClueRegenTime && (
              <div className="out-of-clues-countdown">
                <span className="countdown-label">Next clue in:</span>
                <span className="countdown-value">{countdownText}</span>
              </div>
            )}
          </div>
          <div className="out-of-clues-actions">
            <p className="actions-label">In the meantime...</p>
            <button
              className="action-button action-history"
              onClick={() => openModal("clueHistory")}
            >
              <BiSolidMessageRoundedDetail /> View Clue History
            </button>
            <button
              className="action-button action-guess"
              onClick={() => navigateToTab("guessing")}
            >
              <FaSearch /> Make Guesses
            </button>
          </div>
        </div>
      )}
      {!outOfClues && tutorialStep >= 2 && currentCards && (
        <>
          <div className="clue-instructions">
            {tutorialStep >= 3 ? (
              <>
                Select the{" "}
                <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong>{" "}
                and create a clue for the{" "}
                <strong style={{ color: "var(--insetcolor)" }}>Matches</strong>
              </>
            ) : (
              ""
            )}
          </div>
          <div className="clue-cards-container">
            {currentCards.map((word, index) => (
              <div
                key={index}
                onClick={() => handleCardClick(index)}
                className={
                  "clue-card" +
                  (selectedCardIndex === index
                    ? " selected"
                    : selectedCardIndex === null
                    ? ""
                    : " matching")
                }
              >
                {word}
              </div>
            ))}
          </div>
        </>
      )}
      {!outOfClues && tutorialStep >= 2 && (
        <div className="clue-input-section">
          <div className="clue-input-label">Your clue:</div>
          <input
            type="text"
            className="clue-input"
            placeholder="Enter a single English word"
            value={clue}
            onChange={(e) => setClue(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && clue.trim() && !submitStatus) {
                handleSubmit();
              }
            }}
          />
          {submitStatus ? (
            <div className="submit-status">
              <div className="submit-status-message">{submitStatus}</div>
              {submitStats && submitStats.totalClueGiversCount > 0 && (
                <button
                  className="check-it-out-button"
                  onClick={() => openModal("clueHistory")}
                >
                  Check it out
                </button>
              )}
              {isWaitingForEnergy ? (
                <button
                  className="next-game-button button-done"
                  onClick={() => {
                    setClue("");
                    setSelectedCardIndex(null);
                    setSubmitStatus(null);
                    setSubmitStats(null);
                    setCurrentCards(null);
                    setOutOfClues(true);
                  }}
                >
                  Done
                </button>
              ) : (
                <button
                  className="next-game-button"
                  onClick={async () => {
                    setClue("");
                    setSelectedCardIndex(null);
                    setSubmitStatus(null);
                    setSubmitStats(null);
                    if (tutorialStep === 2) {
                      advanceTutorial();
                    }
                    await fetchAssignedGame();
                  }}
                >
                  Next Game
                </button>
              )}
            </div>
          ) : (
            <button
              className="submit-clue-button"
              onClick={handleSubmit}
              disabled={!clue.trim()}
            >
              Submit Clue
            </button>
          )}
        </div>
      )}
    </div>
  );

  async function handleSubmit() {
    setMessage(null);
    let result;
    try {
      result = await api.submitClue(
        clue,
        wordSetId!,
        currentCards![selectedCardIndex!]
      );
    } catch (err: any) {
      setMessage(err.message);
      return;
    }
    if (tutorialStep === 3) {
      advanceTutorial();
    }

    // Build a single combined message
    const totalClueGiversCount = result.totalClueGiversCount;
    const differentCluesCount = result.differentCluesCount;

    let message = "Clue submitted! ";

    if (totalClueGiversCount === 1) {
      // You're the only cluegiver for this word set
      message +=
        "You were the first to give a clue for this word set - you will have to wait for other players to guess.";
    } else {
      // Check if there are also different clues
      const otherDifferentClues = differentCluesCount - 1;
      if (otherDifferentClues > 0) {
        message += ` ${otherDifferentClues} other clue${
          otherDifferentClues !== 1 ? "s" : ""
        } were given by ${
          totalClueGiversCount - 1
        } other players for this set.`;
      } else {
        message += ` ${
          totalClueGiversCount - 1
        } other players gave the same clue.`;
      }
    }

    setSubmitStatus(message);
    // Store stats for button visibility
    setSubmitStats({
      totalClueGiversCount: result.totalClueGiversCount,
      differentCluesCount: result.differentCluesCount,
    });
    await loadUser();
  }
}
