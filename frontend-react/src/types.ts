// src/types.ts

// The basic building block
export interface WordCard {
  id: string;
  word: string;
  category: string;
}

// A collection of cards + games that use them
export interface CardSet {
  id: string;
  wordCards: WordCard[];
  // We usually don't need the list of Games on the client side here
}

// The Game logic
export interface Game {
  id: string;
  createdAt: string; // ISO Date string
  clue: string;
  cardSet: CardSet; // The cards currently in play
  oddOneOut: WordCard;
  guesses: Guess[];
}

// A specific guess made by a user
export interface Guess {
  id: string;
  gameId: string; // Helpful for local logic
  isCorrect: boolean;
  guessIsInSet: boolean;
  guessedAt: string;
  selectedCard: WordCard; // The full object we fixed!
}

// The User (Player)
export interface User {
  id: string; // Identity GUID
  isGuest: boolean;
  userName?: string;
  displayName?: string;
  email?: string;
  currentGame?: Game;
  currentGameId?: string;
  guesses: Guess[];
  guessRating: number;
  createdGames: Game[];
  clueRating: number;
  guessEnergy: number;
  clueEnergy: number;
  maxGuessEnergy: number;
  maxClueEnergy: number;
  nextGuessRegenTime: string | null;
  nextClueRegenTime: string | null;
  canGiveClues: boolean;
  guessRank?: number;
  clueRank?: number;
}
