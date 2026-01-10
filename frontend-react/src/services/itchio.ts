/**
 * Itch.io integration utilities
 * 
 * This module detects if the app is running inside an itch.io iframe
 * and provides access to the itch.io user context via their JavaScript API.
 * 
 * Documentation: https://itch.io/docs/itch/integrating/html5-api.html
 */

// Type definitions for itch.io context
export interface ItchioContext {
  userId?: number;        // Itch.io user ID (numeric)
  username?: string;      // Itch.io username
  displayName?: string;   // User's display name
  coverUrl?: string;      // User's avatar URL
}

// Itch.io API response types
interface ItchioUser {
  id: number;
  username: string;
  display_name?: string;
  cover_url?: string;
}

interface ItchioUserResult {
  user?: ItchioUser;
}

// Itch.io global object type
interface ItchioAPI {
  getUser: () => Promise<ItchioUserResult>;
}

declare global {
  interface Window {
    Itch?: ItchioAPI;
  }
}

let cachedContext: ItchioContext | null = null;
let contextInitialized = false;

/**
 * Initialize and get the itch.io context.
 * Returns null if not running inside itch.io or user is not logged in.
 */
export async function getItchioContext(): Promise<ItchioContext | null> {
  if (contextInitialized) {
    return cachedContext;
  }

  try {
    // Check if we're running in itch.io (the Itch global is injected by itch.io)
    if (typeof window !== 'undefined' && window.Itch) {
      const result = await window.Itch.getUser();
      
      if (result.user) {
        cachedContext = {
          userId: result.user.id,
          username: result.user.username,
          displayName: result.user.display_name || result.user.username,
          coverUrl: result.user.cover_url,
        };
        console.log('Itch.io user detected:', cachedContext.username);
      } else {
        // User is not logged in on itch.io
        console.log('Running on itch.io but user is not logged in');
        cachedContext = null;
      }
    } else {
      // Not running in itch.io environment
      cachedContext = null;
    }
  } catch (error) {
    console.log('Error getting itch.io context:', error);
    cachedContext = null;
  }

  contextInitialized = true;
  return cachedContext;
}

/**
 * Check if running inside itch.io
 */
export function isItchioEnvironment(): boolean {
  return typeof window !== 'undefined' && window.Itch !== undefined;
}

/**
 * Check if we have a logged-in itch.io user
 */
export function hasItchioUser(): boolean {
  return cachedContext !== null && cachedContext.userId !== undefined;
}

/**
 * Get the itch.io user ID if available
 */
export function getItchioUserId(): number | undefined {
  return cachedContext?.userId;
}

/**
 * Reset the context cache (useful for testing)
 */
export function resetItchioContext(): void {
  cachedContext = null;
  contextInitialized = false;
}
