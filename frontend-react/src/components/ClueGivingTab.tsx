import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";

export default function ClueGivingTab({ userId }: { userId: string }) {
  const [currentCards, setCurrentCards] = useState<string[] | null>(null);
  const [wordSetId, setWordSetId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [clue, setClue] = useState<string>("");
  const [selectedCardIndex, setSelectedCardIndex] = useState<number | null>(
    null
  );
  const { loadUser } = useContext(UserStatsContext);
  const fetchAssignedGame = async () => {
    try {
      const assigned = await api.assignedClueGiving();
      let { id, words } = assigned;
      setCurrentCards(words);
      setWordSetId(id);
      setMessage(null);
    } catch (err: any) {
      setMessage(err.message);
    }
  };
  useEffect(() => {
    fetchAssignedGame();
  }, []);
  let buttons = [];
  return (
    <div>
      {message && <div>{message}</div>}
      {currentCards && (
        <div>
          Select the Odd Word Out and a clue connecting the green words:{" "}
          {currentCards.map((word, index) => (
            <div
              key={index}
              onClick={() => setSelectedCardIndex(index)}
              className={
                "clue-card" + (selectedCardIndex === index ? " selected" : "")
              }
            >
              {word}
            </div>
          ))}
        </div>
      )}
      <input
        type="text"
        placeholder="Enter your clue here"
        value={clue}
        onChange={(e) => setClue(e.target.value)}
      />
      <button
        onClick={async () => {
          await api.submitClue(
            clue,
            wordSetId!,
            currentCards![selectedCardIndex!]
          );
          setClue("");
          setSelectedCardIndex(null);
          fetchAssignedGame();
          await loadUser();
        }}
      >
        Submit Clue
      </button>
    </div>
  );
}
