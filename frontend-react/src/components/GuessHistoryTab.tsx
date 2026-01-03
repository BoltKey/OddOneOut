import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "../services/api";

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
      gameScore: number | null;
      successCoef: number | null;
      ratingChange: number;
    }[]
  >([]);
  const page = useRef<number>(1);
  const [message, setMessage] = useState<string | null>(null);
  const fetchGuessHistory = async (pageNumber: number) => {
    try {
      const history = await api.getGuessHistory(pageNumber);
      setGuessHistory((guessHistory) => [...guessHistory, ...history.data]);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchGuessHistory(1);
    let intervalId = setInterval(() => {
      checkScrollAtBottom();
    }, 1000);
    return () => clearInterval(intervalId);
  }, []);
  // check if scroll is at bottom to load more on interval

  const checkScrollAtBottom = useCallback(() => {
    const container = document.querySelector(".app-container");
    if (container) {
      if (
        container.scrollHeight - document.querySelector("html")!.scrollTop <=
        window.innerHeight + 100
      ) {
        fetchGuessHistory(page.current + 1);
        page.current = page.current + 1;
      }
    }
  }, [page]);
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
                {entry.ratingChange !== 0 && (
                  <div>
                    {entry.ratingChange < 0
                      ? "-" + entry.ratingChange
                      : `+${entry.ratingChange}`}
                  </div>
                )}
                {entry.gameScore !== null && (
                  <div>
                    Game Score: {entry.gameScore} | Success Coef:{" "}
                    {entry.successCoef !== null ? entry.successCoef : "N/A"}
                  </div>
                )}
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
