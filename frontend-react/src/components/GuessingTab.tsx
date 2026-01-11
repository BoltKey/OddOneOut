import { useCallback, useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext, useCountdown } from "../App";
import HelpIcon from "./HelpIcon";
import { GuessRatingHelpContent } from "./HelpContent";
import { TbReportSearch } from "react-icons/tb";
import { BiSolidMessageRounded } from "react-icons/bi";
import "./GuessingTab.css";

export default function GuessingTab({ userId }: { userId: string }) {
  const [currentCard, setCurrentCard] = useState<string>("");
  const [currentClue, setCurrentClue] = useState<string | null>(null);
  const [gameId, setGameId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  const [tutorialStep, setTutorialStep] = useState<number>(0);
  const [tutorialMessage, setTutorialMessage] =
    useState<React.ReactNode | null>(null);
  // guess rating from context
  const {
    guessRating,
    setGuessRating,
    guessRatingChange,
    setGuessRatingChange,
    guessEnergy,
    nextGuessRegenTime,
    loadUser,
    navigateToTab,
    openModal,
    user,
  } = useContext(UserStatsContext);
  const [outOfGuesses, setOutOfGuesses] = useState(false);
  
  // Track previous guessEnergy to detect when it increases
  const [prevGuessEnergy, setPrevGuessEnergy] = useState<number | null>(null);

  // Countdown hook for when out of guesses - must be called at top level
  const countdownText = useCountdown(
    guessEnergy === 0 ? nextGuessRegenTime : null,
    () => {
      loadUser();
    }
  );

  const isWaitingForEnergy = guessEnergy === 0 && nextGuessRegenTime;

  const [solutionWords, setSolutionWords] = useState<
    {
      word: string;
      type: "oddOneOut" | "inSet" | "dontknow" | "othercard";
      totalGuesses?: number;
      correctGuesses?: number;
    }[]
  >([]);
  const fetchAssignedGame = async () => {
    try {
      const assigned = await api.assignedGuess();
      let { gameId, currentCard, currentClue } = assigned;
      setCurrentCard(currentCard);
      setCurrentClue(currentClue);
      setGameId(gameId);
      setSolutionWords([]);
      setIsCorrect(null);
      setMessage(null);
      setOutOfGuesses(false);
      await loadUser();
    } catch (err: any) {
      // Check if user is out of guesses
      const errorMsg = err.message?.toLowerCase() || "";
      if (errorMsg.includes("no guesses") || errorMsg.includes("out of") || errorMsg.includes("energy")) {
        setOutOfGuesses(true);
        setMessage(null);
      } else {
        setMessage(err.message);
        setOutOfGuesses(false);
      }
      await loadUser();
    }
  };
  useEffect(() => {
    fetchAssignedGame();
  }, []);
  
  // When countdown expires and guessEnergy increases, auto-fetch new game
  useEffect(() => {
    if (prevGuessEnergy === 0 && guessEnergy !== null && guessEnergy > 0 && outOfGuesses) {
      fetchAssignedGame();
    }
    setPrevGuessEnergy(guessEnergy);
  }, [guessEnergy, outOfGuesses]);
  
  useEffect(() => {
    let storageKey = "seenGuessingTutorial-" + userId;
    let forceTutorial = false;
    if (localStorage.getItem(storageKey) && !forceTutorial) {
      setTutorialStep(7);
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
            Welcome to <strong>Misfit</strong>! This quick tutorial will show
            you how to play.
          </div>
          <button onClick={advanceTutorial}>Let's go!</button>
        </div>
      ),
      2: (
        <>
          <div style={{ marginBottom: "10px" }}>
            Another player was given <strong>5 secret words</strong>.
          </div>
          <button onClick={advanceTutorial}>Got it</button>
        </>
      ),
      3: (
        <>
          <div style={{ marginBottom: "10px" }}>
            They picked one word as the{" "}
            <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong> and
            the other 4 as{" "}
            <strong style={{ color: "var(--insetcolor)" }}>Matches</strong>.
          </div>
          <button onClick={advanceTutorial}>I see</button>
        </>
      ),
      4: (
        <>
          <div style={{ marginBottom: "10px" }}>
            They created a clue: <strong>"{currentClue}"</strong>
            <br />
            <small style={{ fontSize: "0.9em", color: "#666" }}>
              This clue connects the 4 Matches, but not the Misfit.
            </small>
          </div>
          <button onClick={advanceTutorial}>Understood</button>
        </>
      ),
      5: (
        <div style={{ marginBottom: "10px" }}>
          The 5 words are shuffled, and you're shown{" "}
          <strong>one random card</strong>.
          <br />
          Your goal: Guess if this card is a{" "}
          <strong style={{ color: "var(--insetcolor)" }}>Match</strong> (fits
          the clue) or the{" "}
          <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong>{" "}
          (doesn't fit).
        </div>
      ),
      6: (
        <div style={{ marginBottom: "10px" }}>
          After you guess, you'll see all 5 words revealed.{" "}
          <strong>Your Guess Rating</strong> goes up or down based on whether
          you're correct. You'll also see how other players performed with each
          word. Try to get the highest rating possible! Good luck!
        </div>
      ),
      7: null,
    };
    setTutorialMessage(tutorialMessages[tutorialStep]);
  }, [tutorialStep]);
  const advanceTutorial = useCallback(() => {
    setTutorialStep(tutorialStep + 1);
    if (tutorialStep === 6) {
      let storageKey = "seenGuessingTutorial-" + userId;
      localStorage.setItem(storageKey, "true");
    }
  }, [tutorialStep]);
  let buttons = [];
  let cardDisplay = null;
  let wordsToDisplay = solutionWords;
  if (wordsToDisplay.length > 0) {
    // move current card to middle
    const currentCardIndex = wordsToDisplay.findIndex(
      (w) => w.word === currentCard
    );
    if (currentCardIndex !== -1) {
      const [currentCardWord] = wordsToDisplay.splice(currentCardIndex, 1);
      const middleIndex = Math.floor(wordsToDisplay.length / 2);
      wordsToDisplay.splice(middleIndex, 0, currentCardWord);
    }
  } else if (currentCard !== "") {
    wordsToDisplay = [
      { word: "?", type: "othercard" },
      { word: "?", type: "othercard" },
      { word: currentCard, type: "dontknow" },
      { word: "?", type: "othercard" },
      { word: "?", type: "othercard" },
    ];
    if (tutorialStep === 2) {
      wordsToDisplay = [
        { word: "???", type: "dontknow" },
        { word: "???", type: "dontknow" },
        { word: "???", type: "dontknow" },
        { word: "???", type: "dontknow" },
        { word: "???", type: "dontknow" },
      ];
    }
    if ([3, 4].includes(tutorialStep)) {
      wordsToDisplay = [
        { word: "Match", type: "inSet" },
        { word: "Misfit", type: "oddOneOut" },
        { word: "Match", type: "inSet" },
        { word: "Match", type: "inSet" },
        { word: "Match", type: "inSet" },
      ];
    }
    console.log("Displaying current card only:", wordsToDisplay);
  }
  cardDisplay = (
    <>
      {wordsToDisplay.length > 0 &&
        solutionWords.length === 0 &&
        tutorialStep >= 5 && <div className="your-card">Your word:</div>}
      <div
        className={
          "solution-words-container" +
          (tutorialStep === 5 ? " card-reveal" : "")
        }
      >
        {wordsToDisplay.map((word, index) => (
          <div className={"guessing-card-wrap " + word.type} key={index}>
            {word.totalGuesses !== undefined &&
              word.correctGuesses !== undefined && (
                <div className="success-rate">
                  <span className="amt-correct">{word.correctGuesses}</span> /{" "}
                  {word.totalGuesses}
                </div>
              )}
            <div className={"guessing-card " + word.type} key={index}>
              {word.word}
            </div>
          </div>
        ))}
      </div>
    </>
  );
  if (solutionWords.length === 0 && currentCard !== "") {
    for (let isIn of [true, false]) {
      buttons.push(
        <button
          key={isIn ? "in" : "out"}
          onClick={async () => {
            try {
              const result = await api.submitGuess(isIn);
              setSolutionWords(
                result.allWords.map((w: any) => ({
                  word: w.word,
                  type: w.isOddOneOut ? "oddOneOut" : "inSet",
                  totalGuesses: w.totalGuesses,
                  correctGuesses: w.correctGuesses,
                }))
              );
              if (tutorialStep === 5) {
                advanceTutorial();
              }
              setIsCorrect(result.isCorrect);
              setGuessRating(result.newRating);
              setGuessRatingChange(result.ratingChange);
              await loadUser();
            } catch (err: any) {
              setMessage(err.message);
            }
          }}
          className={isIn ? "button-related" : "button-odd-one-out"}
        >
          {isIn ? "Match" : "Misfit"}
        </button>
      );
    }
  }
  if (solutionWords.length > 0) {
    if (isWaitingForEnergy) {
      // When out of guesses, show "Done" button that leads to out-of-guesses screen
      buttons.push(
        <button
          key="done"
          onClick={() => {
            advanceTutorial();
            setOutOfGuesses(true);
            setSolutionWords([]);
            setCurrentCard("");
            setCurrentClue(null);
          }}
          className="button-done"
        >
          Done
        </button>
      );
    } else {
      buttons.push(
        <button
          key="next-game"
          onClick={() => {
            advanceTutorial();
            fetchAssignedGame();
            loadUser();
          }}
        >
          Next game
        </button>
      );
    }
  }
  return (
    <div
      className={
        "guessing-wrapper " +
        (solutionWords.length > 0 ? (isCorrect ? "correct" : "incorrect") : "")
      }
    >
      {tutorialMessage && tutorialStep < 6 && (
        <div className="tutorial-message">{tutorialMessage}</div>
      )}
      {tutorialStep === 1 ? null : (
        <>
          {message && !outOfGuesses && <div className="error-message">{message}</div>}
          {outOfGuesses && (
            <div className="out-of-guesses-screen">
              <div className="out-of-guesses-icon">✅</div>
              <h2 className="out-of-guesses-title">Guessing done</h2>
              <div className="out-of-guesses-stats">
                {nextGuessRegenTime && (
                  <div className="out-of-guesses-countdown">
                    <span className="countdown-label">Next guess in:</span>
                    <span className="countdown-value">{countdownText}</span>
                  </div>
                )}
                <div className="out-of-guesses-rating">
                  <span className="rating-label">Your Rating</span>
                  <span className="rating-value">{guessRating}</span>
                </div>
              </div>
              <div className="out-of-guesses-actions">
                <p className="actions-label">In the meantime...</p>
                <button
                  className="action-button action-history"
                  onClick={() => openModal("guessHistory")}
                >
                  <TbReportSearch /> View Guess History
                </button>
                {user?.canGiveClues && (
                  <button
                    className="action-button action-clues"
                    onClick={() => navigateToTab("clueGiving")}
                  >
                    <BiSolidMessageRounded /> Give Clues
                  </button>
                )}
              </div>
            </div>
          )}
          {!outOfGuesses && <div className="guessing-content">
            <div className="guessing-header">
              {tutorialStep >= 6 && (
                <div className="guess-rating-display">
                  <span className="rating-label">
                    Rating
                    <HelpIcon
                      title="How Guess Rating Works"
                      content={GuessRatingHelpContent}
                    />
                  </span>
                  <span className="rating-value">{guessRating}</span>
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
                      {guessRatingChange >= 0 ? "+" : ""}
                      {guessRatingChange}
                    </span>
                  )}
                </div>
              )}
            </div>
            <div className="guessing-main">
              {currentClue && tutorialStep >= 4 && (
                <div className="clue-section">
                  <div className="clue-label">Clue</div>
                  <div className="current-clue">{currentClue}</div>
                </div>
              )}
              <div
                className={
                  "clue-word-separator " +
                  (tutorialStep === 4 ? "visible" : "hidden")
                }
              ></div>
              {solutionWords.length > 0 && (
                <div className="guess-result">
                  {isCorrect ? "Correct!" : "Incorrect"}
                </div>
              )}
              {cardDisplay}
              {tutorialStep >= 5 && (
                <div className="guess-buttons-wrapper">{buttons}</div>
              )}
              {tutorialMessage && tutorialStep >= 6 && (
                <div className="tutorial-message">{tutorialMessage}</div>
              )}
            </div>
          </div>}
        </>
      )}
    </div>
  );
}
