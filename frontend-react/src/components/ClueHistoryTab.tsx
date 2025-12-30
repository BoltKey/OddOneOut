import { useEffect, useState } from "react";
import { api } from "../services/api";

interface Props {
  onLoginSuccess: () => void;
}
export default function ClueHistoryTab({ userId }: { userId: string }) {
  const [clueHistory, setClueHistory] = useState<
    {
      game: string;
      oddOneOut: string;
      cardSetWords: {
        word: string;
        guessCount: number;
        correctGuesses: number;
      }[];
      createdAt: string;
      clue: string;
    }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const fetchClueHistory = async () => {
    try {
      const history = await api.getClueHistory(1);
      setClueHistory(history.data);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchClueHistory();
  }, []);
  return (
    <div>
      {message && <div>{message}</div>}
      {clueHistory.length > 0 && (
        <div>
          <h3>Your Clue History:</h3>
          <div className="clue-history-container">
            {clueHistory.map((entry, index) => (
              <div key={index} className={"clue-history-entry"}>
                {entry.createdAt}
                {entry.clue && <div>Clue: "{entry.clue}"</div>}
                {entry.cardSetWords.map((card) => (
                  <div
                    key={card.word}
                    className={
                      "history-word-container " +
                      "guess-word " +
                      (card.word === entry.oddOneOut ? "odd-one-out " : "")
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
