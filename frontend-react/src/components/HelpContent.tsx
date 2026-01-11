export const GuessRatingHelpContent = (
  <>
    <p>
      Your <strong>Guess Rating</strong> measures how well you identify Misfits
      and Matches.
    </p>
    <ul>
      <li>
        <strong>Starting Rating:</strong> 1000
      </li>
      <li>
        <strong>Correct Match:</strong> +10 base points
      </li>
      <li>
        <strong>Wrong Match:</strong> -20 base points
      </li>
      <li>
        <strong>Correct Misfit:</strong> +15 base points
      </li>
      <li>
        <strong>Wrong Misfit:</strong> -30 base points
      </li>
    </ul>
    <p>
      <strong>Multiplier:</strong> Lower ratings gain more and lose less. Higher
      ratings gain less and lose more. This keeps things balanced!
    </p>
    <p>
      <strong>Minimum Rating:</strong> 100
    </p>
    <p style={{ marginTop: "12px", fontSize: "0.9em", color: "#666" }}>
      Something feel unfair? Share feedback on{" "}
      <a
        href="https://www.reddit.com/r/misfitgame/"
        target="_blank"
        rel="noopener noreferrer"
        style={{ color: "#ff4500" }}
      >
        r/misfitgame
      </a>
      !
    </p>
  </>
);

export const ClueRatingHelpContent = (
  <>
    <p>
      Your <strong>Clue Rating</strong> measures the quality of your clues.
    </p>
    <p>
      <strong>Clue Success Rate:</strong> Each card gets a success rate calculated using the following formula: <br></br>
      <br />
      (correct guesses + 2) / (total guesses + 3) * 100
      <br />
      <br />
      The clue success rate is then calculated as the geometric mean of the success rates of all the cards.
    </p>
    <p>
      The success rate is then compared to success rates of all other clues for the same card set. So the final game score is 100 + (clue success rate - average clue success rate).
    </p>
    Put simply:
    <ul>
      <li>
        <strong>Above 100:</strong> Better than average clue
      </li>
      <li>
        <strong>Below 100:</strong> Worse than average clue
      </li>
    </ul>
    <p>
      <strong>Rating Calculation:</strong> Sum of your last 100 game scores,
      starting from 1000. Each older clue has weigh of 1% less than the first one.
    </p>
    <p>
      <strong>Time Decay:</strong> Each game's contribution decreases by 1 point
      per day. Create fresh clues regularly to keep your rating high!
    </p>
    <p style={{ marginTop: "12px", fontSize: "0.9em", color: "#666" }}>
      Something feel unfair? Share feedback on{" "}
      <a
        href="https://www.reddit.com/r/misfitgame/"
        target="_blank"
        rel="noopener noreferrer"
        style={{ color: "#ff4500" }}
      >
        r/misfitgame
      </a>
      !
    </p>
  </>
);

