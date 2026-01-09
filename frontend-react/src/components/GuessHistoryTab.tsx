import { useCallback, useContext, useEffect, useRef, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
import HelpIcon from "./HelpIcon";
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
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [totalPages, setTotalPages] = useState(1);
  const observerTarget = useRef<HTMLDivElement>(null);
  const isLoadingRef = useRef(false);
  const totalPagesRef = useRef(1);
  const { guessRating, guessRatingChange } = useContext(UserStatsContext);

  const fetchGuessHistory = useCallback(async (pageNumber: number) => {
    if (isLoadingRef.current || pageNumber > totalPagesRef.current) return;

    isLoadingRef.current = true;
    setIsLoading(true);
    try {
      const history = await api.getGuessHistory(pageNumber);
      setGuessHistory((guessHistory) => [...guessHistory, ...history.data]);
      const newTotalPages = history.totalPages || 1;
      totalPagesRef.current = newTotalPages;
      setTotalPages(newTotalPages);
      setHasMore(pageNumber < newTotalPages);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    } finally {
      isLoadingRef.current = false;
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchGuessHistory(1);
  }, [fetchGuessHistory]);

  // Intersection Observer for lazy loading
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !isLoading) {
          const nextPage = page.current + 1;
          page.current = nextPage;
          fetchGuessHistory(nextPage);
        }
      },
      { threshold: 0.1 }
    );

    const currentTarget = observerTarget.current;
    if (currentTarget) {
      observer.observe(currentTarget);
    }

    return () => {
      if (currentTarget) {
        observer.unobserve(currentTarget);
      }
    };
  }, [hasMore, isLoading, fetchGuessHistory]);

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
      {/* Rating display in history */}
      <div className="guessing-header" style={{ marginBottom: "20px" }}>
        <div className="guess-rating-display">
          <span className="rating-label">
            Rating
            <HelpIcon
              title="How Guess Rating Works"
              content={
                <>
                  <p>
                    Your <strong>Guess Rating</strong> measures how good you are
                    at identifying Misfits and Matches.
                  </p>
                  <ul>
                    <li>
                      <strong>Starting Rating:</strong> 1000
                    </li>
                    <li>
                      <strong>Correct Match:</strong> +10 base points
                    </li>
                    <li>
                      <strong>Wrong Match:</strong> -20 base points
                    </li>
                    <li>
                      <strong>Correct Misfit:</strong> +15 base points
                    </li>
                    <li>
                      <strong>Wrong Misfit:</strong> -30 base points
                    </li>
                  </ul>
                  <p>
                    <strong>Multiplier System:</strong> Lower ratings get more
                    points for wins and lose fewer for losses. Higher ratings
                    get fewer points for wins and lose more for losses. This
                    helps balance the playing field!
                  </p>
                  <p>
                    <strong>Minimum Rating:</strong> 100 (you can't go below
                    this)
                  </p>
                  <p>
                    <strong>Decay:</strong> Your rating decreases by 1 point per
                    day if you don't play.
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
      </div>
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
                  className={`history-card ${
                    isCorrect ? "correct" : "incorrect"
                  }`}
                >
                  {/* Header with time, result, and rating */}
                  <div className="history-card-header">
                    <div className="history-time">
                      {formatDateTime(entry.guessedAt)}
                    </div>
                    <div
                      className={`history-result ${
                        isCorrect ? "correct" : "incorrect"
                      }`}
                    >
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
                                </span>
                                /{word.totalGuesses}
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
          {/* Loading trigger element */}
          {hasMore && (
            <div
              ref={observerTarget}
              style={{ height: "20px", marginTop: "10px" }}
            >
              {isLoading && (
                <div style={{ textAlign: "center", color: "#666" }}>
                  Loading more...
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
