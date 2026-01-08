import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "../services/api";
import "./GuessingTab.css";

export default function ClueHistoryTab({ userId }: { userId: string }) {
  const [clueHistory, setClueHistory] = useState<
    {
      game: string;
      cardSetId: string;
      oddOneOut: string;
      cardSetWords: {
        word: string;
        guessCount: number;
        correctGuesses: number;
      }[];
      createdAt: string;
      clue: string;
      gameScore: number | null;
      successCoef: number | null;
      otherClues?: {
        clue: string;
        oddOneOut: string;
        createdAt: string;
        gameScore: number;
      }[];
    }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const [clueRating, setClueRating] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [totalPages, setTotalPages] = useState(1);
  const page = useRef<number>(1);
  const observerTarget = useRef<HTMLDivElement>(null);
  const isLoadingRef = useRef(false);
  const totalPagesRef = useRef(1);

  const fetchClueHistory = useCallback(async (pageNumber: number) => {
    if (isLoadingRef.current || pageNumber > totalPagesRef.current) return;
    
    isLoadingRef.current = true;
    setIsLoading(true);
    try {
      const history = await api.getClueHistory(pageNumber);
      setClueHistory((clueHistory) => [...clueHistory, ...history.data]);
      setClueRating(history.clueRating);
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
    fetchClueHistory(1);
  }, [fetchClueHistory]);

  // Intersection Observer for lazy loading
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !isLoading) {
          const nextPage = page.current + 1;
          page.current = nextPage;
          fetchClueHistory(nextPage);
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
  }, [hasMore, isLoading, fetchClueHistory]);

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
    <div className="clue-history-wrapper">
      {message && <div className="history-message">{message}</div>}
      {clueHistory.length > 0 && (
        <div>
          <h3 style={{ marginBottom: "10px", textAlign: "center" }}>
            Your Clue History
          </h3>
          <div
            style={{
              textAlign: "center",
              marginBottom: "15px",
              fontSize: "0.9rem",
            }}
          >
            Your Clue Rating:{" "}
            <strong>
              {clueRating !== null ? clueRating.toFixed(1) : "N/A"}
            </strong>
          </div>
          <div className="clue-history-container">
            {clueHistory.map((entry, index) => {
              // Prepare words for display
              const wordsToDisplay = entry.cardSetWords.map((card) => ({
                word: card.word,
                type: card.word === entry.oddOneOut ? "oddOneOut" : "inSet",
                totalGuesses: card.guessCount,
                correctGuesses: card.correctGuesses,
              }));

              return (
                <div key={index} className="clue-history-card">
                  {/* Header */}
                  <div className="clue-history-header">
                    <div className="history-time">
                      {formatDateTime(entry.createdAt)}
                    </div>
                    {entry.gameScore !== null && (
                      <div className="clue-game-score">
                        Score:{" "}
                        <strong>{entry.gameScore?.toFixed(1) ?? "N/A"}</strong>
                      </div>
                    )}
                  </div>

                  {/* Your Clue - prominent */}
                  <div className="clue-history-your-clue">
                    <div className="clue-history-your-clue-label">
                      Your Clue
                    </div>
                    <div className="history-clue-text">{entry.clue}</div>
                    <div className="clue-odd-one-out">
                      Misfit: <strong>{entry.oddOneOut}</strong>
                    </div>
                  </div>

                  {/* Other Clues on Same Card Set */}
                  {entry.otherClues && entry.otherClues.length > 0 && (
                    <div className="clue-history-other-clues">
                      <div className="other-clues-label">
                        Other Clues ({entry.otherClues.length})
                      </div>
                      <div className="other-clues-list">
                        {entry.otherClues.map((otherClue, clueIndex) => (
                          <div key={clueIndex} className="other-clue-item">
                            <div className="other-clue-text">
                              {otherClue.clue}
                            </div>
                            <div className="other-clue-details">
                              <span className="other-clue-misfit">
                                Misfit: {otherClue.oddOneOut}
                              </span>
                              {otherClue.gameScore !== null && (
                                <span className="other-clue-score">
                                  Score: {otherClue.gameScore.toFixed(1)}
                                </span>
                              )}
                              <span className="other-clue-time">
                                {formatDateTime(otherClue.createdAt)}
                              </span>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {/* Cards display */}
                  <div className="history-cards-section">
                    <div className="solution-words-container">
                      {wordsToDisplay.map((word, wordIndex) => (
                        <div
                          className={`guessing-card-wrap ${word.type}`}
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
                          <div className={`guessing-card ${word.type}`}>
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
            <div ref={observerTarget} style={{ height: "20px", marginTop: "10px" }}>
              {isLoading && (
                <div style={{ textAlign: "center", color: "#666" }}>Loading more...</div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
