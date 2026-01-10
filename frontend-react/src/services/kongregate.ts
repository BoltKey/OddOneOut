// Kongregate API types and utilities

export interface KongregateUser {
  userId: number;
  username: string;
  gameAuthToken: string;
}

export interface KongregateAPI {
  services: {
    getUserId: () => number;
    getUsername: () => string;
    getGameAuthToken: () => string;
    isGuest: () => boolean;
    showRegistrationBox: () => void;
  };
}

declare global {
  interface Window {
    kongregate?: KongregateAPI;
    kongregateAPI?: {
      loadAPI: (callback: () => void) => void;
      getAPI: () => KongregateAPI;
    };
  }
}

/**
 * Check if the game is running on Kongregate
 */
export function isKongregateContext(): boolean {
  // Check if we're in an iframe on Kongregate
  try {
    const isInIframe = window.self !== window.top;
    const referrer = document.referrer.toLowerCase();
    const isKongregateReferrer = referrer.includes('kongregate.com');
    
    // Also check if the Kongregate API is available
    const hasKongregateAPI = typeof window.kongregateAPI !== 'undefined' || typeof window.kongregate !== 'undefined';
    
    return (isInIframe && isKongregateReferrer) || hasKongregateAPI;
  } catch {
    // If we can't access window.top due to cross-origin restrictions, we're in an iframe
    return true;
  }
}

/**
 * Initialize the Kongregate API and get user info
 * Returns null if user is a guest or API not available
 */
export function initKongregate(): Promise<KongregateUser | null> {
  return new Promise((resolve) => {
    // If API is already loaded
    if (window.kongregate) {
      const user = getKongregateUser();
      resolve(user);
      return;
    }

    // If API loader is available but not yet loaded
    if (window.kongregateAPI) {
      window.kongregateAPI.loadAPI(() => {
        window.kongregate = window.kongregateAPI!.getAPI();
        const user = getKongregateUser();
        resolve(user);
      });
      return;
    }

    // API not available - might be running outside Kongregate
    resolve(null);
  });
}

/**
 * Get the current Kongregate user info
 * Returns null if user is a guest or not logged in
 */
function getKongregateUser(): KongregateUser | null {
  if (!window.kongregate) {
    return null;
  }

  const userId = window.kongregate.services.getUserId();
  const username = window.kongregate.services.getUsername();
  const gameAuthToken = window.kongregate.services.getGameAuthToken();

  // User ID of 0 means guest
  if (userId === 0 || !gameAuthToken) {
    return null;
  }

  return {
    userId,
    username,
    gameAuthToken,
  };
}

/**
 * Show Kongregate's registration/login dialog
 */
export function showKongregateLoginDialog(): void {
  if (window.kongregate) {
    window.kongregate.services.showRegistrationBox();
  }
}
