import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";

interface Props {
  onLoginSuccess: () => void;
}

export default function GuessingTab({ userId }: { userId: string }) {
  const [currentCard, setCurrentCard] = useState<string>("");
  const [currentClue, setCurrentClue] = useState<string | null>(null);
  const [gameId, setGameId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  // guess rating from context
  const {
    guessRating,
    setGuessRating,
    guessRatingChange,
    setGuessRatingChange,
  } = useContext(UserStatsContext);
  const [solutionWords, setSolutionWords] = useState<
    { word: string; type: "oddOneOut" | "inSet" | "dontknow" | "othercard" }[]
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
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchAssignedGame();
  }, []);
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
    console.log("Displaying current card only:", wordsToDisplay);
  }
  cardDisplay = (
    <>
      {wordsToDisplay.length > 0 && solutionWords.length === 0 && (
        <div className="your-card">Your word:</div>
      )}
      <div className="solution-words-container">
        {wordsToDisplay.map((word, index) => (
          <div className={"guessing-card " + word.type} key={index}>
            {word.word}
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
                }))
              );
              setIsCorrect(result.isCorrect);
              setGuessRating(result.newRating);
              setGuessRatingChange(result.ratingChange);
            } catch (err: any) {
              setMessage(err.message);
            }
          }}
          className={isIn ? "button-related" : "button-odd-one-out"}
        >
          {isIn ? "Related" : "Odd One Out"}
        </button>
      );
    }
  }
  if (solutionWords.length > 0) {
    buttons.push(
      <button
        onClick={async () => {
          fetchAssignedGame();
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
      {message && <div>{message}</div>}
      {currentClue && (
        <>
          <div className="clue-wrapper">Clue: </div>
          <div className="current-clue">{currentClue}</div>
        </>
      )}
      {solutionWords.length > 0 && (
        <div className="guess-result">
          {isCorrect ? "Correct!" : "Incorrect"}
        </div>
      )}
      {cardDisplay}
      {buttons}
    </div>
  );
}
