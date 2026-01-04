import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";

export default function ClueGivingTab({ userId }: { userId: string }) {
  const [currentCards, setCurrentCards] = useState<string[] | null>(null);
  const [wordSetId, setWordSetId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [clue, setClue] = useState<string>("");
  const [submitted, setSubmitted] = useState<boolean>(false);
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
          Select the Misfit and a clue connecting the Matches:{" "}
          {currentCards.map((word, index) => (
            <div
              key={index}
              onClick={() => setSelectedCardIndex(index)}
              className={
                "clue-card" +
                (selectedCardIndex === index
                  ? " selected"
                  : selectedCardIndex === null
                  ? ""
                  : " matching")
              }
            >
              {word}
            </div>
          ))}
        </div>
      )}
      {selectedCardIndex !== null && (
        <>
          Your clue:{" "}
          <input
            type="text"
            placeholder="Must be English word"
            value={clue}
            onChange={(e) => setClue(e.target.value)}
          />
          {submitted ? (
            <div>Clue submitted! Waiting for guessers...</div>
          ) : (
            <>
              <button
                onClick={async () => {
                  let result = await api.submitClue(
                    clue,
                    wordSetId!,
                    currentCards![selectedCardIndex!]
                  );
                  setSubmitted(true);
                  await loadUser();
                }}
                style={{
                  margin: "10px auto",
                  display: "block",
                }}
              >
                Submit Clue
              </button>
            </>
          )}
        </>
      )}
    </div>
  );
}
