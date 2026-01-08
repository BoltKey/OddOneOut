import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";
import { FaTrophy } from "react-icons/fa";
import "./LeaderboardTab.css";

export default function ClueLeaderboardTab() {
  const [leaderboard, setLeaderboard] = useState<
    { id?: string; userName: string; clueRating: number; rank: number }[]
  >([]);
  const { user } = useContext(UserStatsContext);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchClueLeaderboard = async () => {
    setLoading(true);
    try {
      const board = await api.getClueLeaderboard();
      setLeaderboard(board);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchClueLeaderboard();
  }, []);

  const getRankIcon = (rank: number) => {
    if (rank === 1) return <FaTrophy style={{ color: "#FFD700" }} />;
    if (rank === 2) return <FaTrophy style={{ color: "#C0C0C0" }} />;
    if (rank === 3) return <FaTrophy style={{ color: "#CD7F32" }} />;
    return null;
  };

  let completeLeaderboard = leaderboard ?? [];
  // Only add current user if they're not already in the leaderboard
  // Compare by ID if available, otherwise by displayName (which backend uses) or userName
  if (user && user.clueRank) {
    const userAlreadyInLeaderboard = leaderboard.some(
      (entry) => 
        (entry.id && entry.id === user.id) ||
        entry.userName === user.displayName ||
        entry.userName === user.userName
    );
    if (!userAlreadyInLeaderboard && user.clueRank > leaderboard.length) {
      completeLeaderboard = [
        ...leaderboard,
        {
          id: user.id,
          userName: user.displayName ?? user.userName ?? "You",
          clueRating: user.clueRating ?? 0,
          rank: user.clueRank ?? -1,
        },
      ];
    }
  }

  return (
    <div className="leaderboard-wrapper">
      <h2 className="leaderboard-title">Clue Leaderboard</h2>
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
              {completeLeaderboard.map((entry, index) => {
                const isCurrentUser = 
                  (entry.id && entry.id === user?.id) ||
                  entry.userName === user?.displayName ||
                  entry.userName === user?.userName;
                return (
                  <tr
                    key={entry.id || `${entry.userName}-${entry.rank}-${index}`}
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
                    <td className="rating-cell">{entry.clueRating}</td>
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
