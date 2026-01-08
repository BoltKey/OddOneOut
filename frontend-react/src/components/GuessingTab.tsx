import { useCallback, useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
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
          Welcome to word game Misfit! This 1-minute tutorial will teach you how
          to play. <button onClick={advanceTutorial}>Sounds good</button>
        </div>
      ),
      2: (
        <>
          <div>Another player has been given 5 secret random words.</div>
          <button onClick={advanceTutorial}>Understood</button>
        </>
      ),
      3: (
        <>
          <div>
            They chose one of them to be the Misfit, the other 4 being Matches.
          </div>
          <button onClick={advanceTutorial}>Ok</button>
        </>
      ),
      4: (
        <>
          <div>
            They said clue "{currentClue}", connecting all words but the Misfit.
          </div>
          <button onClick={advanceTutorial}>Understood</button>
        </>
      ),
      5: "All 5 words are then shuffled and you see one of those cards. Your job is to guess whether this card is part of the group that matches the clue, or the misfit.",
      6: "After guessing, you will see the original set of 5 words. You gain or lose Guess Rating based on whether your guess is correct. You can also see success rates of all other players who were given other words. Try to get as high a Guess Rating as possible! Good Luck!",
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
          "solution-words-container" + (tutorialStep === 5 ? " shuffling" : "")
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
          {message && <div>{message}</div>}
          {tutorialStep >= 4 && (
            <div className="guess-rating-display">
              Your Guess Rating: {guessRating}{" "}
              <span>
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
                    {`(${
                      guessRatingChange >= 0 ? "+" : ""
                    }${guessRatingChange})`}
                  </span>
                )}
              </span>
            </div>
          )}
          {currentClue && tutorialStep >= 4 && (
            <>
              <div className="clue-wrapper">Clue: </div>
              <div className="current-clue">{currentClue}</div>
            </>
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
        </>
      )}
    </div>
  );
}
