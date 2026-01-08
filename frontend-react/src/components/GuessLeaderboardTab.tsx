import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
import { FaTrophy } from "react-icons/fa";
import "./LeaderboardTab.css";

export default function GuessLeaderboardTab() {
  const [leaderboard, setLeaderboard] = useState<
    { userName: string; guessRating: number; rank: number }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const { user } = useContext(UserStatsContext);

  const fetchGuessLeaderboard = async () => {
    setLoading(true);
    try {
      const board = await api.getGuessLeaderboard();
      setLeaderboard(board);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGuessLeaderboard();
  }, []);

  const getRankIcon = (rank: number) => {
    if (rank === 1) return <FaTrophy style={{ color: "#FFD700" }} />;
    if (rank === 2) return <FaTrophy style={{ color: "#C0C0C0" }} />;
    if (rank === 3) return <FaTrophy style={{ color: "#CD7F32" }} />;
    return null;
  };

  let completeLeaderboard = leaderboard ?? [];
  if (user && user.guessRank && user.guessRank > leaderboard.length) {
    completeLeaderboard = [
      ...leaderboard,
      {
        userName: user.userName ?? "You",
        guessRating: user.guessRating ?? 0,
        rank: user.guessRank ?? -1,
      },
    ];
  }

  return (
    <div className="leaderboard-wrapper">
      <h2 className="leaderboard-title">Guess Leaderboard</h2>
      {message && <div className="leaderboard-error">{message}</div>}
      {loading ? (
        <div className="leaderboard-loading">Loading...</div>
      ) : completeLeaderboard.length > 0 ? (
        <div className="leaderboard-container">
          <table className="leaderboard-table">
            <thead>
              <tr>
                <th className="rank-col">Rank</th>
                <th className="player-col">Player</th>
                <th className="rating-col">Rating</th>
              </tr>
            </thead>
            <tbody>
              {completeLeaderboard.map((entry) => {
                const isCurrentUser = entry.userName === user?.userName;
                return (
                  <tr
                    key={entry.rank}
                    className={`leaderboard-row ${
                      isCurrentUser ? "current-user" : ""
                    } ${entry.rank <= 3 ? "top-three" : ""}`}
                  >
                    <td className="rank-cell">
                      <span className="rank-number">
                        {getRankIcon(entry.rank)}
                        {entry.rank}
                      </span>
                    </td>
                    <td className="player-cell">
                      {entry.userName}
                    </td>
                    <td className="rating-cell">{entry.guessRating}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="leaderboard-empty">No players yet. Be the first!</div>
      )}
    </div>
  );
}
