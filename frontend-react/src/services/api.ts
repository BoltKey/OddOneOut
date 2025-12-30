import type { User } from "../types";

const BASE_URL = "/api";

export const api = {
  // 1. REGISTER (Create new user)
  register: async (email: string, password: string) => {
    const res = await fetch(`${BASE_URL}/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) {
      const err = await res.json();
      // Identity API returns errors in a specific 'errors' array format
      throw new Error(
        err.errors
          ? Object.values(err.errors).flat().join(", ")
          : "Registration failed"
      );
    }
    return res;
  },

  // 2. LOGIN (Get Access)
  login: async (email: string, password: string) => {
    // useCookies: true tells .NET to set a secure HttpOnly cookie
    // useSessionCookies: true tells it to delete cookie when browser closes
    const res = await fetch(
      `${BASE_URL}/auth/login?useCookies=true&useSessionCookies=true`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      }
    );

    if (!res.ok) throw new Error("Invalid login attempt.");
    // We don't need to return JSON here. The Cookie is set automatically by the browser.
    return;
  },

  assignedGuess: async () => {
    const res = await fetch(`${BASE_URL}/Games/AssignedGuess`, {
      method: "POST",
    });
    if (!res.ok) {
      throw new Error("Failed to fetch assigned guess.");
    }
    return res.json();
  },
  assignedClueGiving: async () => {
    const res = await fetch(`${BASE_URL}/Games/AssignedGiveClue`, {
      method: "POST",
    });
    if (!res.ok) {
      throw new Error("Failed to fetch assigned clue giving.");
    }
    return res.json();
  },

  submitGuess: async (guess: boolean) => {
    const res = await fetch(`${BASE_URL}/Games/MakeGuess`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ guessIsInSet: guess }),
    });
    if (!res.ok) {
      throw new Error("Failed to submit guess.");
    }
    return res.json();
  },
  submitClue: async (clue: string, gameId: string, oddOneOut: string) => {
    const res = await fetch(`${BASE_URL}/Games/CreateGame`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ clue, wordSetId: gameId, oddOneOut }),
    });
    if (!res.ok) {
      throw new Error("Failed to submit clue.");
    }
  },
  getGuessHistory: async (page: number) => {
    const res = await fetch(`${BASE_URL}/Stats/GuessHistory?page=${page}`);
    if (!res.ok) {
      throw new Error("Failed to fetch guess history.");
    }
    return res.json();
  },
  getClueHistory: async (page: number) => {
    const res = await fetch(`${BASE_URL}/Stats/ClueHistory?page=${page}`);
    if (!res.ok) {
      throw new Error("Failed to fetch clue history.");
    }
    return res.json();
  },
  getMe: async (): Promise<User> => {
    const res = await fetch(`${BASE_URL}/Stats/me`);
    if (!res.ok) {
      throw new Error("Not authenticated");
    }
    return res.json();
  },

  // ... keep your submitGuess and other methods here
};
