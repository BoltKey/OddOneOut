import GuessLeaderboardTab from "./GuessLeaderboardTab";
import ClueLeaderboardTab from "./ClueLeaderboardTab";
import "./LeaderboardTab.css";

export default function LeaderboardTab({ userId }: { userId: string }) {
  return (
    <div className="leaderboard-tabs-wrapper">
      <GuessLeaderboardTab userId={userId} />
      <div style={{ marginTop: "40px" }}></div>
      <ClueLeaderboardTab userId={userId} />
    </div>
  );
}
