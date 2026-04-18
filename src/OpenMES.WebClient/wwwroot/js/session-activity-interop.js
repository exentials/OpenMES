/**
 * SessionActivityInterop.js
 * JavaScript interop for tracking user activity on the WebClient terminal.
 * 
 * Registers global event listeners for click, keypress, and mousemove.
 * Throttles calls to RefreshActivity to avoid excessive invocations.
 * 
 * WEB-003: Phase 1 - Session Management
 */

export function initializeActivityTracking(dotnetHelper) {
    if (!dotnetHelper) {
        console.error('SessionActivityInterop: dotnetHelper is required');
        return;
    }

    // Throttle activity refresh - don't call more than once per 10 seconds
    let lastActivityTime = Date.now();
    const THROTTLE_MS = 10000; // 10 seconds

    const refreshActivity = () => {
        const now = Date.now();
        if (now - lastActivityTime >= THROTTLE_MS) {
            lastActivityTime = now;
            dotnetHelper.invokeMethodAsync('RefreshActivity')
                .catch(err => console.error('Error refreshing activity:', err));
        }
    };

    // Register event listeners for user activity
    const eventTypes = ['click', 'keypress', 'mousemove', 'scroll', 'touchstart'];
    
    eventTypes.forEach(eventType => {
        document.addEventListener(eventType, refreshActivity, { passive: true });
    });

    // Return cleanup function
    return () => {
        eventTypes.forEach(eventType => {
            document.removeEventListener(eventType, refreshActivity);
        });
    };
}

/**
 * Get device info (for diagnostics).
 * Returns screen size, user agent, etc.
 */
export function getDeviceInfo() {
    return {
        userAgent: navigator.userAgent,
        screenWidth: window.innerWidth,
        screenHeight: window.innerHeight,
        timestamp: new Date().toISOString()
    };
}

/**
 * Show browser notification (for timeout warning).
 * Requires permissions from user.
 */
export function showNotification(title, options) {
    if ('Notification' in window && Notification.permission === 'granted') {
        new Notification(title, options);
    }
}

/**
 * Request notification permission from user.
 */
export async function requestNotificationPermission() {
    if ('Notification' in window && Notification.permission === 'default') {
        const permission = await Notification.requestPermission();
        return permission === 'granted';
    }
    return Notification.permission === 'granted';
}
