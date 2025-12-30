import { useEffect, useState } from "react";
import { api } from "../services/api";

interface Props {
  onLoginSuccess: () => void;
}

export default function GuessingTab({ userId }: { userId: string }) {
  const [currentCard, setCurrentCard] = useState<string | null>(null);
  const [currentClue, setCurrentClue] = useState<string | null>(null);
  const [gameId, setGameId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  const [solutionWords, setSolutionWords] = useState<
    { word: string; isOddOneOut: boolean }[]
  >([]);
  const fetchAssignedGame = async () => {
    try {
      const assigned = await api.assignedGuess();
      let { gameId, currentCard, currentClue } = assigned;
      setCurrentCard(currentCard);
      setCurrentClue(currentClue);
      setGameId(gameId);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchAssignedGame();
  }, []);
  let buttons = [];
  let solution = null;
  if (solutionWords.length > 0) {
    solution = (
      <div>
        Solution Words:{" "}
        {solutionWords.map((word) => (
          <div
            className={
              "solution-word " + (word.isOddOneOut ? "odd-one-out" : "")
            }
            key={word.word}
          >
            {word.word}
          </div>
        ))}
      </div>
    );
  }
  if (currentCard) {
    for (let isIn of [true, false]) {
      buttons.push(
        <button
          key={isIn ? "in" : "out"}
          onClick={async () => {
            try {
              const result = await api.submitGuess(isIn);
              setSolutionWords(result.allWords);
              setMessage(`Submitted guess: ${isIn ? "in" : "out"}`);
              setIsCorrect(result.isCorrect);
              // Clear current card and clue after submitting guess
              setCurrentCard(null);
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
  return (
    <div>
      {message && <div>{message}</div>}
      {currentCard && <div>Current Card: {currentCard}</div>}
      {currentClue && <div>Current Clue: {currentClue}</div>}
      {buttons}
      {solution}
    </div>
  );
}
