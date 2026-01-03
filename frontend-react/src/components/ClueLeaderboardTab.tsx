import { useEffect, useState } from "react";
import { api } from "../services/api";

export default function ClueLeaderboardTab({ userId }: { userId: string }) {
  const [leaderboard, setLeaderboard] = useState<
    { userName: string; clueRating: number; rank: number }[]
  >([]);
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
  return (
    <div>
      {message && <div>{message}</div>}
      {leaderboard.length > 0 && (
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
              {leaderboard.map((entry, index) => (
                <tr key={index} className="clue-leaderboard-entry">
                  <td>{index + 1}</td>
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
