import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "../services/api";
import "./GuessingTab.css";

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

  // Format date/time nicely
  const formatDateTime = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    // For older dates, show formatted date
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
      hour: "numeric",
      minute: "2-digit",
    });
  };

  return (
    <div className="guess-history-wrapper">
      {message && <div className="history-message">{message}</div>}
      {guessHistory.length > 0 && (
        <div>
          <h3 style={{ marginBottom: "20px", textAlign: "center" }}>
            Your Guess History
          </h3>
          <div className="guess-history-container">
            {guessHistory.map((entry, index) => {
              const isCorrect =
                entry.guessIsInSet === (entry.oddOneOut !== entry.selectedCard);
              
              // Prepare words for display similar to GuessingTab
              const wordsToDisplay = entry.cardSetWords.map((card) => ({
                word: card.word,
                type:
                  card.word === entry.oddOneOut
                    ? "oddOneOut"
                    : card.word === entry.selectedCard
                    ? "inSet"
                    : "inSet",
                totalGuesses: card.guessCount,
                correctGuesses: card.correctGuesses,
                isSelected: card.word === entry.selectedCard,
              }));

              // Move selected card to middle for better visual
              const selectedIndex = wordsToDisplay.findIndex(
                (w) => w.isSelected
              );
              if (selectedIndex !== -1) {
                const [selectedWord] = wordsToDisplay.splice(selectedIndex, 1);
                const middleIndex = Math.floor(wordsToDisplay.length / 2);
                wordsToDisplay.splice(middleIndex, 0, selectedWord);
              }

              return (
                <div
                  key={index}
                  className={`history-card ${isCorrect ? "correct" : "incorrect"}`}
                >
                  {/* Header with time, result, and rating */}
                  <div className="history-card-header">
                    <div className="history-time">{formatDateTime(entry.guessedAt)}</div>
                    <div className={`history-result ${isCorrect ? "correct" : "incorrect"}`}>
                      {isCorrect ? "✓" : "✗"}
                    </div>
                    {entry.ratingChange !== 0 && (
                      <span
                        className={`history-rating-change ${
                          entry.ratingChange > 0 ? "positive" : "negative"
                        }`}
                      >
                        {entry.ratingChange > 0 ? "+" : ""}
                        {entry.ratingChange}
                      </span>
                    )}
                  </div>

                  {/* Clue - compact display */}
                  <div className="history-clue-section">
                    <div className="history-clue-text">{entry.clue}</div>
                  </div>

                  {/* Cards display - compact */}
                  <div className="history-cards-section">
                    <div className="solution-words-container">
                      {wordsToDisplay.map((word, wordIndex) => (
                        <div
                          className={`guessing-card-wrap ${word.type} ${
                            word.isSelected ? "selected-card" : ""
                          }`}
                          key={wordIndex}
                        >
                          {word.totalGuesses !== undefined &&
                            word.correctGuesses !== undefined && (
                              <div className="success-rate">
                                <span className="amt-correct">
                                  {word.correctGuesses}
                                </span>/{word.totalGuesses}
                              </div>
                            )}
                          <div
                            className={`guessing-card ${word.type} ${
                              word.isSelected ? "selected" : ""
                            }`}
                            title={word.isSelected ? "Your guess" : ""}
                          >
                            {word.word}
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
