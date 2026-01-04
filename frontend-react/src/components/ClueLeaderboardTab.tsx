import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";

export default function ClueLeaderboardTab({ userId }: { userId: string }) {
  const [leaderboard, setLeaderboard] = useState<
    { userName: string; clueRating: number; rank: number }[]
  >([]);
  const { user } = useContext(UserStatsContext);
  const [message, setMessage] = useState<string | null>(null);
  const fetchClueLeaderboard = async () => {
    try {
      const board = await api.getClueLeaderboard();
      setLeaderboard(board);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchClueLeaderboard();
  }, []);
  let completeLeaderboard = leaderboard ?? [];
  if (user && user.clueRank && user.clueRank > leaderboard.length) {
    completeLeaderboard = [
      ...leaderboard,
      {
        userName: user.userName ?? "You",
        clueRating: user.clueRating,
        rank: user.clueRank ?? -1,
      },
    ];
  }
  return (
    <div>
      {message && <div>{message}</div>}
      {completeLeaderboard.length > 0 && (
        <div>
          <h3>Clue Leaderboard:</h3>
          <table className="clue-leaderboard-table leaderboard-table">
            <thead>
              <tr>
                <th>Rank</th>
                <th>Player</th>
                <th>Rating</th>
              </tr>
            </thead>
            <tbody>
              {completeLeaderboard.map((entry) => (
                <tr
                  key={entry.rank}
                  className={
                    "clue-leaderboard-entry" +
                    (entry.userName === user?.userName ? " current-user" : "")
                  }
                >
                  <td>{entry.rank}</td>
                  <td>{entry.userName}</td>
                  <td>{entry.clueRating}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
