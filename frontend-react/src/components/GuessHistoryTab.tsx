import { useEffect, useState } from "react";
import { api } from "../services/api";

interface Props {
  onLoginSuccess: () => void;
}
export default function GuessHistoryTab({ userId }: { userId: string }) {
  const [guessHistory, setGuessHistory] = useState<
    {
      game: string;
      oddOneOut: string;
      cardSetWords: {
        word: string;
        guessCount: number;
        correctGuesses: number;
      }[];
      guessedAt: string;
      guessIsInSet: boolean;
      selectedCard: string;
      clue: string;
    }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const fetchGuessHistory = async () => {
    try {
      const history = await api.getGuessHistory(1);
      setGuessHistory(history.data);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchGuessHistory();
  }, []);
  return (
    <div>
      {message && <div>{message}</div>}
      {guessHistory.length > 0 && (
        <div>
          <h3>Your Guess History:</h3>
          <div className="guess-history-container">
            {guessHistory.map((entry, index) => (
              <div
                key={index}
                className={
                  "guess-history-entry " +
                  (entry.guessIsInSet ===
                  (entry.oddOneOut !== entry.selectedCard)
                    ? "correct-guess"
                    : "incorrect-guess")
                }
              >
                {entry.guessedAt}
                {entry.clue && <div>Clue: "{entry.clue}"</div>}
                {entry.cardSetWords.map((card) => (
                  <div
                    key={card.word}
                    className={
                      "history-word-container " +
                      "guess-word " +
                      (card.word === entry.oddOneOut ? "odd-one-out " : "") +
                      (card.word === entry.selectedCard ? "selected-card" : "")
                    }
                  >
                    {card.word} {card.correctGuesses}/{card.guessCount}
                  </div>
                ))}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
