// API URL helper for Nuxt
// In Nuxt, we use useRuntimeConfig() in components/pages
// Services should receive the API URL as a parameter or use $fetch

export function getApiUrl(): string {
    // For SSR compatibility, we check if we're on the server or client
    // This is a fallback - prefer using useRuntimeConfig() in components
    if (import.meta.server) {
        return process.env.NUXT_PUBLIC_API_URL || 'http://backend:5000/api'
    }
    return (window as any).__NUXT__?.config?.public?.apiUrl || 'http://localhost:5000/api'
}
