/**
 * Plugin to fetch filter registry from backend on app initialization.
 * This ensures the registry is synced before any component uses it.
 * Falls back to hardcoded registry if fetch fails.
 */
import { setFilterRegistry, type FilterRegistryEntry } from '@/utils/filters'

export default defineNuxtPlugin(async () => {
    const config = useRuntimeConfig()
    // Use the apiUrl from runtime config (relative /api for client, http://backend:5000/api for server)
    const apiUrl = config.public.apiUrl || '/api'

    try {
        const registry = await $fetch<FilterRegistryEntry[]>(`${apiUrl}/Filters/registry`)
        setFilterRegistry(registry)
        console.log(`[filterRegistry] Synced ${registry.length} filters from backend`)
    } catch (e) {
        console.warn('[filterRegistry] Failed to fetch registry, using fallback:', e)
        // Fallback registry will be used automatically via getFilterRegistry()
    }
})
