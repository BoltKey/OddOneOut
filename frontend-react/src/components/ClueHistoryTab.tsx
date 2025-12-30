import { useEffect, useState } from "react";
import { api } from "../services/api";

interface Props {
  onLoginSuccess: () => void;
}
export default function ClueHistoryTab({ userId }: { userId: string }) {
  const [clueHistory, setClueHistory] = useState<
    { gameId: string; clue: string; timestamp: string }[]
  >([]);
  const [message, setMessage] = useState<string | null>(null);
  const fetchClueHistory = async () => {
    try {
      const history = await api.getClueHistory(1);
      setClueHistory(history);
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
          <ul>
            {clueHistory.map((entry, index) => (
              <li key={index}>
                Game ID: {entry.gameId} - Clue: "{entry.clue}" -{" "}
                {new Date(entry.timestamp).toLocaleString()}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
