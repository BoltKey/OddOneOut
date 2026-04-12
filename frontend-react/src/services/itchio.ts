/**
 * Itch.io integration utilities
 *
 * This module detects if the app is running inside an itch.io iframe
 * and provides access to the itch.io user context via their JavaScript API.
 */

export interface ItchioContext {
  userId?: number;
  username?: string;
  displayName?: string;
  coverUrl?: string;
}

interface ItchioUser {
  id: number;
  username: string;
  display_name?: string;
  cover_url?: string;
}

interface ItchioUserResult {
  user?: ItchioUser;
}

interface ItchioAPI {
  getUser?: () => Promise<ItchioUserResult>;
}

declare global {
  interface Window {
    Itch?: ItchioAPI;
  }
}

let cachedContext: ItchioContext | null = null;
let contextInitialized = false;

export async function getItchioContext(): Promise<ItchioContext | null> {
  if (contextInitialized) {
    return cachedContext;
  }

  try {
    if (
      typeof window !== "undefined" &&
      window.Itch &&
      typeof window.Itch.getUser === "function"
    ) {
      const result = await window.Itch.getUser();

      if (result.user) {
        cachedContext = {
          userId: result.user.id,
          username: result.user.username,
          displayName: result.user.display_name || result.user.username,
          coverUrl: result.user.cover_url,
        };
      } else {
        cachedContext = null;
      }
    } else {
      cachedContext = null;
    }
  } catch {
    cachedContext = null;
  }

  contextInitialized = true;
  return cachedContext;
}

export function isItchioEnvironment(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  const host = window.location.hostname.toLowerCase();
  const hostedOnItch = host.endsWith(".itch.io") || host.endsWith(".itch.zone");
  return hostedOnItch || window.Itch !== undefined;
}

export function resetItchioContext(): void {
  cachedContext = null;
  contextInitialized = false;
}
