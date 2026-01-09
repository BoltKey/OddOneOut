import React from "react";

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
    <ul>
      <li>
        <strong>Starting Rating:</strong> 1000
      </li>
      <li>
        <strong>Game Score:</strong> Based on how well players guess your clue
      </li>
      <li>
        <strong>Score = 100 +</strong> (your success rate - average success
        rate)
      </li>
    </ul>
    <p>
      <strong>Rating Update:</strong> Your rating increases by each game's
      score. Only your last 100 games count.
    </p>
    <p>
      <strong>Time Decay:</strong> Each game's contribution decreases by 1 point
      per day. Create fresh clues regularly!
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

export const GameScoreHelpContent = (
  <>
    <p>
      <strong>Game Score</strong> measures how well-balanced your clue was.
    </p>
    <p>
      <strong>Clue Success Rate:</strong> Each clue has a success rate based on
      geometric average of success rates for each individual word. In addition,
      each card counts 3 dummy guesses, out of which 2 count as correct.
      Basically, you goal is to create clues that are good for all the cards. If
      one card is too hard, it will be more punishing.
      <strong>Clue Score Formula:</strong> 100 + (your clue's success rate -
      average success rate of other clues with same words)
    </p>
    <ul>
      <li>
        <strong>Above 100:</strong> Better clue than average
      </li>
      <li>
        <strong>Below 100:</strong> Worse clue than average
      </li>
    </ul>
    <p>
      This score contributes to your <strong>Clue Rating</strong>. Each clue
      loses 1 point per day, and older clues count for less score, 1% less for
      each clue. (e.g. your 5th most recent clue counts 5% less).
    </p>
  </>
);
