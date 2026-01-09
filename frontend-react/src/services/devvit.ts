/**
 * Devvit/Reddit Games integration utilities
 * 
 * This module detects if the app is running inside a Reddit Devvit webview
 * and provides access to the Reddit user context.
 */

// Type definitions for Devvit context
export interface DevvitContext {
  userId?: string;       // Reddit user ID in T2 format (e.g., "t2_abc123")
  postId?: string;       // The post this webview is embedded in
  subredditName?: string;
  subredditId?: string;
  appName?: string;
  appVersion?: string;
}

let cachedContext: DevvitContext | null = null;
let contextInitialized = false;

/**
 * Initialize and get the Devvit context.
 * Returns null if not running inside Reddit/Devvit.
 */
export async function getDevvitContext(): Promise<DevvitContext | null> {
  if (contextInitialized) {
    return cachedContext;
  }

  try {
    // Dynamically import @devvit/client to avoid errors when not in Devvit environment
    const devvitClient = await import('@devvit/client');
    const context = devvitClient.context;
    
    if (context && context.userId) {
      cachedContext = {
        userId: context.userId,
        postId: context.postId,
        subredditName: context.subredditName,
        subredditId: context.subredditId,
        appName: context.appName,
        appVersion: context.appVersion,
      };
    }
  } catch (error) {
    // Not running in Devvit environment - this is expected outside Reddit
    console.log('Not running in Devvit environment');
    cachedContext = null;
  }

  contextInitialized = true;
  return cachedContext;
}

/**
 * Check if running inside Reddit/Devvit webview
 */
export function isDevvitEnvironment(): boolean {
  return cachedContext !== null && cachedContext.userId !== undefined;
}

/**
 * Get the Reddit user ID if available
 */
export function getRedditUserId(): string | undefined {
  return cachedContext?.userId;
}

/**
 * Reset the context cache (useful for testing)
 */
export function resetDevvitContext(): void {
  cachedContext = null;
  contextInitialized = false;
}
