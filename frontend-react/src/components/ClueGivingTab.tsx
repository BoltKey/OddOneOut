import { useContext, useEffect, useState } from "react";
import { api } from "../services/api";
import { UserStatsContext } from "../App";

export default function ClueGivingTab({ userId }: { userId: string }) {
  const [currentCards, setCurrentCards] = useState<string[] | null>(null);
  const [wordSetId, setWordSetId] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [clue, setClue] = useState<string>("");
  const [submitStatus, setSubmitStatus] = useState<string | null>(null);
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
          {submitStatus ? (
            <>
              <div>{submitStatus}</div>
              <button
                onClick={async () => {
                  setClue("");
                  setSelectedCardIndex(null);
                  setSubmitStatus(null);
                  await fetchAssignedGame();
                }}
              >
                Go next
              </button>
            </>
          ) : (
            <>
              <button
                onClick={async () => {
                  setMessage(null);
                  let result;
                  try {
                    result = await api.submitClue(
                      clue,
                      wordSetId!,
                      currentCards![selectedCardIndex!]
                    );
                  } catch (err: any) {
                    setMessage(err.message);
                    return;
                  }
                  if (result.clueGiverAmt === 1) {
                    setSubmitStatus(
                      "Clue submitted! You were the first to give it for this word set."
                    );
                  } else {
                    setSubmitStatus(
                      `Clue submitted! It was the same clue given by ${result.clueGiverAmt} clue givers so far for this word set.`
                    );
                  }
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
      {message && <div>{message}</div>}
    </div>
  );
}
