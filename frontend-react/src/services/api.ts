import type { User } from "../types";

const BASE_URL = import.meta.env.VITE_API_URL;
// VITE_API_URL will automatically be swapped when you run 'npm run build';

export const api = {
  // 1. REGISTER (Create new user)
  register: async (username: string, password: string, email?: string) => {
    const res = await fetch(`${BASE_URL}/user/signup`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password, email }),
    });
    if (!res.ok) {
      const err = await res.text();
      // Identity API returns errors in a specific 'errors' array format
      throw new Error(err || "Registration failed.");
    }
    // Clear guest ID when a registered user signs up
    localStorage.removeItem("guestUserId");
    return res;
  },

  // 2. LOGIN (Get Access)
  login: async (username: string, password: string) => {
    // useCookies: true tells .NET to set a secure HttpOnly cookie
    // useSessionCookies: true tells it to delete cookie when browser closes
    const res = await fetch(
      `${BASE_URL}/user/login?useCookies=true&useSessionCookies=true`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      }
    );

    if (!res.ok) throw new Error("Invalid login attempt.");
    // Clear guest ID when a registered user logs in
    localStorage.removeItem("guestUserId");
    // We don't need to return JSON here. The Cookie is set automatically by the browser.
    return;
  },
  logout: async () => {
    const res = await fetch(`${BASE_URL}/user/logout`, {
      method: "POST",
    });
    if (!res.ok) {
      throw new Error("Logout failed.");
    }
    return;
  },
  loginGoogle: async () => {
    const res = await fetch(
      `${BASE_URL}/user/login-google?useCookies=true&useSessionCookies=true`,
      {
        method: "GET",
      }
    );
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message || "Google login failed.");
    }
    return;
  },
  createGuest: async (guestUserId?: string) => {
    const guestParam = guestUserId ? `&guestUserId=${encodeURIComponent(guestUserId)}` : "";
    const res = await fetch(
      `${BASE_URL}/user/create-guest?useCookies=true&useSessionCookies=true${guestParam}`,
      {
        method: "POST",
        credentials: "include",
      }
    );
    if (!res.ok) {
      throw new Error((await res.text()) || "Guest login failed.");
    }
    const data = await res.json();
    return data;
  },
  changeDisplayName: async (NewDisplayName: string) => {
    const res = await fetch(`${BASE_URL}/User/ChangeDisplayName`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ NewDisplayName }),
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message || "Failed to change display name.");
    }
    return;
  },

  assignedGuess: async () => {
    const res = await fetch(`${BASE_URL}/Games/AssignedGuess`, {
      method: "POST",
    });
    if (!res.ok) {
      const errorText = await res.text();
      throw new Error(errorText || "Failed to fetch assigned guess.");
    }
    return res.json();
  },
  assignedClueGiving: async () => {
    const res = await fetch(`${BASE_URL}/Games/AssignedGiveClue`, {
      method: "POST",
    });
    if (!res.ok) {
      throw new Error(
        (await res.text()) || "Failed to fetch assigned clue giving."
      );
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
      throw new Error(await res.text());
    }
    return res.json();
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
  getGuessLeaderboard: async (): Promise<
    { userName: string; guessRating: number; rank: number }[]
  > => {
    const res = await fetch(`${BASE_URL}/Stats/GuessLeaderboard`);
    if (!res.ok) {
      throw new Error("Failed to fetch guess leaderboard.");
    }
    return res.json();
  },
  getClueLeaderboard: async (): Promise<
    { userName: string; clueRating: number; rank: number }[]
  > => {
    const res = await fetch(`${BASE_URL}/Stats/ClueLeaderboard`);
    if (!res.ok) {
      throw new Error("Failed to fetch clue leaderboard.");
    }
    return res.json();
  },
  getMe: async (): Promise<User> => {
    const res = await fetch(`${BASE_URL}/User/me`);
    if (!res.ok) {
      throw new Error("Not authenticated");
    }
    return res.json();
  },

  /**
   * Authenticate a Reddit user coming from Devvit/Reddit Games.
   * This creates or finds a user based on their Reddit user ID.
   */
  redditLogin: async (redditUserId: string, redditUsername?: string) => {
    const res = await fetch(
      `${BASE_URL}/user/reddit-login`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ redditUserId, redditUsername }),
      }
    );
    if (!res.ok) {
      const err = await res.text();
      throw new Error(err || "Reddit login failed.");
    }
    return res.json();
  },
};
