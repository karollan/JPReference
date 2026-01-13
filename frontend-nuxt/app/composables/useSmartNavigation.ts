/**
 * Smart navigation composable that tracks in-app history.
 * Provides a goBack function that:
 * 1. Navigates back within the app's history
 * 2. Falls back to /search if there's no in-app history (e.g., landed directly on a details page)
 */
export function useSmartNavigation() {
    const router = useRouter()

    /**
     * Go back within the app. If there's no in-app history,
     * navigate to /search instead of going to an external site.
     */
    function goBack() {
        // Check if we have history state from our app
        // window.history.state contains Vue Router's internal state
        // If we navigated within our app, back will be set
        if (typeof window !== 'undefined' && window.history.state?.back) {
            router.back()
        } else {
            // No in-app history, go to search as the default landing page
            router.push('/search')
        }
    }

    return {
        goBack
    }
}
