import { useEffect, useState } from "react";
import { api } from "../services/api";

export default function GuessLeaderboardTab({ userId }: { userId: string }) {
  const [leaderboard, setLeaderboard] = useState<
    { userName: string; guessRating: number; rank: number }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const fetchGuessLeaderboard = async () => {
    try {
      const board = await api.getGuessLeaderboard();
      setLeaderboard(board);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchGuessLeaderboard();
  }, []);
  return (
    <div>
      {message && <div>{message}</div>}
      {leaderboard.length > 0 && (
        <div>
          <h3>Guess Leaderboard:</h3>
          <table className="guess-leaderboard-table leaderboard-table">
            <thead>
              <tr>
                <th>Rank</th>
                <th>Player</th>
                <th>Rating</th>
              </tr>
            </thead>
            <tbody>
              {leaderboard.map((entry, index) => (
                <tr key={index} className="guess-leaderboard-entry">
                  <td>{entry.rank}</td>
                  <td>{entry.userName}</td>
                  <td>{entry.guessRating}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
