import { useCallback, useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
import HelpIcon from "./HelpIcon";
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
    loadUser,
  } = useContext(UserStatsContext);
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
      await loadUser();
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchAssignedGame();
  }, []);
  useEffect(() => {
    let storageKey = "seenGuessingTutorial-" + userId;
    let forceTutorial = false;
    if (localStorage.getItem(storageKey) && !forceTutorial) {
      setTutorialStep(8);
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
            Welcome to <strong>Misfit</strong>! This quick tutorial will show you how to play.
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
            They picked one word as the <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong> and the other 4 as <strong style={{ color: "var(--insetcolor)" }}>Matches</strong>.
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
          The 5 words are shuffled, and you're shown <strong>one random card</strong>.
          <br />
          Your goal: Guess if this card is a <strong style={{ color: "var(--insetcolor)" }}>Match</strong> (fits the clue) or the <strong style={{ color: "var(--misfitcolor)" }}>Misfit</strong> (doesn't fit).
        </div>
      ),
      6: (
        <div style={{ marginBottom: "10px" }}>
          After you guess, you'll see all 5 words revealed. <strong>Your Guess Rating</strong> goes up or down based on whether you're correct. You'll also see how other players performed with each word. Try to get the highest rating possible! Good luck!
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
          "solution-words-container" + (tutorialStep === 5 ? " card-reveal" : "")
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
    buttons.push(
      <button
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
  return (
    <div
      className={
        "guessing-wrapper " +
        (solutionWords.length > 0 ? (isCorrect ? "correct" : "incorrect") : "")
      }
    >
      {tutorialMessage && (
        <div className="tutorial-message">{tutorialMessage}</div>
      )}
      {tutorialStep === 1 ? null : (
        <>
          {message && <div className="error-message">{message}</div>}
          <div className="guessing-content">
            <div className="guessing-header">
              {(tutorialStep >= 8 || solutionWords.length > 0) && (
                <div className="guess-rating-display">
                  <span className="rating-label">
                    Rating
                    <HelpIcon
                      title="How Guess Rating Works"
                      content={
                        <>
                          <p>
                            Your <strong>Guess Rating</strong> measures how well you identify the Misfit word.
                          </p>
                          <ul>
                            <li><strong>Starting Rating:</strong> 1000</li>
                            <li><strong>Correct Match:</strong> +10 base points</li>
                            <li><strong>Wrong Match:</strong> -20 base points</li>
                            <li><strong>Correct Misfit:</strong> +15 base points</li>
                            <li><strong>Wrong Misfit:</strong> -30 base points</li>
                          </ul>
                          <p>
                            <strong>Multiplier System:</strong> Lower ratings get more points for wins and lose fewer for losses. Higher ratings get fewer points for wins and lose more for losses. This helps balance the playing field!
                          </p>
                          <p>
                            <strong>Minimum Rating:</strong> 100 (you can't go below this)
                          </p>
                          <p>
                            <strong>Decay:</strong> Your rating decreases by 1 point per day if you don't play.
                          </p>
                        </>
                      }
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
            </div>
          </div>
        </>
      )}
    </div>
  );
}
