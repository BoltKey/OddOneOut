import { useCallback, useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
import "./ClueGivingTab.css";

export default function ClueGivingTab({ userId }: { userId: string }) {
  const [currentCards, setCurrentCards] = useState<string[] | null>(null);
  const [wordSetId, setWordSetId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [clue, setClue] = useState<string>("");
  const [submitStatus, setSubmitStatus] = useState<string | null>(null);
  const [selectedCardIndex, setSelectedCardIndex] = useState<number | null>(
    null
  );
  const [tutorialStep, setTutorialStep] = useState<number>(0);
  const [tutorialMessage, setTutorialMessage] =
    useState<React.ReactNode | null>(null);
  const { loadUser } = useContext(UserStatsContext);

  const fetchAssignedGame = async () => {
    try {
      const assigned = await api.assignedClueGiving();
      let { id, words } = assigned;
      setCurrentCards(words);
      setWordSetId(id);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };

  useEffect(() => {
    fetchAssignedGame();
  }, []);

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
            Let's learn how to create clues.
          </div>
          <button onClick={advanceTutorial}>Let's go!</button>
        </div>
      ),
      2: (
        <>
          <div style={{ marginBottom: "10px" }}>
            You are given <strong>5 words</strong>. Pick one as the{" "}
            <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong>.
          </div>
        </>
      ),
      3: (
        <>
          <div style={{ marginBottom: "10px" }}>
            The other 4 words become{" "}
            <strong style={{ color: "var(--insetcolor)" }}>Matches</strong>.
            Create a clue that connects them, but <strong>not</strong> the
            Misfit. You have quite limited number of clues you can give, so take
            your time!
          </div>
        </>
      ),
      4: (
        <>
          <div style={{ marginBottom: "10px" }}>
            You can check how your clue is doing in the Clue History tab. Either
            somebody else already had the same clue (in which case there may
            already be some guesses), or you were the first to give it, in which
            case you will have to wait for other players to guess.
          </div>
        </>
      ),
      5: null,
    };
    setTutorialMessage(tutorialMessages[tutorialStep]);
  }, [tutorialStep, currentCards]);

  const advanceTutorial = useCallback(() => {
    if (tutorialStep === 4) {
      // Complete tutorial - jump to step 6
      setTutorialStep(5);
      let storageKey = "seenClueTutorial-" + userId;
      localStorage.setItem(storageKey, "true");
    } else {
      setTutorialStep(tutorialStep + 1);
    }
  }, [tutorialStep, userId]);

  const handleCardClick = (index: number) => {
    if (tutorialStep === 2) {
      advanceTutorial();
    }
    setSelectedCardIndex(index);
  };

  return (
    <div className="clue-giving-wrapper">
      {tutorialMessage && (
        <div className="tutorial-message">{tutorialMessage}</div>
      )}
      {tutorialStep >= 2 && currentCards && (
        <>
          {message && <div className="error-message">{message}</div>}
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
      {selectedCardIndex !== null && tutorialStep >= 3 && (
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
              <button
                className="next-game-button"
                onClick={async () => {
                  setClue("");
                  setSelectedCardIndex(null);
                  setSubmitStatus(null);
                  if (tutorialStep === 4) {
                    advanceTutorial();
                  }
                  await fetchAssignedGame();
                }}
              >
                Next Game
              </button>
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
    if (result.clueGiverAmt === 1) {
      setSubmitStatus(
        "Clue submitted! You were the first to give it for this word set."
      );
    } else {
      setSubmitStatus(
        `Clue submitted! It was the same clue given by ${result.clueGiverAmt} clue givers so far for this word set.`
      );
    }
    await loadUser();
  }
}
